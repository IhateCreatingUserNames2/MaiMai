using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMUnity
{
    public class LLMUnitySetupHelper : MonoBehaviour
    {
        // Singleton instance to ensure single access point if needed
        public static LLMUnitySetupHelper Instance { get; private set; }

        void Awake()
        {
            // Ensure only one instance exists
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

        // Method to extract the file with additional checks
        public async Task ExtractFileWithChecks(string assetName, bool overwrite = false)
        {
            string targetPath = LLMUnity.LLMUnitySetup.GetAssetPath(assetName);
            string targetDirectory = Path.GetDirectoryName(targetPath);

            // Ensure directory exists before attempting extraction
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Call the original extraction method with added error handling
            try
            {
                await LLMUnity.LLMUnitySetup.AndroidExtractFile(assetName, overwrite);
                Debug.Log($"File {assetName} extracted successfully.");
            }
            catch (DirectoryNotFoundException e)
            {
                Debug.LogError($"Directory not found: {e.Message}. Creating directories and retrying extraction...");
                Directory.CreateDirectory(targetDirectory); // Attempt to create missing directories
                await LLMUnity.LLMUnitySetup.AndroidExtractFile(assetName, overwrite);
            }
            catch (IOException e)
            {
                Debug.LogError($"IO Exception during extraction: {e.Message}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Unexpected error during file extraction: {e.Message}");
            }
        }

        // Additional method to check and request permissions on Android
        public void RequestAndroidPermissions()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
            }

            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
            }
#endif
        }

        // Helper function to wrap LLM initialization
        public async Task InitializeLLMUnity()
        {
            // Ensure permissions on Android
            RequestAndroidPermissions();

            // List of files that need extraction
            string[] filesToExtract = new string[]
            {
                "models/llama-3.2-1b-instruct-q4_k_m.gguf",
                "models/llama-3.2-3b-instruct-q4_k_m.gguf"
                // Add other files as necessary
            };

            foreach (var file in filesToExtract)
            {
                await ExtractFileWithChecks(file, overwrite: false);
            }

            Debug.Log("LLM Unity setup completed.");
        }
    }
}
