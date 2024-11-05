using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LLMUnity;
using System.Threading.Tasks;
using Photon.Pun; // Include Photon PUN namespace
using Photon.Realtime;
using MFPS; 


public class AgentCreateUI : MonoBehaviour
{
    public TMP_InputField agentNameInput;
    public TMP_InputField personalityInput;
    public TMP_InputField backgroundInput;
    public Button createAgentButton;
    public AIAgentManager aiButtonController;
    public LLMCharacter llmCharacter;
    public LLMCharacterMemoryManager memoryManager;

    private void Start()
    {
        // Dynamically assign LLMCharacterMemoryManager from the persistent instance
        if (memoryManager == null)
        {
            memoryManager = LLMCharacterMemoryManager.Instance; // Get the singleton instance
            if (memoryManager == null)
            {
                Debug.LogError("LLMCharacterMemoryManager is not found. Ensure it is initialized in the MainMenu scene.");
            }
            else
            {
                Debug.Log("LLMCharacterMemoryManager successfully assigned from MainMenu.");
            }
        }

        // Ensure llmCharacter is initialized
        if (llmCharacter == null)
        {
            // Try to find an existing LLMCharacter in the scene
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

        // Assign the listener for the Create Agent button
        if (createAgentButton != null)
        {
            createAgentButton.onClick.AddListener(OnCreateAgentClicked);
        }
        else
        {
            Debug.LogError("CreateAgentButton is not assigned.");
        }
    }


    public async void OnCreateAgentClicked()
    {
        string agentName = agentNameInput.text.Trim();
        string personality = personalityInput.text.Trim();
        string background = backgroundInput.text.Trim();

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

        llmCharacter.AIName = agentName;
        llmCharacter.playerName = GetPlayerName();

        LLMCharacter agentLLMCharacter = Instantiate(llmCharacter);
        agentLLMCharacter.AIName = agentName;
        agentLLMCharacter.playerName = llmCharacter.playerName;

        AIAgent newAgent = new AIAgent(
            System.Guid.NewGuid().ToString(),
            agentName,
            agentLLMCharacter,
            personality,
            background,
            memoryManager
        );

        // Explicitly call SetupSystemPrompt and check if it applies correctly
        newAgent.SetupSystemPrompt();

        Debug.Log($"Agent Created - Name: {newAgent.AgentName}, Personality: {newAgent.Personality}, Background: {newAgent.Background}");

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
    }


    private string GetPlayerName()
    {
        // Attempt to get the player's name from bl_PhotonNetwork
        if (!string.IsNullOrEmpty(bl_PhotonNetwork.NickName))
        {
            Debug.Log($"Player name retrieved from bl_PhotonNetwork: {bl_PhotonNetwork.NickName}");
            return bl_PhotonNetwork.NickName;
        }
        else
        {
            Debug.LogWarning("bl_PhotonNetwork.NickName is null or empty.");
        }

        // Alternatively, attempt to get the player's name from PhotonNetwork
        if (PhotonNetwork.LocalPlayer != null && !string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
        {
            Debug.Log($"Player name retrieved from PhotonNetwork.LocalPlayer: {PhotonNetwork.LocalPlayer.NickName}");
            return PhotonNetwork.LocalPlayer.NickName;
        }
        else
        {
            Debug.LogWarning("PhotonNetwork.LocalPlayer.NickName is null or empty.");
        }

        // As a last resort, attempt to get the player's name from the GameObject's name
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            string playerName = playerGO.name;
            if (!string.IsNullOrEmpty(playerName))
            {
                Debug.Log($"Player name retrieved from GameObject name: {playerName}");
                return playerName;
            }
            else
            {
                Debug.LogWarning("Player GameObject name is null or empty.");
            }
        }
        else
        {
            Debug.LogWarning("Player GameObject with tag 'Player' not found in the scene.");
        }

        // Return a default name if all else fails
        Debug.LogWarning("Player name could not be retrieved. Using default name 'Player'.");
        return "Player";
    }




    private void ClearInputFields()
    {
        agentNameInput.text = "";
        personalityInput.text = "";
        backgroundInput.text = "";
    }
}
