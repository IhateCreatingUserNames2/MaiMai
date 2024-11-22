// AIButtonController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AIButtonController : MonoBehaviour
{
    public Button selectAIButton;
    public TMP_Dropdown aiDropdown;
    public GameObject aiPrefab;
    public PicoDialogue picoDialogue;

    private void Start()
    {
        if (selectAIButton != null)
        {
            selectAIButton.onClick.AddListener(OnSelectAIClicked);
        }

        if (aiDropdown == null)
        {
            Debug.LogError("AI Dropdown is not assigned in the Inspector.");
        }

        if (aiPrefab == null)
        {
            Debug.LogError("AI Prefab is not assigned in the Inspector.");
        }

        if (picoDialogue == null)
        {
            Debug.LogError("PicoDialogue reference is not assigned in the Inspector.");
        }

        PopulateDropdown();
    }

    public void PopulateDropdown()
    {
        if (aiDropdown != null && AIManager.Instance != null)
        {
            aiDropdown.ClearOptions();
            aiDropdown.AddOptions(AIManager.Instance.GetAllAgentNames());
        }
    }

    public void OnSelectAIClicked()
    {
        if (aiDropdown != null && aiDropdown.options.Count > 0)
        {
            int selectedIndex = aiDropdown.value;
            string selectedAgent = aiDropdown.options[selectedIndex].text;
            SpawnAI(selectedAgent);
        }
    }

    public void SpawnAI(string agentName)
    {
        if (aiPrefab != null)
        {
            // Check if the AI Agent with this name is already in the scene
            GameObject existingAgent = GameObject.Find(agentName);
            if (existingAgent != null)
            {
                Debug.LogWarning($"AI Agent '{agentName}' is already in the scene. Not spawning a duplicate.");
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                Vector3 spawnPosition = player.transform.position + player.transform.forward * 2f;

                GameObject spawnedAI = Instantiate(aiPrefab, spawnPosition, Quaternion.identity);
                spawnedAI.name = agentName;

                AIAgentInteraction agentInteraction = spawnedAI.GetComponent<AIAgentInteraction>();
                if (agentInteraction != null)
                {
                    AIAgent agent = AIManager.Instance.GetAIAgentByName(agentName);
                    if (agent != null)
                    {
                        agentInteraction.SetAIAgent(agent);

                        if (picoDialogue != null && picoDialogue.llmCharacter != null)
                        {
                            agent.SetLLMCharacter(picoDialogue.llmCharacter);
                        }
                    }
                }
            }
        }
    }
}
