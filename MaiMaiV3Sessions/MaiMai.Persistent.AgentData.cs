using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using MaiMai.Core;

namespace MaiMai.Implementation
{
    /// <summary>
    /// Serializable data structure for agent persistence
    /// </summary>
    [Serializable]
    public class AgentData
    {
        public string AgentId { get; set; }
        public string AgentName { get; set; }
        public string SystemPrompt { get; set; }
        public Dictionary<string, List<MessageEntry>> UserConversations { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        
        public AgentData()
        {
            UserConversations = new Dictionary<string, List<MessageEntry>>();
        }
    }
    
    /// <summary>
    /// Serializable manifest for tracking all agents
    /// </summary>
    [Serializable]
    public class AgentManifest
    {
        public List<string> AgentIds { get; set; } = new List<string>();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Implements persistence for AI agents using JSON serialization
    /// </summary>
    public class JsonPersistenceProvider : IPersistenceProvider
    {
        // Dependencies for creating agents
        private readonly ILLMProvider _llmProvider;
        private readonly IMemoryProvider _memoryProvider;
        private readonly IContextBuilder _contextBuilder;
        
        // Path configuration
        private readonly string _basePath;
        private readonly string _manifestPath;
        
        /// <summary>
        /// Creates a new persistence provider with the specified dependencies
        /// </summary>
        public JsonPersistenceProvider(
            ILLMProvider llmProvider,
            IMemoryProvider memoryProvider,
            IContextBuilder contextBuilder,
            string basePath = null)
        {
            _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
            _memoryProvider = memoryProvider ?? throw new ArgumentNullException(nameof(memoryProvider));
            _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
            
            _basePath = string.IsNullOrEmpty(basePath) 
                ? Path.Combine(Application.persistentDataPath, "MaiMai", "Agents") 
                : basePath;
                
            _manifestPath = Path.Combine(_basePath, "agent_manifest.json");
            
            // Ensure directory exists
            Directory.CreateDirectory(_basePath);
        }
        
        /// <summary>
        /// Gets the file path for an agent's data
        /// </summary>
        private string GetAgentFilePath(string agentId)
        {
            return Path.Combine(_basePath, $"{agentId}.json");
        }

        /// <summary>
        /// Saves an agent's data to disk
        /// </summary>
        public async Task SaveAgentAsync(IAIAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }
            
            try
            {
                // Convert to concrete implementation to access non-interface properties
                // In a real implementation, you might use a different approach
                var concreteAgent = agent as AIAgent;
                if (concreteAgent == null)
                {
                    throw new InvalidOperationException(
                        $"Agent type {agent.GetType().Name} is not supported by this persistence provider");
                }
                
                // Create data object
                var data = new AgentData
                {
                    AgentId = agent.AgentId,
                    AgentName = agent.AgentName,
                    SystemPrompt = concreteAgent.SystemPrompt,
                    LastModified = DateTime.Now
                };
                
                // Get user conversations - in a real implementation, this would be exposed in a better way
                // This is a simplification for the example
                var userConversations = typeof(AIAgent)
                    .GetField("_userConversations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(concreteAgent) as Dictionary<string, List<MessageEntry>>;
                    
                if (userConversations != null)
                {
                    data.UserConversations = new Dictionary<string, List<MessageEntry>>(userConversations);
                }
                
                // Serialize to JSON
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                
                // Write to file
                string filePath = GetAgentFilePath(agent.AgentId);
                await WriteTextAsync(filePath, json);
                
                // Update manifest
                await UpdateManifestAsync(agent.AgentId);
                
                Debug.Log($"Agent {agent.AgentName} saved to {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save agent {agent.AgentName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads an agent from disk by ID
        /// </summary>
        public async Task<IAIAgent> LoadAgentAsync(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));
            }
            
            string filePath = GetAgentFilePath(agentId);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Agent data file not found: {filePath}");
            }
            
            try
            {
                // Read and deserialize JSON
                string json = await ReadTextAsync(filePath);
                var data = JsonConvert.DeserializeObject<AgentData>(json);
                
                if (data == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize agent data from {filePath}");
                }
                
                // Create and initialize agent
                var agent = new AIAgent(
                    data.AgentId,
                    data.AgentName,
                    data.SystemPrompt,
                    _llmProvider,
                    _memoryProvider,
                    _contextBuilder);
                
                // Set conversations - in a real implementation, this would be handled better
                var conversationsField = typeof(AIAgent)
                    .GetField("_userConversations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (conversationsField != null && data.UserConversations != null)
                {
                    conversationsField.SetValue(agent, new Dictionary<string, List<MessageEntry>>(data.UserConversations));
                }
                
                // Initialize the agent
                await agent.Initialize();
                
                Debug.Log($"Agent {data.AgentName} loaded from {filePath}");
                return agent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load agent {agentId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads all agents from the manifest
        /// </summary>
        public async Task<List<IAIAgent>> LoadAllAgentsAsync()
        {
            List<IAIAgent> agents = new List<IAIAgent>();
            
            if (!File.Exists(_manifestPath))
            {
                Debug.Log("Agent manifest not found. No agents to load.");
                return agents;
            }
            
            try
            {
                // Read and deserialize manifest
                string json = await ReadTextAsync(_manifestPath);
                var manifest = JsonConvert.DeserializeObject<AgentManifest>(json);
                
                if (manifest == null || manifest.AgentIds == null || manifest.AgentIds.Count == 0)
                {
                    Debug.Log("Manifest contains no agent IDs.");
                    return agents;
                }
                
                // Load each agent
                foreach (string agentId in manifest.AgentIds)
                {
                    try
                    {
                        var agent = await LoadAgentAsync(agentId);
                        if (agent != null)
                        {
                            agents.Add(agent);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to load agent {agentId}: {ex.Message}");
                        // Continue loading other agents
                    }
                }
                
                Debug.Log($"Loaded {agents.Count} agents from manifest");
                return agents;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load agents from manifest: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves the agent manifest containing all agent IDs
        /// </summary>
        public async Task SaveManifestAsync(List<IAIAgent> agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }
            
            try
            {
                // Create manifest
                var manifest = new AgentManifest
                {
                    AgentIds = agents.Select(a => a.AgentId).ToList(),
                    LastUpdated = DateTime.Now
                };
                
                // Serialize to JSON
                string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
                
                // Write to file
                await WriteTextAsync(_manifestPath, json);
                
                Debug.Log($"Agent manifest updated with {agents.Count} agents");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save agent manifest: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Updates the manifest to include an agent ID
        /// </summary>
        private async Task UpdateManifestAsync(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                return;
            }
            
            try
            {
                AgentManifest manifest;
                
                if (File.Exists(_manifestPath))
                {
                    // Read existing manifest
                    string json = await ReadTextAsync(_manifestPath);
                    manifest = JsonConvert.DeserializeObject<AgentManifest>(json) ?? new AgentManifest();
                }
                else
                {
                    // Create new manifest
                    manifest = new AgentManifest();
                }
                
                // Add agent ID if not already present
                if (!manifest.AgentIds.Contains(agentId))
                {
                    manifest.AgentIds.Add(agentId);
                    manifest.LastUpdated = DateTime.Now;
                    
                    // Serialize and save
                    string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
                    await WriteTextAsync(_manifestPath, json);
                    
                    Debug.Log($"Agent {agentId} added to manifest");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update manifest with agent {agentId}: {ex.Message}");
                // Continue without throwing to avoid blocking agent save
            }
        }
        
        // Helper methods for file I/O
        private async Task WriteTextAsync(string filePath, string content)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                await writer.WriteAsync(content);
            }
        }
        
        private async Task<string> ReadTextAsync(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}