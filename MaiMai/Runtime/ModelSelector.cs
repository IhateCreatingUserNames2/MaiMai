using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using LLMUnity;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;

public class ModelSelector : MonoBehaviour
{
    public LLM llm; // Reference to the LLM script
    public TMP_Dropdown modelDropdown; // TMP Dropdown for model selection
    public Button changeModelButton; // Button to apply the model change

    void Start()
    {
        // Populate the dropdown with models from LLMManager
        StartCoroutine(PopulateModelDropdown());

        // Set the Change Model button listener
        changeModelButton.onClick.AddListener(OnChangeModelButtonClicked);
    }

    // Populates the dropdown with models from LLMManager
    IEnumerator PopulateModelDropdown()
    {
        modelDropdown.ClearOptions();

#if UNITY_ANDROID && !UNITY_EDITOR
        string modelsJsonPath = Path.Combine(Application.streamingAssetsPath, "models.json");
        string jsonContent = "";

        using (UnityWebRequest www = UnityWebRequest.Get(modelsJsonPath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load models.json: " + www.error);
                yield break;
            }
            else
            {
                jsonContent = www.downloadHandler.text;
            }
        }

        // Parse jsonContent to populate modelEntries
        ModelEntryList entryList = JsonUtility.FromJson<ModelEntryList>(jsonContent);
        LLMManager.modelEntries = entryList.entries;
#else
        // Use the existing LLMManager.LoadFromDisk() method
        LLMManager.LoadFromDisk();
#endif

        // Proceed after model entries are loaded
        foreach (var modelEntry in LLMManager.modelEntries)
        {
            if (!string.IsNullOrEmpty(modelEntry.label))
            {
                modelDropdown.options.Add(new TMP_Dropdown.OptionData(modelEntry.label));
            }
        }

        if (modelDropdown.options.Count == 0)
        {
            Debug.LogError("No models found in LLMManager.");
        }
        else
        {
            modelDropdown.RefreshShownValue();
        }

        yield return null; // Ensure the coroutine completes properly
    }

    // Handles the model change when the button is clicked
    public async void OnChangeModelButtonClicked()
    {
        try
        {
            // Wait until the LLM is ready before changing models
            await llm.WaitUntilReady();

            // Get the selected model from the dropdown
            string selectedModelLabel = modelDropdown.options[modelDropdown.value].text;

            // Find the corresponding ModelEntry in LLMManager
            var selectedModel = LLMManager.modelEntries.Find(entry => entry.label == selectedModelLabel);

            if (selectedModel != null)
            {
                // Destroy the current LLM instance
                llm.Destroy();
                Debug.Log("Current model destroyed.");

                // Set the selected model in the LLM script using the model's filename
                llm.SetModel(selectedModel.filename);
                Debug.Log("Changed model to: " + selectedModel.filename);

                // Restart the LLM server with the new model
                llm.Awake(); // Call the existing Awake method
                Debug.Log("LLM server restarted with the new model.");
            }
            else
            {
                Debug.LogError("Selected model not found in LLMManager.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception occurred while changing model: {ex.Message}");
        }
    }
}

// Define ModelEntryList using LLMUnity.ModelEntry
[System.Serializable]
public class ModelEntryList
{
    public List<LLMUnity.ModelEntry> entries;
}
