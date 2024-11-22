using UnityEngine;
using Puerts;
using System;
using LLMUnity;
using Newtonsoft.Json; // Add this if JSON serialization is not already present

public class InitializePuerts : MonoBehaviour
{
    private JsEnv jsEnv;
    public LLMCharacter llmCharacter;

    // Singleton Instance
    public static InitializePuerts Instance { get; private set; }

    // Event to notify when an LLM response is received
    public event Action<string> OnLLMResponseReceived;

    // Reference to the JavaScript function
    private Action<string> processUserInputJS;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        try
        {
            Debug.Log("Initializing PuerTS environment...");
            jsEnv = new JsEnv(new CustomLoader("Assets/Resources"));

            // Load the JavaScript module and bind to the function that processes user input
            processUserInputJS = jsEnv.ExecuteModule<Action<string>>("langgraph.bundle.mjs", "processUserInput");

            if (processUserInputJS == null)
            {
                Debug.LogError("JavaScript function processUserInput not found.");
            }
            else
            {
                Debug.Log("Successfully bound to processUserInput function.");
            }

            Debug.Log("PuerTS environment initialized successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize PuerTS: {e}");
        }
    }

    void Update()
    {
        jsEnv?.Tick();
    }

    private void OnDestroy()
    {
        jsEnv?.Dispose();
        if (Instance == this) Instance = null;
    }

    // Modified ProcessUserInput to serialize inputs as JSON
    public void ProcessUserInput(string userInput, string agentId)
    {
        if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(agentId))
        {
            Debug.LogError("Invalid input: userInput or agentId is missing.");
            return;
        }

        if (jsEnv == null || processUserInputJS == null)
        {
            Debug.LogError("PuerTS environment or JavaScript function is not initialized.");
            return;
        }

        var inputData = new { userInput, agentId };
        string jsonData = JsonConvert.SerializeObject(inputData);

        try
        {
            processUserInputJS.Invoke(jsonData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error invoking JavaScript function: {ex.Message}");
        }
    }



    public void SendMessageToLLMCharacter(string message, string agentId)
    {
        var aiManager = AIManager.Instance;
        var agent = aiManager?.GetAIAgentById(agentId);

        if (agent == null)
        {
            Debug.LogError($"Agent with ID {agentId} not found.");
            return;
        }

        // Update currentAgent in PicoDialogue
        PicoDialogue.Instance.currentAgent = agent;

        Debug.Log($"Sending message to AIAgent '{agent.AgentName}': {message}");

        // Use the RunAsyncResponse directly
        PicoDialogue.Instance.RunAsyncResponse(message);
    }



}
