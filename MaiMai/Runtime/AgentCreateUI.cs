using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LLMUnity;
using System.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using MFPS;
using System.Collections;

public class AgentCreateUI : MonoBehaviour
{
    public TMP_InputField agentNameInput; // New field for AgentName
    public TMP_InputField customPromptInput; // Field for CustomPrompt
    public Button createAgentButton;
    public AIAgentManager aiButtonController;
    public LLMCharacter llmCharacter;
    public LLMCharacterMemoryManager memoryManager;

    private void Start()
    {

        StartCoroutine(RetryMemoryManagerAssignment());

        if (memoryManager == null)
        {
            memoryManager = LLMCharacterMemoryManager.Instance;
            if (memoryManager == null)
            {
                Debug.LogError("LLMCharacterMemoryManager is not found. Ensure it is initialized in the MainMenu scene.");
            }
            else
            {
                Debug.Log("LLMCharacterMemoryManager successfully assigned from MainMenu.");
            }
        }

        if (llmCharacter == null)
        {
            llmCharacter = FindObjectOfType<LLMCharacter>();
            if (llmCharacter == null)
            {
                Debug.LogError("LLMCharacter is not assigned in the Inspector and could not be found in the scene. Please assign it.");
            }
            else
            {
                Debug.Log("LLMCharacter found and assigned from the scene.");
            }
        }
        else
        {
            Debug.Log("LLMCharacter is already assigned via the Inspector.");
        }

        if (createAgentButton != null)
        {
            createAgentButton.onClick.AddListener(OnCreateAgentClicked);
        }
        else
        {
            Debug.LogError("CreateAgentButton is not assigned.");
        }
    }

    private IEnumerator RetryMemoryManagerAssignment()
    {
        int retryCount = 10;
        while (retryCount > 0)
        {
            memoryManager = LLMCharacterMemoryManager.Instance;
            if (memoryManager != null)
            {
                Debug.Log("LLMCharacterMemoryManager successfully assigned in AgentCreateUI.");
                yield break;
            }
            retryCount--;
            yield return new WaitForSeconds(0.2f); // Retry after a short delay
        }
        Debug.LogError("LLMCharacterMemoryManager could not be found in the current scene.");
    }



    public async void OnCreateAgentClicked()
    {
        string agentName = agentNameInput.text.Trim();
        string customPrompt = customPromptInput.text.Trim();

        if (string.IsNullOrEmpty(agentName))
        {
            Debug.LogError("AgentName cannot be empty.");
            return;
        }

        if (memoryManager == null)
        {
            Debug.LogError("LLMCharacterMemoryManager is not assigned in the AgentCreateUI.");
            return;
        }

        if (llmCharacter == null)
        {
            Debug.LogError("LLMCharacter is not assigned in AgentCreateUI.");
            return;
        }

        llmCharacter.AIName = agentName; // Use AgentName for display purposes
        llmCharacter.playerName = GetPlayerName();

        LLMCharacter agentLLMCharacter = Instantiate(llmCharacter);
        agentLLMCharacter.AIName = agentName;
        agentLLMCharacter.playerName = llmCharacter.playerName;

        AIAgent newAgent = new AIAgent(
            System.Guid.NewGuid().ToString(),
            agentName, // Assign the specified AgentName for UI
            agentLLMCharacter,
            customPrompt,
            memoryManager
        );

        newAgent.SetupSystemPrompt(newAgent.AgentId, customPrompt);

        Debug.Log($"Agent Created - Name: {newAgent.AgentName}, Custom Prompt: {customPrompt}");

        if (AIManager.Instance != null)
        {
            AIManager.Instance.RegisterAIAgent(newAgent);
        }

        await SaveSystem.SaveAgentDataAsync(newAgent);
        await SaveSystem.SaveManifestAsync(AIManager.Instance.GetAllAgents());

        if (aiButtonController != null)
        {
            aiButtonController.PopulateDropdown();
        }

        ClearInputFields();
    }

    private string GetPlayerName()
    {
        if (!string.IsNullOrEmpty(bl_PhotonNetwork.NickName))
        {
            Debug.Log($"Player name retrieved from bl_PhotonNetwork: {bl_PhotonNetwork.NickName}");
            return bl_PhotonNetwork.NickName;
        }
        if (PhotonNetwork.LocalPlayer != null && !string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
        {
            Debug.Log($"Player name retrieved from PhotonNetwork.LocalPlayer: {PhotonNetwork.LocalPlayer.NickName}");
            return PhotonNetwork.LocalPlayer.NickName;
        }

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            string playerName = playerGO.name;
            if (!string.IsNullOrEmpty(playerName))
            {
                Debug.Log($"Player name retrieved from GameObject name: {playerName}");
                return playerName;
            }
        }

        Debug.LogWarning("Player name could not be retrieved. Using default name 'Player'.");
        return "Player";
    }

    private void ClearInputFields()
    {
        agentNameInput.text = "";
        customPromptInput.text = "";
    }
}
