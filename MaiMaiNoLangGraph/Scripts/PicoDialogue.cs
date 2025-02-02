// PicoDialogue.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using LLMUnity;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using UnityEngine.SceneManagement;

public class PicoDialogue : MonoBehaviour
{
    [Header("LLMUnity Integration")]
    public LLMCharacter llmCharacter; // Reference to the LLMCharacter component from LLMUnity

    public bool isAwaitingResponse = false;
    public static PicoDialogue Instance { get; private set; }

    private string currentPlayerName;
    private GameObject player; // Reference to the player GameObject

    private bool isLLMInitialized = false;
    private CancellationTokenSource cancellationTokenSource; // Declare it a



    [Header("UI Elements")]
    public Canvas dialogueCanvas;
    public Text dialogueText; // UnityEngine.UI.Text for dialogue UI
    public GameObject playerInputPanel;
    public TMP_InputField playerInputField; // TMPro.TMP_InputField for player input
    public Button sendButton;
    public Toggle voiceToggleButton; // Toggle to enable/disable voice
    public Text floatingDialogueText; // UnityEngine.UI.Text for floating dialogue

    public AIAgent currentAgent; // Track the currently active agent

    [Header("NPC Configurations")]
    public string npcName;
    public string npcPersonality;
    public string npcBackground;

  
    private string userId = string.Empty; // Player's unique ID assigned 
    private bool isConnected = false; // Flag to check if connected
    private string generatedUserId; // Generated User ID using GUID

#if UNITY_ANDROID || UNITY_IOS
    private TextToSpeechController ttsController;
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scene loads

            // Subscribe to the sceneLoaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene Loaded: {scene.name}");
        InitializeReferences(); // Re-initialize references to UI elements
    }

    void Start()
    {
        // Validate UI Assignments
        ValidateUIElements();

        // Call this on scene load to reinitialize everything
        InitializeReferences();

        cancellationTokenSource = new CancellationTokenSource();


        // Initialize voice toggle button
        InitializeVoiceToggle();

        // Get or assign llmCharacter
        if (llmCharacter == null)
        {
            llmCharacter = GetComponent<LLMCharacter>();
            if (llmCharacter == null)
            {
                Debug.LogError("LLMCharacter component is missing on PicoDialogue.");
            }
        }
#if UNITY_ANDROID || UNITY_IOS
            // Initialize Text-to-Speech
            InitializeTextToSpeech();
#endif

    
     


        // Start async initialization
        InitializeLLMService();

        DisableUIForInitialization();

        // Hide UI elements at the start
        HideUIElements();

        // Add listener to the send button
        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        // Ensure fonts are assigned correctly
        AssignFonts();

        Debug.Log("PicoDialogue system initialized.");
    }



    void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this; // Reassign singleton instance if needed
        }


        InitializeReferences(); // Ensure everything is reinitialized

        if (sendButton != null)
            sendButton.interactable = true; // Re-enable input if it was disabled
    }



    private void HandleLLMResponse(string aiResponse)
    {
        // Since this might be called from a background thread, ensure it's executed on the main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (string.IsNullOrEmpty(aiResponse))
            {
                Debug.LogWarning("AI returned an empty response.");
                ShowDialogue("Sorry, I couldn't generate a response.");
            }
            else
            {
                Debug.Log($"Received response from LLM: {aiResponse}");

                // Send response to chat and dialogue UI
                
                ShowDialogue(aiResponse);
            }

            isAwaitingResponse = false; // Mark the system as free for another request
            sendButton.interactable = true; // Re-enable the send button
        });
    }



    public void InitializeReferences()
    {
        // Ensure UI elements are assigned again
        if (dialogueCanvas == null)
        {
            GameObject canvasGO = GameObject.Find("DialogueCanvas");
            if (canvasGO != null)
            {
                dialogueCanvas = canvasGO.GetComponent<Canvas>();
                Debug.Log("Dialogue Canvas re-initialized.");
            }
            else
            {
                Debug.LogError("Dialogue Canvas not found in the scene.");
            }
        }

        // Reinitialize other references like llmCharacter
        if (llmCharacter == null)
        {
            GameObject dialogueELLMCharacterGO = GameObject.Find("DialogueELLMCHARACTER");
            if (dialogueELLMCharacterGO != null)
            {
                llmCharacter = dialogueELLMCharacterGO.GetComponent<LLMCharacter>();
                if (llmCharacter == null)
                {
                    Debug.LogError("LLMCharacter component is missing.");
                }
                else
                {
                    Debug.Log("LLMCharacter re-initialized successfully.");
                }
            }
            else
            {
                Debug.LogError("DialogueELLMCHARACTER GameObject not found.");
            }
        }
        else
        {
            Debug.Log("LLMCharacter is already initialized.");
        }

        // Re-initialize other critical components as needed
        // For example, re-assign playerInputPanel, playerInputField, sendButton, etc.
        if (playerInputPanel == null)
        {
            playerInputPanel = GameObject.Find("PlayerInputPanel");
            if (playerInputPanel != null)
            {
                Debug.Log("Player Input Panel re-initialized.");
            }
            else
            {
                Debug.LogError("Player Input Panel not found in the scene.");
            }
        }

        if (playerInputField == null)
        {
            GameObject inputFieldGO = GameObject.Find("PlayerInputField");
            if (inputFieldGO != null)
            {
                playerInputField = inputFieldGO.GetComponent<TMP_InputField>();
                Debug.Log("Player Input Field re-initialized.");
            }
            else
            {
                Debug.LogError("Player Input Field not found in the scene.");
            }
        }

        if (sendButton == null)
        {
            GameObject sendButtonGO = GameObject.Find("SendButton");
            if (sendButtonGO != null)
            {
                sendButton = sendButtonGO.GetComponent<Button>();
                Debug.Log("Send Button re-initialized.");
            }
            else
            {
                Debug.LogError("Send Button not found in the scene.");
            }
        }

        // Re-assign event listeners if necessary
        if (sendButton != null)
        {
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        if (playerInputPanel != null)
        {
            playerInputPanel.SetActive(false); // Hide input panel initially
        }

        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false); // Hide dialogue canvas initially
        }

        // Similarly re-initialize other UI elements like dialogueText, floatingDialogueText, etc.

        // Ensure fonts are assigned correctly
        AssignFonts();
    }


    private async void InitializeLLMService()
    {
        if (isLLMInitialized)
        {
            Debug.Log("LLM service already initialized.");
            return;
        }

        await EnsureLLMServiceReady();

        EnableUIAfterInitialization();

        isLLMInitialized = true;

        Debug.Log("LLM service is fully initialized.");
    }


    private void DisableUIForInitialization()
    {
        if (playerInputPanel != null)
        {
            playerInputPanel.SetActive(false); // Disable input panel
        }

        if (sendButton != null)
        {
            sendButton.interactable = false; // Disable send button
        }

        Debug.Log("UI disabled during LLM initialization.");
    }

    private void EnableUIAfterInitialization()
    {
        if (playerInputPanel != null)
        {
            playerInputPanel.SetActive(true); // Enable input panel
        }
        else
        {
            Debug.LogWarning("PlayerInputPanel is not initialized.");
        }

        if (sendButton != null)
        {
            sendButton.interactable = true; // Enable send button
        }
        else
        {
            Debug.LogWarning("SendButton is not initialized.");
        }

        Debug.Log("UI enabled after LLM initialization.");
    }



    // Method to validate UI elements
    private void ValidateUIElements()
    {
        if (dialogueCanvas == null)
        {
            Debug.LogError("Dialogue Canvas is not assigned in the Inspector.");
            // Add the following lines to attempt automatic assignment
            GameObject canvasGO = GameObject.Find("DialogueCanvas"); // Ensure the Canvas GameObject is named "DialogueCanvas"
            if (canvasGO != null)
            {
                dialogueCanvas = canvasGO.GetComponent<Canvas>();
                Debug.Log("Dialogue Canvas automatically assigned via GameObject.Find.");
            }
            else
            {
                Debug.LogError("PicoDialogue: Dialogue Canvas GameObject 'DialogueCanvas' not found in the scene.");
            }
        }
        if (dialogueText == null)
        {
            Debug.LogError("Dialogue Text is not assigned in the Inspector.");
        }
        if (playerInputPanel == null)
        {
            Debug.LogError("Player Input Panel is not assigned in the Inspector.");
        }
        if (playerInputField == null)
        {
            Debug.LogError("Player Input Field is not assigned in the Inspector.");
        }
        if (sendButton == null)
        {
            Debug.LogError("Send Button is not assigned in the Inspector.");
        }
        if (floatingDialogueText == null)
        {
            Debug.LogError("Floating Dialogue Text is not assigned in the Inspector.");
        }
        if (voiceToggleButton == null)
        {
            Debug.LogError("Voice Toggle Button is not assigned in the Inspector.");
        }
    }


    // Initialize voice toggle button
    private void InitializeVoiceToggle()
    {
        if (voiceToggleButton != null)
        {
            voiceToggleButton.isOn = GameSettings.Instance != null && GameSettings.Instance.isVoiceEnabled;
            voiceToggleButton.onValueChanged.RemoveAllListeners();
            voiceToggleButton.onValueChanged.AddListener(OnVoiceToggleClicked);
        }
    }

#if UNITY_ANDROID || UNITY_IOS
    // Initialize Text-to-Speech
    private void InitializeTextToSpeech()
    {
        ttsController = TextToSpeechController.Instance;
        ttsController.Setup("en-US", 1f, 1f); // Locale, Pitch, Rate

        if (voiceToggleButton != null)
        {
            voiceToggleButton.onValueChanged.RemoveAllListeners();
            voiceToggleButton.onValueChanged.AddListener(OnVoiceToggleClicked);
        }
        else
        {
            Debug.LogError("Voice Toggle Button is not assigned in the Inspector.");
        }
    }
#endif


    // Hide UI elements at the start
    private void HideUIElements()
    {
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false);
        }

        if (playerInputPanel != null)
        {
            playerInputPanel.SetActive(false);
        }

        if (floatingDialogueText != null)
        {
            floatingDialogueText.gameObject.SetActive(false);
        }
    }

    // Open player input UI when interacting with the NPC
    public void OpenPlayerInputUI()
    {
        Debug.Log("Opening Player Input UI...");

        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(true);

            if (playerInputPanel != null)
            {
                playerInputPanel.SetActive(true);
                playerInputField.text = "";
                playerInputField.ActivateInputField();
            }
            else
            {
                Debug.LogError("Player Input Panel is not assigned in the Inspector.");
            }
        }
        else
        {
            Debug.LogError("Dialogue Canvas is not assigned in the Inspector.");
        }
    }

    public void HidePlayerInputUI()
    {
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false);
            Debug.Log("Player Input UI hidden.");
        }

        if (playerInputPanel != null)
        {
            playerInputPanel.SetActive(false);
            Debug.Log("Player Input Panel hidden.");
        }
    }

    public async void SetAgentData(AIAgent agent)
    {
        if (agent == null)
        {
            Debug.LogError("Attempted to set agent data with a null AIAgent.");
            return;
        }

        currentAgent = agent;
        npcName = currentAgent.AgentName;
        Debug.Log($"Current agent set to: {currentAgent.AgentName}");

        if (agent.llmCharacter != null)
        {
            llmCharacter = agent.llmCharacter;
            llmCharacter.AIName = currentAgent.AgentName; // Ensure AIName is set
            Debug.Log($"LLMCharacter assigned to PicoDialogue from agent '{agent.AgentName}'.");

            // Ensure to load chat history
            string historyFileName = $"{currentAgent.AgentId}_chatHistory";
            await llmCharacter.Load(historyFileName); // Load chat history using LLMCharacter
        }
        else
        {
            Debug.LogError($"No LLMCharacter assigned for agent '{agent.AgentName}'.");
        }

        UpdateAgentUI();
        sendButton.interactable = true;
        await EnsureLLMServiceReady(); // Ensure llm service is ready again
    }


    private void UpdateAgentUI()
    {
        // Example of updating UI elements to reflect the current agent
        if (dialogueText != null)
        {
            dialogueText.text = $"Live";
        }
    }

    // Callback when the player sends a message via the input panel
    public void OnSendButtonClicked()
    {
        if (playerInputField == null)
        {
            Debug.LogError("Player Input Field is not assigned.");
            return;
        }

        if (isAwaitingResponse)
        {
            Debug.LogWarning("Cannot send message. Already awaiting a response.");
            return;
        }

        string playerInput = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(playerInput))
        {
            Debug.LogWarning("Player input is empty. Please enter a message.");
            return;
        }

        if (currentAgent == null)
        {
            Debug.LogError("Cannot proceed with LLM response. No current agent is set.");
            ShowDialogue("The AI assistant is currently unavailable.");
            return;
        }

        // Disable the send button to prevent multiple inputs while waiting for AI response
        sendButton.interactable = false;
        playerInputPanel.SetActive(false);

        // Send input to InitializePuerts for processing via LangGraph
        try
        {
            Debug.Log("Sending input to InitializePuerts for processing...");
            RunAsyncResponse(playerInput);

            Debug.Log("Input sent to InitializePuerts successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing input via InitializePuerts: {ex.Message}");
            ShowDialogue("An error occurred while processing your input.");
        }
    }



    private async Task EnsureLLMServiceReady()
    {
        if (llmCharacter == null || llmCharacter.llm == null)
        {
            Debug.LogError("llmCharacter or llm is null. Skipping LLM service readiness check.");
            return;
        }

        Debug.Log("Waiting for LLM service to be ready...");
        try
        {
            await llmCharacter.llm.WaitUntilReady(); // Ensure LLM is fully ready
            Debug.Log("LLM service is now ready.");
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("LLM initialization was canceled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while waiting for LLM service: {ex.Message}");
        }
    }




    public async void RunAsyncResponse(string playerInput)
    {
        if (string.IsNullOrWhiteSpace(playerInput))
        {
            Debug.LogWarning("Player input is empty. Please enter a message.");
            return;
        }

        if (isAwaitingResponse)
        {
            Debug.Log("Already awaiting a response. Please wait for the current interaction to complete.");
            return;
        }

        if (currentAgent == null)
        {
            Debug.LogError("Cannot process AI response. No AI agent is set.");
            ShowDialogue("The AI assistant is currently unavailable.");
            return;
        }

        if (currentAgent.llmCharacter == null)
        {
            Debug.LogError("The AI agent is missing its LLMCharacter component. Cannot proceed.");
            ShowDialogue("Sorry, the AI service is unavailable.");
            return;
        }

        // Mark the system as awaiting a response
        isAwaitingResponse = true;
        sendButton.interactable = false;

        Debug.Log($"Sending player input to AI: {playerInput}");

        try
        {
            // Ensure the LLM service is ready
            Debug.Log("Ensuring LLM service readiness...");
            await EnsureLLMServiceReady();
            Debug.Log("LLM service is ready.");

            // Process AI interaction
            string aiResponse = await currentAgent.Interact(userId, playerInput);
            Debug.Log("AI interaction complete.");

            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                Debug.LogWarning("AI returned an empty response.");
                ShowDialogue("Sorry, I couldn't generate a response.");
            }
            else
            {
                Debug.Log($"AI Response: {aiResponse}");
                ShowDialogue(aiResponse);
        
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during AI interaction: {ex.Message}\n{ex.StackTrace}");
            ShowDialogue("An error occurred while processing your request.");
        }
        finally
        {
            // Reset the state
            isAwaitingResponse = false;
            sendButton.interactable = true;
            Debug.Log("RunAsyncResponse has completed.");
        }
    }


 

    // Function to display the dialogue above NPC and as floating text
    public void ShowDialogue(string message)
    {
        // Display in Dialogue UI
        if (dialogueCanvas != null && dialogueText != null)
        {
            dialogueText.text = message;
            dialogueCanvas.gameObject.SetActive(true);
            Invoke("HideDialogue", 5f); // Hide dialogue after 5 seconds
            Debug.Log("Displayed dialogue in UI.");
        }
        else
        {
            Debug.LogError("Dialogue Canvas or Dialogue Text is not assigned.");
        }

        // Also display floating text above NPC
        if (floatingDialogueText != null)
        {
            floatingDialogueText.text = message;
            floatingDialogueText.gameObject.SetActive(true);
            Invoke("HideFloatingDialogue", 5f); // Hide floating text after 5 seconds
            Debug.Log("Displayed floating dialogue above NPC.");
        }
        else
        {
            Debug.LogError("Floating Dialogue Text is not assigned.");
        }

        // TTS Integration
        if (GameSettings.Instance != null && GameSettings.Instance.isVoiceEnabled)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (ttsController != null && !ttsController.IsSpeaking)
            {
                ttsController.Speak(message, (status) =>
                {
                    if (status == TextToSpeechComplete.Error)
                    {
                        Debug.LogError("TTS encountered an error while speaking.");
                    }
                    else
                    {
                        Debug.Log("TTS completed speaking the message.");
                    }
                });
                Debug.Log("TTS is speaking the message.");
            }
            else
            {
                Debug.LogWarning("TTS controller is not initialized or already speaking.");
            }
#else
            Debug.LogWarning("TTS is only supported on Android and iOS platforms.");
#endif
        }
    }

    void HideDialogue()
    {
        if (dialogueCanvas != null)
        {
            dialogueCanvas.gameObject.SetActive(false);
            Debug.Log("Dialogue UI hidden.");
        }
    }

    void HideFloatingDialogue()
    {
        if (floatingDialogueText != null)
        {
            floatingDialogueText.gameObject.SetActive(false);
            Debug.Log("Floating Dialogue hidden.");
        }
    }


    // Assign default fonts if missing
    void AssignFonts()
    {
        // Replace Arial.ttf with LegacyRuntime.ttf
        Font defaultUIFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (dialogueText != null && dialogueText.font == null)
        {
            dialogueText.font = defaultUIFont;
            Debug.Log("Assigned LegacyRuntime font to Dialogue Text.");
        }

        if (floatingDialogueText != null && floatingDialogueText.font == null)
        {
            floatingDialogueText.font = defaultUIFont;
            Debug.Log("Assigned LegacyRuntime font to Floating Dialogue Text.");
        }

        // Assign TMP_FontAsset to TMP_InputField's text component
        TMP_FontAsset defaultTMPFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Arial SDF"); // Ensure this path is correct

        if (playerInputField != null && playerInputField.textComponent != null && playerInputField.textComponent.font == null)
        {
            if (defaultTMPFont != null)
            {
                playerInputField.textComponent.font = defaultTMPFont;
                Debug.Log("Assigned default TMP font to Player Input Field.");
            }
            else
            {
                Debug.LogWarning("Default TMP Font not found. Assigning fallback font.");
                playerInputField.textComponent.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Arial SDF"); // Fallback
            }
        }
    }

    // Handle the player entering the NPC interaction zone
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.gameObject;
            currentPlayerName = player.name;
            OpenPlayerInputUI();
        }
    }

    // Handle the player leaving the NPC interaction zone
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = null;
            currentPlayerName = null;
            HidePlayerInputUI();
        }
    }

#if UNITY_ANDROID || UNITY_IOS
    void OnDestroy()
    {
        if (ttsController != null && ttsController.IsSpeaking)
        {
            ttsController.Stop();
            Debug.Log("TTS stopped on destroy.");
        }


        if (InitializePuerts.Instance != null)
        {
            InitializePuerts.Instance.OnLLMResponseReceived -= HandleLLMResponse;
        }


        cancellationTokenSource?.Cancel();

 

        SceneManager.sceneLoaded -= OnSceneLoaded;


    }
#endif

    // Voice Toggle Button Value Changed Handler
    private void OnVoiceToggleClicked(bool isOn)
    {
#if UNITY_ANDROID || UNITY_IOS
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.isVoiceEnabled = isOn;
            Debug.Log($"Voice Enabled set to: {GameSettings.Instance.isVoiceEnabled}");
        }
        else
        {
            Debug.LogError("GameSettings instance is not available.");
        }
#else
        Debug.LogWarning("Voice toggle is only supported on Android and iOS platforms.");
#endif
    }
}
