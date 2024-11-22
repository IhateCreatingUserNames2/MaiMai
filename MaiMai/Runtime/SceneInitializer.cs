using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

namespace LLMUnity
{
    public class SceneInitializer : MonoBehaviour
    {
        private async void Awake()
        {
            // Ensure LLMUnitySetupHelper instance is present and initialized
            if (LLMUnitySetupHelper.Instance == null)
            {
                GameObject helperObject = new GameObject("LLMUnitySetupHelper");
                helperObject.AddComponent<LLMUnitySetupHelper>();
            }

            // Start initialization if this is not the MainMenu scene
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                await LLMUnitySetupHelper.Instance.InitializeLLMUnity();
                Debug.Log("LLM Unity setup initialized in Awake for non-MainMenu scene.");
            }

            // Add listener for any subsequent scene loads
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Ensure we only initialize in Map scenes, not in MainMenu
            if (scene.name != "MainMenu")
            {
                await LLMUnitySetupHelper.Instance.InitializeLLMUnity();
                Debug.Log("LLM Unity setup initialized after scene load.");
            }
        }

        private void OnDestroy()
        {
            // Clean up the scene loaded listener when this object is destroyed
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
