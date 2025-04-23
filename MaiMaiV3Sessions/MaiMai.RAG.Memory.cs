using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
using MaiMai.Core;

namespace MaiMai.Implementation
{
    /// <summary>
    /// Implements memory management using Retrieval-Augmented Generation (RAG)
    /// </summary>
    public class RAGMemoryProvider : IMemoryProvider
    {
        private readonly Dictionary<string, RAG> _agentRags = new Dictionary<string, RAG>();
        private readonly Dictionary<string, HashSet<string>> _embeddedMessageIds = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, List<MessageEntry>> _conversationCache = new Dictionary<string, List<MessageEntry>>();
        
        // Configuration options
        private readonly int _maxCacheSize;
        private readonly string _savePath;
        private readonly SearchMethods _searchMethod;
        private readonly ChunkingMethods _chunkingMethod;
        
        // Dependencies
        private readonly ILLMProvider _llmProvider;
        
        /// <summary>
        /// Create a new RAG memory provider
        /// </summary>
        public RAGMemoryProvider(
            ILLMProvider llmProvider, 
            string savePath = null,
            int maxCacheSize = 100,
            SearchMethods searchMethod = SearchMethods.DBSearch,
            ChunkingMethods chunkingMethod = ChunkingMethods.SentenceSplitter)
        {
            _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
            _savePath = string.IsNullOrEmpty(savePath) 
                ? Path.Combine(Application.persistentDataPath, "MaiMai", "Memory") 
                : savePath;
            _maxCacheSize = maxCacheSize;
            _searchMethod = searchMethod;
            _chunkingMethod = chunkingMethod;
            
            // Ensure directory exists
            Directory.CreateDirectory(_savePath);
        }

        /// <summary>
        /// Ensures that a RAG instance exists for the specified agent
        /// </summary>
        private async Task<RAG> GetOrCreateRAGForAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));
            }

            if (_agentRags.TryGetValue(agentId, out RAG ragInstance))
            {
                return ragInstance;
            }

            // Create a new RAG instance
            RAG rag = new GameObject($"RAG_{agentId}").AddComponent<RAG>();
            rag.Init(_searchMethod, _chunkingMethod);

            // Find the embedding LLM in the scene
            LLM embeddingLLM = null;
            LLM[] allLLMs = UnityEngine.Object.FindObjectsOfType<LLM>();
            foreach (LLM llm in allLLMs)
            {
                // Look for the embedding-only model
                if (llm.embeddingsOnly)
                {
                    embeddingLLM = llm;
                    Debug.Log($"Found embedding LLM: {llm.name}");
                    break;
                }
            }

            // If no embedding-only model found, try to find one with "embedding" in the name or path
            if (embeddingLLM == null)
            {
                foreach (LLM llm in allLLMs)
                {
                    if (llm.model != null &&
                        (llm.model.ToLower().Contains("bge") ||
                         llm.model.ToLower().Contains("embed")))
                    {
                        embeddingLLM = llm;
                        Debug.Log($"Found likely embedding LLM: {llm.name} with model {llm.model}");
                        break;
                    }
                }
            }

            // Set the embedding LLM explicitly if found
            if (embeddingLLM != null && rag.search != null && rag.search.llmEmbedder != null)
            {
                rag.search.SetLLM(embeddingLLM);
                Debug.Log($"Set embedding LLM for RAG_{agentId}");
            }
            else
            {
                Debug.LogWarning($"Could not find suitable embedding LLM for RAG_{agentId}");
            }

            // Try to load existing state
            string ragFilePath = GetRAGFilePath(agentId);
            if (File.Exists(ragFilePath))
            {
                await rag.Load(ragFilePath);
                Debug.Log($"Loaded RAG state for agent {agentId}");
            }

            _agentRags[agentId] = rag;
            return rag;
        }

        private string GetRAGFilePath(string agentId)
        {
            return Path.Combine(_savePath, $"{agentId}_rag.state");
        }
        
        private string GetConversationFilePath(string agentId)
        {
            return Path.Combine(_savePath, $"{agentId}_conversations.json");
        }
        
        private bool IsMessageEmbedded(string agentId, string messageId)
        {
            if (!_embeddedMessageIds.TryGetValue(agentId, out var messageIds))
            {
                return false;
            }
            
            return messageIds.Contains(messageId);
        }
        
        private void MarkMessageAsEmbedded(string agentId, string messageId)
        {
            if (!_embeddedMessageIds.ContainsKey(agentId))
            {
                _embeddedMessageIds[agentId] = new HashSet<string>();
            }
            
            _embeddedMessageIds[agentId].Add(messageId);
        }

        /// <summary>
        /// Stores a conversation message in RAG memory
        /// </summary>
        public async Task StoreMessageAsync(MessageEntry message, string agentId)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            
            // Add to conversation cache
            string cacheKey = $"{agentId}_{message.Sender}";
            if (!_conversationCache.ContainsKey(cacheKey))
            {
                _conversationCache[cacheKey] = new List<MessageEntry>();
            }
            
            // Add to cache and trim if necessary
            _conversationCache[cacheKey].Add(message);
            if (_conversationCache[cacheKey].Count > _maxCacheSize)
            {
                _conversationCache[cacheKey].RemoveAt(0);
            }
            
            // Skip if already embedded
            if (IsMessageEmbedded(agentId, message.MessageId))
            {
                return;
            }
            
            try
            {
                // Format message for embedding
                string formattedMessage = $"From {message.Sender}: {message.Message}";
                
                // Get RAG for this agent
                RAG rag = await GetOrCreateRAGForAgent(agentId);


                if (rag == null || rag.search == null)
                {
                    Debug.LogError($"RAG system not properly initialized for agent {agentId}");
                    return;
                }

                // Add to RAG
                await rag.Add(formattedMessage);
                
                // Mark as embedded
                MarkMessageAsEmbedded(agentId, message.MessageId);
                
                Debug.Log($"Message {message.MessageId} embedded in RAG for agent {agentId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to store message in RAG: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Stores fixed memory content (like character background) in RAG
        /// </summary>
        public async Task StoreFixedMemoryAsync(string content, string agentId)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            
            try
            {
                // Get RAG for this agent
                RAG rag = await GetOrCreateRAGForAgent(agentId);
                
                // Split content into chunks for better retrieval
                string[] chunks = content.Split(
                    new[] { "\n\n", "---", "##" }, 
                    StringSplitOptions.RemoveEmptyEntries
                );
                
                foreach (string chunk in chunks)
                {
                    string trimmedChunk = chunk.Trim();
                    if (!string.IsNullOrEmpty(trimmedChunk))
                    {
                        // Add a tag to identify fixed memory
                        string taggedChunk = $"[FIXED_MEMORY] {trimmedChunk}";
                        await rag.Add(taggedChunk, "fixed");
                    }
                }
                
                Debug.Log($"Fixed memory stored for agent {agentId} ({chunks.Length} chunks)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to store fixed memory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves relevant context based on the query
        /// </summary>
        public async Task<string> RetrieveContextAsync(string query, string agentId, int resultCount = 3)
        {
            if (string.IsNullOrEmpty(query))
            {
                return string.Empty;
            }
            
            try
            {
                // Get RAG for this agent
                RAG rag = await GetOrCreateRAGForAgent(agentId);
                
                // Search for relevant content
                (string[] results, float[] distances) = await rag.Search(query, resultCount);
                
                if (results == null || results.Length == 0)
                {
                    return string.Empty;
                }
                
                // Format results with relevance scores
                List<string> formattedResults = new List<string>();
                for (int i = 0; i < results.Length; i++)
                {
                    // Lower distance means higher relevance
                    float relevanceScore = 1.0f - distances[i];
                    string relevanceIndicator = relevanceScore > 0.8f ? "Highly Relevant" : 
                                              relevanceScore > 0.6f ? "Relevant" : "Somewhat Relevant";
                    
                    formattedResults.Add($"{results[i]} [{relevanceIndicator}]");
                }
                
                string context = string.Join("\n\n", formattedResults);
                return context;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to retrieve context: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets recent conversation messages for a user with an agent
        /// </summary>
        public Task<List<MessageEntry>> GetRecentConversationAsync(string userId, string agentId, int maxMessages = 10)
        {
            string cacheKey = $"{agentId}_{userId}";
            
            if (!_conversationCache.TryGetValue(cacheKey, out var messages))
            {
                return Task.FromResult(new List<MessageEntry>());
            }
            
            // Get the most recent messages up to maxMessages
            int startIndex = Math.Max(0, messages.Count - maxMessages);
            List<MessageEntry> recentMessages = messages
                .Skip(startIndex)
                .Take(maxMessages)
                .ToList();
                
            return Task.FromResult(recentMessages);
        }

        /// <summary>
        /// Saves the current memory state for an agent
        /// </summary>
        public async Task SaveMemoryStateAsync(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));
            }
            
            if (!_agentRags.TryGetValue(agentId, out RAG rag))
            {
                Debug.LogWarning($"No RAG instance found for agent {agentId}. Nothing to save.");
                return;
            }
            
            try
            {
                string ragFilePath = GetRAGFilePath(agentId);
                rag.Save(ragFilePath);
                
                // TODO: Save conversation cache to disk
                // This would require serializing _conversationCache[agentId]
                
                Debug.Log($"Memory state saved for agent {agentId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save memory state for agent {agentId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads the memory state for an agent
        /// </summary>
        public async Task LoadMemoryStateAsync(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));
            }
            
            RAG rag = await GetOrCreateRAGForAgent(agentId);
            
            // The RAG was loaded in GetOrCreateRAGForAgent if the file existed
            
            // TODO: Load conversation cache from disk
            // This would require deserializing the conversation history
            
            Debug.Log($"Memory state loaded for agent {agentId}");
        }

        /// <summary>
        /// Clears all memory for an agent
        /// </summary>
        public async Task ClearMemoryAsync(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));
            }
            
            if (_agentRags.TryGetValue(agentId, out RAG rag))
            {
                // Destroy the RAG component
                rag.Clear();
                UnityEngine.Object.Destroy(rag.gameObject);
                _agentRags.Remove(agentId);
            }
            
            // Clear conversation cache for this agent
            foreach (var key in _conversationCache.Keys.ToList())
            {
                if (key.StartsWith($"{agentId}_"))
                {
                    _conversationCache.Remove(key);
                }
            }
            
            // Clear embedded message IDs
            _embeddedMessageIds.Remove(agentId);
            
            // Delete saved files
            string ragFilePath = GetRAGFilePath(agentId);
            if (File.Exists(ragFilePath))
            {
                File.Delete(ragFilePath);
            }
            
            string conversationFilePath = GetConversationFilePath(agentId);
            if (File.Exists(conversationFilePath))
            {
                File.Delete(conversationFilePath);
            }
            
            Debug.Log($"Memory cleared for agent {agentId}");
        }
    }
}