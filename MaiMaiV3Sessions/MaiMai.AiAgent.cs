using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
using MaiMai.Core;

namespace MaiMai.Implementation
{
    /// <summary>
    /// Core implementation of an AI agent in the MaiMai system
    /// </summary>
    public class AIAgent : IAIAgent
    {
        // Properties from interface
        public string AgentId { get; private set; }
        public string AgentName { get; private set; }
        public AgentState State { get; private set; } = AgentState.Uninitialized;
        public event Action<AgentState> OnStateChanged;

        // Implementation specifics
        private readonly ILLMProvider _llmProvider;
        private readonly IMemoryProvider _memoryProvider;
        private readonly IContextBuilder _contextBuilder;
        
        // Configuration
        public string SystemPrompt { get; private set; }
        
        // Rate limiting and concurrency control
        private SemaphoreSlim _interactionLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        // Conversation tracking
        private Dictionary<string, List<MessageEntry>> _userConversations = 
            new Dictionary<string, List<MessageEntry>>();

        /// <summary>
        /// Creates a new AI agent with the specified parameters
        /// </summary>
        public AIAgent(
            string agentId, 
            string agentName, 
            string systemPrompt,
            ILLMProvider llmProvider,
            IMemoryProvider memoryProvider,
            IContextBuilder contextBuilder)
        {
            AgentId = agentId ?? Guid.NewGuid().ToString();
            AgentName = agentName ?? "Assistant";
            SystemPrompt = systemPrompt ?? "You are a helpful assistant.";
            
            _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
            _memoryProvider = memoryProvider ?? throw new ArgumentNullException(nameof(memoryProvider));
            _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
        }

        private void SetState(AgentState newState)
        {
            if (State == newState) return;
            
            State = newState;
            OnStateChanged?.Invoke(newState);
            Debug.Log($"Agent {AgentName} state changed to {newState}");
        }

        /// <summary>
        /// Initializes the agent and prepares it for interactions
        /// </summary>
        public async Task Initialize()
        {
            if (State != AgentState.Uninitialized)
            {
                Debug.LogWarning($"Agent {AgentName} is already initialized or being initialized");
                return;
            }

            SetState(AgentState.Initializing);
            
            try
            {
                // Wait for LLM to be ready
                await _llmProvider.Initialize();
                bool isReady = await _llmProvider.IsReady();
                
                if (!isReady)
                {
                    throw new Exception("LLM provider failed to initialize");
                }
                
                // Load existing memory state if available
                await _memoryProvider.LoadMemoryStateAsync(AgentId);
                
                SetState(AgentState.Ready);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize agent {AgentName}: {ex.Message}");
                SetState(AgentState.Error);
                throw;
            }
        }

        /// <summary>
        /// Properly shuts down the agent, saving its state
        /// </summary>
        public async Task Shutdown()
        {
            if (State == AgentState.Shutdown)
            {
                return;
            }

            try
            {
                // Cancel any ongoing operations
                _cancellationTokenSource.Cancel();
                
                // Save the memory state
                await _memoryProvider.SaveMemoryStateAsync(AgentId);
                
                SetState(AgentState.Shutdown);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during agent shutdown: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sets or updates the system prompt for this agent
        /// </summary>
        public void SetSystemPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("System prompt cannot be empty", nameof(prompt));
            }
            
            SystemPrompt = prompt;
            Debug.Log($"Updated system prompt for agent {AgentName}");
        }

        /// <summary>
        /// Process a user message and generate an AI response
        /// </summary>
        public async Task<string> Interact(string userId, string message, string context = "")
        {
            if (State != AgentState.Ready)
            {
                throw new InvalidOperationException($"Agent {AgentName} is not ready for interaction (current state: {State})");
            }
            
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = "default_user";
            }
            
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(message));
            }

            await _interactionLock.WaitAsync();
            SetState(AgentState.Processing);
            
            try
            {
                // Track the user's message
                var userMessage = new MessageEntry("User", message, null);
                await StoreMessage(userId, userMessage);
                
                // Retrieve conversation context
                var recentConversation = await _memoryProvider.GetRecentConversationAsync(userId, AgentId);
                string retrievedContext = await _memoryProvider.RetrieveContextAsync(message, AgentId);
                
                // Build the full context for the LLM
                string fullContext = await _contextBuilder.BuildContextAsync(
                    AgentName,
                    SystemPrompt,
                    userId,
                    message,
                    recentConversation,
                    retrievedContext,
                    context);
                
                // Get response from LLM
                string response = await _llmProvider.GetResponseAsync(fullContext);
                
                if (string.IsNullOrEmpty(response))
                {
                    Debug.LogWarning($"Agent {AgentName} received empty response from LLM");
                    response = "I'm sorry, I wasn't able to generate a response. Please try again.";
                }
                
                // Store the AI's response
                var aiMessage = new MessageEntry(AgentName, response, null);
                await StoreMessage(userId, aiMessage);
                
                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during agent interaction: {ex.Message}");
                SetState(AgentState.Error);
                throw;
            }
            finally
            {
                SetState(AgentState.Ready);
                _interactionLock.Release();
            }
        }
        
        private async Task StoreMessage(string userId, MessageEntry message)
        {
            // Update in-memory conversation history
            if (!_userConversations.ContainsKey(userId))
            {
                _userConversations[userId] = new List<MessageEntry>();
            }
            _userConversations[userId].Add(message);
            
            // Store in the memory system
            await _memoryProvider.StoreMessageAsync(message, AgentId);
        }
    }
}