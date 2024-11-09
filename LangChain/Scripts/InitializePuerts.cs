using UnityEngine;
using Puerts;
using LLMUnity;
using System.Threading.Tasks;


namespace MFPS.Scripts
{
    public class InitializePuerts : MonoBehaviour
    {
        private JsEnv jsEnv;
        public LLMCharacter llmCharacter;

        // Singleton Instance
        public static InitializePuerts Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
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
                jsEnv = new JsEnv();

                // Execute the testLangGraph module to test the PuerTS setup
                jsEnv.ExecuteModule("testLangGraph.mjs");

                Debug.Log("PuerTS environment initialized successfully!");
            }
            catch (System.Exception e)
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

        // Public method to receive messages from JavaScript and call LLMCharacter.Chat
        public void SendMessageToLLMCharacter(string message)
        {
            if (llmCharacter == null)
            {
                Debug.LogError("LLMCharacter is not assigned.");
                return;
            }

            // Call Chat on the LLMCharacter and log the result asynchronously
            llmCharacter.Chat(message).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log($"LLM response: {task.Result}");
                }
                else
                {
                    Debug.LogError("Error calling LLMCharacter.Chat");
                }
            });
        }
    }
}
