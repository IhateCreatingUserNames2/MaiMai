using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnity
{
    public class FixModelsScript : MonoBehaviour
    {
        // The list of model URLs that we want to download if missing
        private readonly Dictionary<string, string> requiredModels = new Dictionary<string, string>
        {
            // Add required models with URL here (filename, download URL)
            { "llama-3.2-1b-instruct-q4_k_m.gguf", "https://huggingface.co/hugging-quants/Llama-3.2-1B-Instruct-Q4_K_M-GGUF/resolve/main/llama-3.2-1b-instruct-q4_k_m.gguf" },
            { "llama-3.2-3b-instruct-q4_k_m.gguf", "https://huggingface.co/hugging-quants/Llama-3.2-3B-Instruct-Q4_K_M-GGUF/resolve/main/llama-3.2-3b-instruct-q4_k_m.gguf" }
        };

        // Reference to the button in the UI
        public Button fixButton;

        private void Start()
        {
            // Attach the FixModels method to the button's onClick event
            if (fixButton != null)
            {
                fixButton.onClick.AddListener(FixModels);
            }
        }

        public async void FixModels()
        {
            Debug.Log("Checking for missing models...");
            bool modelsMissing = false;

            foreach (var model in requiredModels)
            {
                string localPath = LLMUnitySetup.GetAssetPath("models/" + model.Key);
                if (!File.Exists(localPath))
                {
                    modelsMissing = true;
                    Debug.Log($"Model {model.Key} not found, preparing to download...");
                    await DownloadModel(model.Key, model.Value);
                }
                else
                {
                    Debug.Log($"Model {model.Key} is already present.");
                }
            }

            if (!modelsMissing)
            {
                Debug.Log("All required models are already present.");
            }
            else
            {
                Debug.Log("Model download process completed.");
            }
        }

        private async Task DownloadModel(string modelName, string modelUrl)
        {
            try
            {
                // Download the model file using LLMUnity's DownloadFile method
                string savePath = LLMUnitySetup.GetAssetPath("models/" + modelName);
                await LLMUnitySetup.DownloadFile(modelUrl, savePath, overwrite: false);
                Debug.Log($"Model {modelName} downloaded successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to download model {modelName}: {ex.Message}");
            }
        }
    }
}
