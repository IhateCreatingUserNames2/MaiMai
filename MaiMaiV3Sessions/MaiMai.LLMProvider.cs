using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
using MaiMai.Core;
using System.Linq;

namespace MaiMai.Implementation
{
    /// <summary>
    /// Provides LLM capabilities using LLMUnity as the backend
    /// </summary>
    public class LLMUnityProvider : ILLMProvider
    {
        private readonly LLMCharacter _llmCharacter;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private volatile bool _isInitialized = false;
        
        // Caching for performance
        private readonly Dictionary<string, float[]> _embeddingCache = new Dictionary<string, float[]>();
        private readonly int _maxCacheSize;
        
        // LLM configuration
        private readonly float _temperature;
        private readonly int _maxTokens;

        /// <summary>
        /// Creates a new LLM provider using LLMUnity
        /// </summary>
        public LLMUnityProvider(
            LLMCharacter llmCharacter, 
            float temperature = 0.7f, 
            int maxTokens = 1024,
            int maxCacheSize = 1000)
        {
            _llmCharacter = llmCharacter ?? throw new ArgumentNullException(nameof(llmCharacter));
            _temperature = temperature;
            _maxTokens = maxTokens;
            _maxCacheSize = maxCacheSize;
        }

        /// <summary>
        /// Initializes the LLM Provider
        /// </summary>
        public async Task Initialize()
        {
            if (_isInitialized)
            {
                return;
            }
            
            await _initLock.WaitAsync();
            
            try
            {
                if (_isInitialized)
                {
                    return;
                }
                
                if (_llmCharacter == null)
                {
                    throw new InvalidOperationException("LLMCharacter is not assigned");
                }
                
                if (_llmCharacter.llm == null)
                {
                    throw new InvalidOperationException("LLMCharacter does not have an LLM assigned");
                }
                
                // Wait for LLM to be ready
                await _llmCharacter.llm.WaitUntilReady();
                
                // Check if LLM setup was successful
                if (_llmCharacter.llm.failed)
                {
                    throw new InvalidOperationException("LLM failed to initialize");
                }
                
                // Warm up the model with an empty prompt
                await _llmCharacter.Warmup();
                
                _isInitialized = true;
                Debug.Log("LLMUnityProvider successfully initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize LLMUnityProvider: {ex.Message}");
                throw;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Checks if the LLM provider is ready for use
        /// </summary>
        public Task<bool> IsReady()
        {
            return Task.FromResult(_isInitialized && 
                                  _llmCharacter != null && 
                                  _llmCharacter.llm != null && 
                                  !_llmCharacter.llm.failed);
        }

        /// <summary>
        /// Shuts down the LLM provider
        /// </summary>
        public Task Shutdown()
        {
            _isInitialized = false;
            _embeddingCache.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a response from the LLM based on the prompt
        /// </summary>
        public async Task<string> GetResponseAsync(string prompt, Dictionary<string, string> parameters = null)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("LLMUnityProvider is not initialized");
            }
            
            if (string.IsNullOrEmpty(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
            }
            
            try
            {
                // Set parameters if provided
                if (parameters != null)
                {
                    if (parameters.TryGetValue("temperature", out string tempValue) && 
                        float.TryParse(tempValue, out float temp))
                    {
                        _llmCharacter.temperature = temp;
                    }
                    
                    if (parameters.TryGetValue("max_tokens", out string maxTokensValue) && 
                        int.TryParse(maxTokensValue, out int tokens))
                    {
                        _llmCharacter.numPredict = tokens;
                    }
                }
                else
                {
                    // Use default parameters
                    _llmCharacter.temperature = _temperature;
                    _llmCharacter.numPredict = _maxTokens;
                }
                
                // Get response from LLMCharacter
                string response = await _llmCharacter.Complete(prompt);
                
                return response?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting LLM response: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets embeddings for text from the LLM
        /// </summary>
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("LLMUnityProvider is not initialized");
            }
            
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Text cannot be empty", nameof(text));
            }
            
            // Check cache first
            string cacheKey = text.GetHashCode().ToString();
            if (_embeddingCache.TryGetValue(cacheKey, out float[] cachedEmbedding))
            {
                return cachedEmbedding;
            }
            
            try
            {
                // Get embeddings from LLMCharacter
                List<float> embeddings = await _llmCharacter.Embeddings(text);
                float[] result = embeddings?.ToArray() ?? new float[0];
                
                // Add to cache
                if (result.Length > 0)
                {
                    // Add to cache, potentially removing old entries if cache is full
                    if (_embeddingCache.Count >= _maxCacheSize)
                    {
                        // Simple strategy: remove random entry
                        var keyToRemove = _embeddingCache.Keys.First();
                        _embeddingCache.Remove(keyToRemove);
                    }
                    
                    _embeddingCache[cacheKey] = result;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting embeddings: {ex.Message}");
                throw;
            }
        }
    }
}