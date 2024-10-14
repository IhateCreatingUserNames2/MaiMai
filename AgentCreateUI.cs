using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LLMUnity;


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


    public void OnCreateAgentClicked()
    {
        string agentName = agentNameInput.text.Trim();
        string personality = personalityInput.text.Trim();
        string background = backgroundInput.text.Trim();

        if (memoryManager == null)
        {
            Debug.LogError("LLMCharacterMemoryManager is not assigned in the AgentCreateUI.");
            return;
        }

        // Create new agent with personality and background
        AIAgent newAgent = new AIAgent(System.Guid.NewGuid().ToString(), agentName, llmCharacter, personality, background, memoryManager);

        // Register the agent in AIManager
        if (AIManager.Instance != null)
        {
            AIManager.Instance.RegisterAIAgent(newAgent);
        }

        aiButtonController.PopulateDropdown();

        ClearInputFields();
    }


    private void ClearInputFields()
    {
        agentNameInput.text = "";
        personalityInput.text = "";
        backgroundInput.text = "";
    }
}
