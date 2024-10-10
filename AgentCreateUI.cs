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
        if (createAgentButton != null)
        {
            createAgentButton.onClick.AddListener(OnCreateAgentClicked);
        }
    }

    private void OnCreateAgentClicked()
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
