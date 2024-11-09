using UnityEngine;
using Puerts;
using System;
using LLMUnity;

namespace MFPS.Scripts
{
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

        // Method to process user input from PicoDialogue.cs
        public void ProcessUserInput(string userInput)
        {
            if (jsEnv == null || processUserInputJS == null)
            {
                Debug.LogError("PuerTS environment or JavaScript function not properly initialized.");
                return;
            }

            // Call the JavaScript function and pass the user input
            processUserInputJS.Invoke(userInput);
        }

        public void SendMessageToLLMCharacter(string message)
        {
            if (llmCharacter == null)
            {
                Debug.LogError("LLMCharacter is not assigned.");
                return;
            }

            llmCharacter.Chat(message).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    string response = task.Result;
                    Debug.Log($"LLM response: {response}");

                    // Notify subscribers with the response
                    OnLLMResponseReceived?.Invoke(response);
                }
                else
                {
                    Debug.LogError("Error calling LLMCharacter.Chat");
                }
            });
        }
    }
}
