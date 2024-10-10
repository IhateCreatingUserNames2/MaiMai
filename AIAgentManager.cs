// AIAgentManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using LLMUnity;
using RAGSearchUnity;
using System.Linq;

public class AIAgentManager : MonoBehaviour
{
    public Embedding embeddingModel;
    public LLMCharacterMemoryManager memoryManager;

    [Header("UI Elements")]
    public Button selectAIButton;
    public Button despawnAllButton; // Button to despawn all agents
    public TMP_Dropdown aiDropdown;
    public GameObject aiPrefab;

    private List<GameObject> spawnedAgents = new List<GameObject>(); // List to track spawned AI agents

    private void Start()
    {
        if (memoryManager == null)
        {
            memoryManager = GetComponent<LLMCharacterMemoryManager>();
            if (memoryManager == null)
            {
                memoryManager = gameObject.AddComponent<LLMCharacterMemoryManager>();
            }
        }

        memoryManager.embeddingModel = embeddingModel;

        if (selectAIButton != null)
        {
            selectAIButton.interactable = true; // Ensure the button is interactable
            selectAIButton.onClick.AddListener(OnSelectAIClicked);
        }
        else
        {
            Debug.LogError("Select AI Button is not assigned in the Inspector.");
        }

        if (aiDropdown == null)
        {
            Debug.LogError("AI Dropdown is not assigned in the Inspector.");
        }

        if (aiPrefab == null)
        {
            Debug.LogError("AI Prefab is not assigned in the Inspector.");
        }

        PopulateDropdown();
    }

    public void PopulateDropdown()
    {
        if (aiDropdown != null && AIManager.Instance != null)
        {
            aiDropdown.ClearOptions();
            aiDropdown.AddOptions(AIManager.Instance.GetAllAgentNames());
            Debug.Log("AI Dropdown populated with saved agents.");
        }
        else
        {
            Debug.LogError("Cannot populate dropdown: AI Dropdown or AIManager.Instance is null.");
        }
    }

    public void OnSelectAIClicked()
    {
        if (aiDropdown != null && aiDropdown.options.Count > 0)
        {
            int selectedIndex = aiDropdown.value;
            string selectedAgent = aiDropdown.options[selectedIndex].text;

            Debug.Log($"Selected agent from dropdown: '{selectedAgent}'");

            // Try to spawn the AI agent
            if (!string.IsNullOrEmpty(selectedAgent))
            {
                SpawnAI(selectedAgent);
            }
            else
            {
                Debug.LogError("Selected agent name is null or empty.");
            }
        }
        else
        {
            Debug.LogError("AI Dropdown is not populated or no selection made.");
        }
    }

    public void SpawnAI(string agentName)
    {
        if (aiPrefab == null)
        {
            Debug.LogError("AI Prefab is not assigned.");
            return;
        }

        // Check if an agent with this name already exists in the scene
        GameObject existingAgent = GameObject.Find(agentName);
        if (existingAgent != null)
        {
            Debug.LogWarning($"AI Agent '{agentName}' is already in the scene. Destroying the existing one before spawning a new one.");
            Destroy(existingAgent);

            StartCoroutine(WaitAndSpawnAI(agentName));
            return;
        }

        SpawnAIDirectly(agentName);
    }

    private IEnumerator WaitAndSpawnAI(string agentName)
    {
        yield return new WaitForEndOfFrame();
        SpawnAIDirectly(agentName);
    }

    private void SpawnAIDirectly(string agentName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Player GameObject with tag 'Player' not found.");
            return;
        }

        Vector3 spawnPosition = player.transform.position + player.transform.forward * 2f;
        GameObject spawnedAI = Instantiate(aiPrefab, spawnPosition, Quaternion.identity);
        spawnedAI.name = agentName;
        spawnedAI.SetActive(true);

        AIAgentInteraction agentInteraction = spawnedAI.GetComponent<AIAgentInteraction>();
        if (agentInteraction != null)
        {
            AIAgent agent = AIManager.Instance.GetAIAgentByName(agentName);
            if (agent != null)
            {
                // Ensure llmCharacter is assigned
                if (agent.llmCharacter == null)
                {
                    Debug.LogError($"AIAgentManager: LLMCharacter is not assigned for agent '{agent.AgentName}'. Assigning PicoDialogue's LLMCharacter.");
                    agent.SetLLMCharacter(PicoDialogue.Instance.llmCharacter);
                }

                agentInteraction.SetAIAgent(agent);

                if (PicoDialogue.Instance != null)
                {
                    PicoDialogue.Instance.SetAgentData(agent);

                    // Load chat history for the newly spawned agent
                    agent.llmCharacter.Load($"{agent.AgentId}_chatHistory");

                    Debug.Log($"PicoDialogue NPC data updated with agent '{agent.AgentName}', and chat history loaded.");
                }
                else
                {
                    Debug.LogError("AIAgentManager: PicoDialogue instance is null.");
                }

                Debug.Log($"AI Agent '{agent.AgentName}' has been assigned to interaction and PicoDialogue updated.");
            }
            else
            {
                Debug.LogError($"AI Agent '{agentName}' not found in AIManager.");
            }
        }
        else
        {
            Debug.LogError("AIAgentInteraction component is missing on the AI Prefab.");
        }

        spawnedAgents.Add(spawnedAI);
    }


    public void SaveAgentData(AIAgent agent)
    {
        // Combine all chat messages
        string chatHistory = string.Join(" ", agent.llmCharacter.chat.Select(m => m.content));
        memoryManager.EmbedChatHistory(chatHistory, agent.AgentId);
    }

    public void DespawnAllAgents()
    {
        Debug.Log("Despawning all AI agents.");
        foreach (GameObject agent in spawnedAgents)
        {
            if (agent != null)
            {
                Destroy(agent);
            }
        }
        spawnedAgents.Clear(); // Clear the list after despawning all agents
    }
}
