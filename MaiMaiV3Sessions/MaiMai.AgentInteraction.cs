using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using MaiMai.Core;
using MaiMai.Implementation;
using LLMUnity;

namespace MaiMai.Integration
{
    /// <summary>
    /// Manages the interaction between players and AI agents in the game world
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AgentInteractionController : MonoBehaviour
    {
        [Header("Agent Configuration")]
        [SerializeField] private string _agentId;
        [SerializeField] private string _agentName;
        [Tooltip("If both ID and name are provided, ID takes precedence")]
        
        [Header("Fixed Agent Settings")]
        [SerializeField] private bool _isFixedAgent = false;
        [SerializeField] private string _fixedAgentName;
        [TextArea(3, 5)]
        [SerializeField] private string _fixedAgentPrompt;
        
        [Header("Fixed Memory")]
        [SerializeField] private bool _hasFixedMemory = false;
        [SerializeField] private TextAsset[] _memoryFiles;
        
        [Header("Interaction Settings")]
        [SerializeField] private InteractionMode _interactionMode = InteractionMode.Proximity;
        [SerializeField] private KeyCode _interactionKey = KeyCode.E;
        [SerializeField] private float _interactionRadius = 2f;
        [SerializeField] private bool _showInteractionPrompt = true;
        [SerializeField] private string _interactionPromptText = "Press {0} to talk";

        [SerializeField] private ChatSessionManager sessionManager;
        [SerializeField] private ExtendedDialogueUIManager uiManager;

        // Private state
        private IAIAgent _assignedAgent;
        private IDialogueUI _dialogueUI;
        private bool _playerInRange = false;
        private GameObject _player;
        
        // Dependencies
        private AIAgentManager _agentManager;
        private DialogueUIManager _uiManager;
        public LLMCharacter llmCharacter;

        public enum InteractionMode
        {
            Proximity,  // Automatically interact when player enters trigger
            KeyTrigger   // Interact when player presses key while in trigger
        }
        
        private void Start()
        {
            StartCoroutine(InitializeWithRetry());
        }

        /// <summary>
        /// Initialize components with retry to wait for managers to be ready
        /// </summary>
        private IEnumerator InitializeWithRetry()
        {
            int retryCount = 0;
            int maxRetries = 10;
            float retryDelay = 0.5f;

            while (retryCount < maxRetries)
            {
                bool initialized = false;
                try
                {
                    initialized = Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during initialization attempt {retryCount}: {ex.Message}");
                }

                if (initialized)
                {
                    yield break;
                }

                retryCount++;
                yield return new WaitForSeconds(retryDelay);
            }

            Debug.LogError($"Failed to initialize {gameObject.name} after {maxRetries} attempts");
        }

        /// <summary>
        /// Initialize the component
        /// </summary>
        private bool Initialize()
        {
            // Get agent manager
            _agentManager = AIAgentManager.Instance;
            if (_agentManager == null)
            {
                Debug.LogWarning("AIAgentManager not found. Retrying...");
                return false;
            }
            
            // Get UI manager
            _uiManager = DialogueUIManager.Instance;
            if (_uiManager == null)
            {
                Debug.LogWarning("DialogueUIManager not found. Retrying...");
                return false;
            }
            
            // Set up dialogue UI
            _dialogueUI = _uiManager as IDialogueUI;
            if (_dialogueUI != null)
            {
                _dialogueUI.OnUserMessageSubmitted += HandleUserMessage;
            }
            
            // Configure agent
            if (_isFixedAgent)
            {
                StartCoroutine(InitializeFixedAgent());
            }
            else
            {
                FindExistingAgent();
            }
            
            // Configure trigger collider
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            else
            {
                Debug.LogError("No Collider component found on this GameObject");
            }
            
            return true;
        }
        
        /// <summary>
        /// Find an existing agent by ID or name
        /// </summary>
        private void FindExistingAgent()
        {
            if (!string.IsNullOrEmpty(_agentId))
            {
                _assignedAgent = _agentManager.GetAgentById(_agentId);
                
                if (_assignedAgent != null)
                {
                    Debug.Log($"Found agent by ID: {_assignedAgent.AgentName}");
                    return;
                }
                
                Debug.LogWarning($"Agent with ID {_agentId} not found");
            }
            
            if (!string.IsNullOrEmpty(_agentName))
            {
                _assignedAgent = _agentManager.GetAgentByName(_agentName);
                
                if (_assignedAgent != null)
                {
                    Debug.Log($"Found agent by name: {_assignedAgent.AgentName}");
                    return;
                }
                
                Debug.LogWarning($"Agent with name {_agentName} not found");
            }
        }

        /// <summary>
        /// Create a fixed agent for this interaction
        /// </summary>
        private async Task InitializeFixedAgentAsync()
        {
            if (string.IsNullOrEmpty(_fixedAgentName))
            {
                Debug.LogError("Fixed agent name is empty");
                return;
            }

            if (string.IsNullOrEmpty(_fixedAgentPrompt))
            {
                Debug.LogError("Fixed agent prompt is empty");
                return;
            }

            // Check if agent already exists
            _assignedAgent = _agentManager.GetAgentByName(_fixedAgentName);

            if (_assignedAgent != null)
            {
                Debug.Log($"Using existing agent '{_fixedAgentName}'");

                if (_hasFixedMemory)
                {
                    await LoadFixedMemoryAsync();
                }

                return;
            }

            // Create the agent
            try
            {
                // Wait for agent manager to be fully initialized
                // Convert this to a coroutine-compatible approach
                while (_agentManager.GetAllAgents() == null)
                {
                    await Task.Delay(500);
                }

                _assignedAgent = await _agentManager.CreateAgent(_fixedAgentName, _fixedAgentPrompt);
                Debug.Log($"Created fixed agent '{_fixedAgentName}'");

                if (_hasFixedMemory)
                {
                    await LoadFixedMemoryAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create fixed agent: {ex.Message}");
            }
        }

        // Create a wrapper coroutine method
        private IEnumerator InitializeFixedAgent()
        {
            var task = InitializeFixedAgentAsync();
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogError($"Error in InitializeFixedAgentAsync: {task.Exception}");
            }
        }

        /// <summary>
        /// Load fixed memory content into the agent's memory system
        /// </summary>
        private async Task LoadFixedMemoryAsync()
        {
            if (_memoryFiles == null || _memoryFiles.Length == 0)
            {
                Debug.LogWarning("No memory files provided");
                return;
            }

            if (_assignedAgent == null)
            {
                Debug.LogError("Cannot load memory: No agent assigned");
                return;
            }

            // This requires access to the memory provider, we'll use reflection for this example
            // In a real implementation, you might expose this functionality directly
            try
            {
                IMemoryProvider memoryProvider = _agentManager
                    .GetType()
                    .GetField("_memoryProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_agentManager) as IMemoryProvider;

                if (memoryProvider == null)
                {
                    Debug.LogError("Unable to access memory provider");
                    return;
                }

                foreach (var file in _memoryFiles)
                {
                    if (file != null)
                    {
                        string content = file.text;
                        await memoryProvider.StoreFixedMemoryAsync(content, _assignedAgent.AgentId);
                        Debug.Log($"Loaded memory file '{file.name}' for agent '{_assignedAgent.AgentName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load fixed memory: {ex.Message}");
            }
        }

        // Create a wrapper coroutine method
        private IEnumerator LoadFixedMemory()
        {
            var task = LoadFixedMemoryAsync();
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogError($"Error in LoadFixedMemoryAsync: {task.Exception}");
            }
        }

        private void Update()
        {
            // Handle key press interaction
            if (_interactionMode == InteractionMode.KeyTrigger && 
                _playerInRange && 
                Input.GetKeyDown(_interactionKey))
            {
                TriggerInteraction();
            }
            
            // Show interaction prompt
            if (_showInteractionPrompt && _playerInRange)
            {
                // Implementation depends on UI system
                // Example: Display floating text above NPC
            }
        }

        /// <summary>
        /// Trigger the interaction with the agent
        /// </summary>
        private async void TriggerInteraction()
        {
            Debug.Log("TriggerInteraction called");

            // Try to wait for agent initialization to complete
            int retryCount = 0;
            while (_assignedAgent == null && retryCount < 5)
            {
                Debug.Log($"Agent not ready yet, waiting... (attempt {retryCount + 1})");
                await Task.Delay(500); // Wait half a second
                retryCount++;

                // Try to find existing agent again if we're still null
                if (_assignedAgent == null && !string.IsNullOrEmpty(_fixedAgentName))
                {
                    _assignedAgent = _agentManager.GetAgentByName(_fixedAgentName);
                }
            }

            Debug.Log("Assigned Agent: " + (_assignedAgent != null ? _assignedAgent.AgentName : "NULL"));
            Debug.Log("DialogueUIManager Instance: " + (DialogueUIManager.Instance != null ? "EXISTS" : "NULL"));

            if (_assignedAgent == null)
            {
                Debug.LogWarning("No agent assigned for interaction");
                return;
            }

            // Set the current interacting agent AFTER confirming it's not null
            DialogueUIManager.CurrentInteractingAgentName = _assignedAgent.AgentName;
            DialogueUIManager.Instance.SetAgentName(_assignedAgent.AgentName);
            DialogueUIManager.Instance.ShowDialogue();
        }

        /// <summary>
        /// Handle a user message submission

        private async void HandleUserMessage(string message)
        {
            if (_assignedAgent == null || string.IsNullOrEmpty(message))
                return;

            try
            {
                // Show user message in UI
                uiManager.ShowMessage(message, "User");

                // Set UI to processing state
                uiManager.SetProcessingState(true);

                // Get response from agent
                string response = await _assignedAgent.Interact(GetUserId(), message);

                // Show agent response in UI
                uiManager.ShowMessage(response, _assignedAgent.AgentName);

                // If you need to manually save (though your AddMessageToCurrentSession already does this)
                if (sessionManager.CurrentSessionId != null)
                    await llmCharacter.Save($"chat_{sessionManager.CurrentSessionId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during agent interaction: {ex.Message}");
                uiManager.ShowMessage("Sorry, I'm having trouble responding right now.", _assignedAgent.AgentName);
            }
            finally
            {
                // Reset UI state
                uiManager.SetProcessingState(false);
            }
        }


        /// <summary>
        /// Get a unique ID for the current user/player
        /// </summary>
        private string GetUserId()
        {
            if (_player != null)
            {
                // Try to use player name or ID
                return _player.name;
            }
            
            return "Player";
        }

        /// <summary>
        /// Handle player entering the interaction zone
        /// </summary>
        /// 
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Trigger entered by: " + other.gameObject.name);
            Debug.Log("Has Player tag: " + other.CompareTag("Player"));

            if (other.CompareTag("Player"))
            {
                _player = other.gameObject;
                _playerInRange = true;

                Debug.Log("Player in range for interaction");

                if (_interactionMode == InteractionMode.Proximity)
                {
                    Debug.Log("Attempting to trigger interaction");
                    TriggerInteraction();
                }
            }
        }

        /// <summary>
        /// Handle player exiting the interaction zone
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInRange = false;

                // Re-enable the player controller
                if (_player != null)
                {
                    MonoBehaviour playerController = _player.GetComponent<MonoBehaviour>();
                    if (playerController != null)
                    {
                        playerController.enabled = true;
                    }
                }

                _player = null;

                // Hide dialogue if using proximity mode
                if (_interactionMode == InteractionMode.Proximity)
                {
                    (_dialogueUI as DialogueUIManager)?.HideDialogue();
                }
            }
        }

        /// <summary>
        /// Set the agent for this interaction component
        /// </summary>
        public void SetAgent(IAIAgent agent)
        {
            if (agent == null)
            {
                Debug.LogError("Cannot set null agent");
                return;
            }
            
            _assignedAgent = agent;
            _agentId = agent.AgentId;
            _agentName = agent.AgentName;
            
            Debug.Log($"Agent '{agent.AgentName}' assigned to {gameObject.name}");
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_dialogueUI != null)
            {
                _dialogueUI.OnUserMessageSubmitted -= HandleUserMessage;
            }
        }
    }
}