using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using MaiMai.Core;

namespace MaiMai.Implementation
{
    /// <summary>
    /// Manages all AI agents in the game
    /// </summary>
    public class AIAgentManager : MonoBehaviour, IAIAgentManager
    {
        // Singleton pattern
        private static AIAgentManager _instance;
        public static AIAgentManager Instance => _instance;
        
        // Dependencies
        [SerializeField] private LLMUnity.LLMCharacter _llmCharacter;
        
        // Services
        private ILLMProvider _llmProvider;
        private IMemoryProvider _memoryProvider;
        private IContextBuilder _contextBuilder;
        private IPersistenceProvider _persistenceProvider;
        
        // Agent storage
        private Dictionary<string, IAIAgent> _agentsById = new Dictionary<string, IAIAgent>();
        private Dictionary<string, IAIAgent> _agentsByName = new Dictionary<string, IAIAgent>();
        
        // State tracking
        private bool _isInitialized = false;
        private TaskCompletionSource<bool> _initializationTask;
        
        [Header("Settings")]
        [SerializeField] private bool _loadAgentsOnStart = true;
        [SerializeField] private bool _createDefaultAgents = false;
        [Tooltip("Always load from persistent storage even if agents exist in memory")]
        [SerializeField] private bool _alwaysReloadFromDisk = false;

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _initializationTask = new TaskCompletionSource<bool>();
        }
        
        private async void Start()
        {
            try
            {
                await InitializeServices();
                
                if (_loadAgentsOnStart)
                {
                    await LoadAllAgents();
                }
                
                if (_createDefaultAgents && _agentsById.Count == 0)
                {
                    await CreateDefaultAgents();
                }
                
                _isInitialized = true;
                _initializationTask.SetResult(true);
                
                Debug.Log("AIAgentManager initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize AIAgentManager: {ex.Message}");
                _initializationTask.SetException(ex);
            }
        }
        
        /// <summary>
        /// Initializes required services for the agent manager
        /// </summary>
        private async Task InitializeServices()
        {
            if (_llmCharacter == null)
            {
                _llmCharacter = FindObjectOfType<LLMUnity.LLMCharacter>();
                
                if (_llmCharacter == null)
                {
                    throw new InvalidOperationException("LLMCharacter not found in scene");
                }
            }
            
            // Create providers
            _llmProvider = new LLMUnityProvider(_llmCharacter);
            _memoryProvider = new RAGMemoryProvider(_llmProvider);
            _contextBuilder = new DefaultContextBuilder();
            _persistenceProvider = new JsonPersistenceProvider(
                _llmProvider, 
                _memoryProvider, 
                _contextBuilder);
                
            // Initialize LLM
            await _llmProvider.Initialize();
            
            if (!await _llmProvider.IsReady())
            {
                throw new InvalidOperationException("LLM provider failed to initialize");
            }
        }
        
        /// <summary>
        /// Ensures the manager is initialized before performing operations
        /// </summary>
        private async Task EnsureInitialized()
        {
            if (!_isInitialized)
            {
                await _initializationTask.Task;
            }
        }
        
        /// <summary>
        /// Creates default agents if no agents exist
        /// </summary>
        private async Task CreateDefaultAgents()
        {
            Debug.Log("Creating default agents...");
            
            await CreateAgent("Guide", "You are a helpful guide who assists players with game mechanics, quests, and navigation. You are friendly, patient, and knowledgeable about the game world.");
            await CreateAgent("Merchant", "You are a merchant who buys and sells items. You're business-minded but fair, always looking for a good deal. You know the value of rare items and can provide information about equipment.");
            
            Debug.Log("Default agents created");
        }

        /// <summary>
        /// Creates a new AI agent with the specified parameters
        /// </summary>
        public async Task<IAIAgent> CreateAgent(string agentName, string systemPrompt)
        {
            await EnsureInitialized();
            
            if (string.IsNullOrEmpty(agentName))
            {
                throw new ArgumentException("Agent name cannot be empty", nameof(agentName));
            }
            
            if (string.IsNullOrEmpty(systemPrompt))
            {
                throw new ArgumentException("System prompt cannot be empty", nameof(systemPrompt));
            }
            
            // Check for name collision
            if (_agentsByName.ContainsKey(agentName))
            {
                throw new InvalidOperationException($"An agent with the name '{agentName}' already exists");
            }
            
            try
            {
                // Create agent
                string agentId = Guid.NewGuid().ToString();
                var agent = new AIAgent(
                    agentId,
                    agentName,
                    systemPrompt,
                    _llmProvider,
                    _memoryProvider,
                    _contextBuilder);
                
                // Initialize agent
                await agent.Initialize();
                
                // Register agent
                RegisterAgent(agent);
                
                // Save agent
                await _persistenceProvider.SaveAgentAsync(agent);
                await _persistenceProvider.SaveManifestAsync(GetAllAgents());
                
                Debug.Log($"Agent '{agentName}' created successfully");
                return agent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create agent '{agentName}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets an agent by its unique ID
        /// </summary>
        public IAIAgent GetAgentById(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                return null;
            }
            
            _agentsById.TryGetValue(agentId, out IAIAgent agent);
            return agent;
        }

        /// <summary>
        /// Gets an agent by its display name
        /// </summary>
        public IAIAgent GetAgentByName(string agentName)
        {
            if (string.IsNullOrEmpty(agentName))
            {
                return null;
            }
            
            _agentsByName.TryGetValue(agentName, out IAIAgent agent);
            return agent;
        }

        /// <summary>
        /// Gets all registered agents
        /// </summary>
        public List<IAIAgent> GetAllAgents()
        {
            return new List<IAIAgent>(_agentsById.Values);
        }

        /// <summary>
        /// Registers an agent with the manager
        /// </summary>
        public void RegisterAgent(IAIAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }
            
            // Register by ID
            _agentsById[agent.AgentId] = agent;
            
            // Register by name (with collision handling)
            if (_agentsByName.ContainsKey(agent.AgentName))
            {
                Debug.LogWarning($"Multiple agents with name '{agent.AgentName}' found. Only the most recent will be accessible by name.");
            }
            
            _agentsByName[agent.AgentName] = agent;
            
            Debug.Log($"Agent '{agent.AgentName}' (ID: {agent.AgentId}) registered");
        }

        /// <summary>
        /// Unregisters an agent from the manager
        /// </summary>
        public void UnregisterAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                return;
            }
            
            if (_agentsById.TryGetValue(agentId, out IAIAgent agent))
            {
                _agentsById.Remove(agentId);
                
                // Remove from name dictionary only if it's the same agent
                if (_agentsByName.TryGetValue(agent.AgentName, out IAIAgent namedAgent) && 
                    namedAgent.AgentId == agentId)
                {
                    _agentsByName.Remove(agent.AgentName);
                }
                
                Debug.Log($"Agent '{agent.AgentName}' (ID: {agentId}) unregistered");
            }
        }
        
        /// <summary>
        /// Loads all agents from persistence
        /// </summary>
        public async Task LoadAllAgents()
        {
            await EnsureInitialized();
            
            try
            {
                if (_agentsById.Count > 0 && !_alwaysReloadFromDisk)
                {
                    Debug.Log($"Using {_agentsById.Count} agents already in memory");
                    return;
                }
                
                // Clear existing agents if reloading
                if (_alwaysReloadFromDisk)
                {
                    _agentsById.Clear();
                    _agentsByName.Clear();
                }
                
                // Load agents from persistence
                List<IAIAgent> agents = await _persistenceProvider.LoadAllAgentsAsync();
                
                // Register each agent
                foreach (var agent in agents)
                {
                    RegisterAgent(agent);
                }
                
                Debug.Log($"Loaded {agents.Count} agents from persistent storage");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load agents: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Save specific agent to persistence
        /// </summary>
        public async Task SaveAgent(string agentId)
        {
            await EnsureInitialized();
            
            if (string.IsNullOrEmpty(agentId))
            {
                throw new ArgumentException("Agent ID cannot be empty", nameof(agentId));
            }
            
            if (!_agentsById.TryGetValue(agentId, out IAIAgent agent))
            {
                throw new InvalidOperationException($"Agent with ID '{agentId}' not found");
            }
            
            try
            {
                await _persistenceProvider.SaveAgentAsync(agent);
                Debug.Log($"Agent '{agent.AgentName}' (ID: {agentId}) saved to persistence");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save agent '{agent.AgentName}': {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Save all agents to persistence
        /// </summary>
        public async Task SaveAllAgents()
        {
            await EnsureInitialized();
            
            try
            {
                List<IAIAgent> agents = GetAllAgents();
                
                foreach (var agent in agents)
                {
                    await _persistenceProvider.SaveAgentAsync(agent);
                }
                
                await _persistenceProvider.SaveManifestAsync(agents);
                Debug.Log($"Saved {agents.Count} agents to persistence");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save all agents: {ex.Message}");
                throw;
            }
        }
        
        private void OnApplicationQuit()
        {
            // Save all agents when the application quits
            _ = SaveAllAgents();
        }
    }
}