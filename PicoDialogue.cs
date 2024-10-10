// PicoDialogue.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Chat;
using ExitGames.Client.Photon;
using LLMUnity;
using System.Threading.Tasks;
using System;
using System.Linq;
using TextToSpeech;

public class PicoDialogue : MonoBehaviour, IChatClientListener
{
    [Header("LLMUnity Integration")]
    public LLMCharacter llmCharacter; // Reference to the LLMCharacter component from LLMUnity

    public bool isAwaitingResponse = false;
    public static PicoDialogue Instance { get; private set; }

    private string currentPlayerName;
    private GameObject player; // Reference to the player GameObject

    [Header("UI Elements")]
    public Canvas dialogueCanvas;
    public Text dialogueText; // UnityEngine.UI.Text for dialogue UI
    public GameObject playerInputPanel;
    public TMP_InputField playerInputField; // TMPro.TMP_InputField for player input
    public Button sendButton;
    public Toggle voiceToggleButton; // Toggle to enable/disable voice
    public Text floatingDialogueText; // UnityEngine.UI.Text for floating dialogue

    private AIAgent currentAgent; // Track the currently active agent

    [Header("NPC Configurations")]
    public string npcName;
    public string npcPersonality;
    public string npcBackground;

    [Header("Photon Chat Settings")]
    public string appIdChat = "YOUR_PHOTON_CHAT_APP_ID"; // Replace with your Photon Chat App ID
    public string appVersion = "1.0";
    public string fixedRegion = "us"; // Adjust based on your region (e.g., eu, us)

    private ChatClient chatClient;
    private string userId = string.Empty; // Player's unique ID assigned via Photon Chat
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
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
            return;
        }
    }

    void Start()
    {
        // Validate UI Assignments
        ValidateUIElements();

        // Initialize voice toggle button
        InitializeVoiceToggle();

        // Get or assign llmCharacter
        if (llmCharacter == null)
        {
            // Remove the existing GetComponent call and add the following:
            GameObject dialogueELLMCharacterGO = GameObject.Find("DialogueELLMCHARACTER");
            if (dialogueELLMCharacterGO != null)
            {
                llmCharacter = dialogueELLMCharacterGO.GetComponent<LLMCharacter>();
                if (llmCharacter == null)
                {
                    Debug.LogError("PicoDialogue: LLMCharacter component is missing on DialogueELLMCHARACTER GameObject.");
                }
                else
                {
                    Debug.Log("LLMCharacter successfully assigned from DialogueELLMCHARACTER GameObject.");
                }
            }
            else
            {
                Debug.LogError("PicoDialogue: DialogueELLMCHARACTER GameObject not found in the scene.");
            }
        }

#if UNITY_ANDROID || UNITY_IOS
        // Initialize Text-to-Speech
        InitializeTextToSpeech();
#endif

        // Initialize Photon Chat
        InitializePhotonChat();

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


    void Update()
    {
        if (chatClient != null)
        {
            chatClient.Service(); // Maintain the Photon chat connection
        }
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

    // Initialize Photon Chat
    private void InitializePhotonChat()
    {
        ChatAppSettings chatAppSettings = new ChatAppSettings
        {
            AppIdChat = appIdChat,
            AppVersion = appVersion,
            FixedRegion = fixedRegion,
            Protocol = ConnectionProtocol.Udp,
            EnableProtocolFallback = true
        };

        // Generate a unique User ID using GUID
        generatedUserId = System.Guid.NewGuid().ToString();
        userId = generatedUserId;
        Debug.Log($"User ID set to: {userId}");

        // Initialize authentication values for Photon Chat
        Photon.Chat.AuthenticationValues authValues = new Photon.Chat.AuthenticationValues
        {
            UserId = generatedUserId
        };

        // Create a new Chat Client
        chatClient = new ChatClient(this);

        // Connect to the chat server
        bool connectSuccess = chatClient.Connect(chatAppSettings.AppIdChat, chatAppSettings.AppVersion, authValues);
        Debug.Log($"Attempting to connect to Photon Chat: {(connectSuccess ? "Initiated" : "Failed to initiate")}");
    }

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
    }


    private void UpdateAgentUI()
    {
        // Example of updating UI elements to reflect the current agent
        if (dialogueText != null)
        {
            dialogueText.text = $"{currentAgent.AgentName}: {currentAgent.Personality}";
        }
    }

    // Callback when the player sends a message via the input panel
    public void OnSendButtonClicked()
    {
        if (playerInputField != null && !isAwaitingResponse)
        {
            string playerInput = playerInputField.text.Trim();
            if (string.IsNullOrEmpty(playerInput))
            {
                Debug.LogWarning("Player input is empty. Please enter a message.");
                return;
            }

            if (currentAgent == null)
            {
                Debug.LogError("Cannot proceed with LLM response. No current agent is set.");
                return;
            }

            // Disable the send button to prevent multiple inputs while waiting for AI response
            sendButton.interactable = false;

            playerInputPanel.SetActive(false);

            // Start the async response
            RunAsyncResponse(playerInput);
        }
        else
        {
            Debug.LogWarning("Cannot send message. Either Input Field is not assigned or waiting for AI response.");
        }
    }

    private async Task EnsureLLMServiceReady()
    {
        if (llmCharacter != null && llmCharacter.llm != null)
        {
            Debug.Log("Waiting for LLM service to be ready...");
            await llmCharacter.llm.WaitUntilReady();
            Debug.Log("LLM service is now ready.");
        }
        else
        {
            Debug.LogError("LLMCharacter or LLM is null, cannot proceed.");
        }
    }

    private async void RunAsyncResponse(string playerInput)
    {
        await EnsureLLMServiceReady();

        if (isAwaitingResponse)
        {
            return; // Exit if already waiting for a response
        }

        isAwaitingResponse = true; // Mark the system as awaiting response

        Debug.Log($"Sending prompt to LLM: {playerInput}");

        try
        {
            string aiResponse = await currentAgent.Interact(userId, playerInput);
            Debug.Log($"Received response from LLM: {aiResponse}");

            // Send response to chat and dialogue UI
            SendMessageToChat(aiResponse);
            ShowDialogue(aiResponse);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during AI interaction: {ex.Message}");
            ShowDialogue("Sorry, I couldn't process that.");
        }
        finally
        {
            isAwaitingResponse = false; // Mark the system as free for another request
            sendButton.interactable = true; // Re-enable the send button
        }
    }

    // Function to send the response to the chat system via bl_RoomChat
    void SendMessageToChat(string message)
    {
        if (bl_RoomChatBase.Instance != null)
        {
            bl_RoomChat roomChat = bl_RoomChatBase.Instance as bl_RoomChat;

            if (roomChat != null)
            {
                // Only send the AI's message, and not simulate the user part of the conversation
                string formattedMessage = $"[ALL] {llmCharacter.AIName}: {message}";
                roomChat.SetChat(formattedMessage);         // Send the message to the network as NPC

                currentAgent.llmCharacter.AddAIMessage(message); // Add only AI messages
                Debug.Log($"AI '{llmCharacter.AIName}' published message to chat: {message}");
            }
            else
            {
                Debug.LogError("RoomChat instance is null.");
            }
        }
        else
        {
            Debug.LogError("RoomChat system is not initialized.");
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

    // Photon Chat Callbacks

    public void DebugReturn(DebugLevel level, string message)
    {
        Debug.Log($"Photon Chat Debug ({level}): {message}");
    }

    public void OnDisconnected()
    {
        Debug.LogError("Photon Chat Disconnected. Reconnecting...");
        isConnected = false;
        StartCoroutine(ReconnectToChat());
    }

    IEnumerator ReconnectToChat()
    {
        int attempt = 1;
        while (!chatClient.CanChat && attempt <= 5)
        {
            yield return new WaitForSeconds(2f); // Delay before retrying connection

            Debug.Log($"Reconnecting to Photon Chat. Attempt {attempt}...");
            chatClient.Connect(appIdChat, appVersion, new Photon.Chat.AuthenticationValues(userId));

            attempt++;
        }

        if (!chatClient.CanChat)
        {
            Debug.LogError("Failed to reconnect to Photon Chat after 5 attempts.");
        }
    }

    public void OnConnected()
    {
        Debug.Log("Photon Chat Connected Successfully. Subscribing to ALL channel...");
        isConnected = true;
        chatClient.Subscribe(new string[] { "ALL" });  // Subscribe to the "ALL" channel
    }

    public void OnChatStateChange(ChatState state) { }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            string sender = senders[i];
            string message = messages[i].ToString();

            if (sender == llmCharacter.AIName)
            {
                ShowDialogue($"{sender}: {message}");
            }
            else
            {
                // Handle messages from other players if necessary
            }
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName) { }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        for (int i = 0; i < channels.Length; i++)
        {
            if (results[i])
                Debug.Log($"Successfully subscribed to channel: {channels[i]}");
            else
                Debug.LogError($"Failed to subscribe to channel: {channels[i]}");
        }
    }

    public void OnUnsubscribed(string[] channels)
    {
        foreach (var channel in channels)
        {
            Debug.Log($"Unsubscribed from channel: {channel}");
        }
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }

    public void OnUserSubscribed(string channel, string user) { }

    public void OnUserUnsubscribed(string channel, string user) { }

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
