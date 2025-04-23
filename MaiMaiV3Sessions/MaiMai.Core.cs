using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MaiMai.Core
{
    /// <summary>
    /// Represents a message in a conversation
    /// </summary>
    [Serializable]
    public class MessageEntry
    {
        public string Sender;
        public string Message;
        public string MessageId;
        public DateTime Timestamp;
        

        public MessageEntry(string sender, string message, string messageId)
        {
            Sender = sender;
            Message = message;
            MessageId = messageId ?? Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Interface for AI agent identity and state
    /// </summary>
    public interface IAIAgent
    {
        string AgentId { get; }
        string AgentName { get; }
        AgentState State { get; }
        
        // Core interaction
        Task<string> Interact(string userId, string message, string context = "");
        void SetSystemPrompt(string prompt);
        
        // State management
        Task Initialize();
        Task Shutdown();
        event Action<AgentState> OnStateChanged;
    }

    /// <summary>
    /// Defines the possible states of an AI agent
    /// </summary>
    public enum AgentState
    {
        Uninitialized,
        Initializing,
        Ready,
        Processing,
        Error,
        Shutdown
    }

    /// <summary>
    /// Interface for memory management
    /// </summary>
    public interface IMemoryProvider
    {
        // Store information
        Task StoreMessageAsync(MessageEntry message, string agentId);
        Task StoreFixedMemoryAsync(string content, string agentId);
        
        // Retrieve information
        Task<string> RetrieveContextAsync(string query, string agentId, int resultCount = 3);
        Task<List<MessageEntry>> GetRecentConversationAsync(string userId, string agentId, int maxMessages = 10);
        
        // Memory management
        Task SaveMemoryStateAsync(string agentId);
        Task LoadMemoryStateAsync(string agentId);
        Task ClearMemoryAsync(string agentId);
    }

    /// <summary>
    /// Interface for LLM interaction
    /// </summary>
    public interface ILLMProvider
    {
        // Core LLM functionality
        Task<string> GetResponseAsync(string prompt, Dictionary<string, string> parameters = null);
        Task<float[]> GetEmbeddingAsync(string text);
        
        // Management
        Task Initialize();
        Task<bool> IsReady();
        Task Shutdown();
    }

    /// <summary>
    /// Interface for building context for LLM interactions
    /// </summary>
    public interface IContextBuilder
    {
        Task<string> BuildContextAsync(
            string agentName,
            string systemPrompt,
            string userId,
            string userInput,
            List<MessageEntry> recentConversation,
            string retrievedContext,
            string fixedContext);
    }

    /// <summary>
    /// Interface for agent persistence
    /// </summary>
    public interface IPersistenceProvider
    {
        Task SaveAgentAsync(IAIAgent agent);
        Task<IAIAgent> LoadAgentAsync(string agentId);
        Task<List<IAIAgent>> LoadAllAgentsAsync();
        Task SaveManifestAsync(List<IAIAgent> agents);
    }

    /// <summary>
    /// Interface for UI interaction with AI agents
    /// </summary>
    public interface IDialogueUI
    {
        void ShowMessage(string message, string sender);
        void ShowInputField(bool show);
        void SetAgentName(string agentName);
        void SetProcessingState(bool isProcessing);
        
        event Action<string> OnUserMessageSubmitted;
    }

    /// <summary>
    /// Interface for managing all AI agents in the game
    /// </summary>
    public interface IAIAgentManager
    {
        Task<IAIAgent> CreateAgent(string agentName, string systemPrompt);
        IAIAgent GetAgentById(string agentId);
        IAIAgent GetAgentByName(string agentName);
        List<IAIAgent> GetAllAgents();
        void RegisterAgent(IAIAgent agent);
        void UnregisterAgent(string agentId);
    }
}