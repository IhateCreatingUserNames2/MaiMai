using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using LLMUnity;
using System;
using System.Threading.Tasks;

public class AIAgentManager : MonoBehaviour
{
    public LLMCharacterMemoryManager memoryManager;

    [Header("UI Elements")]
    public Button selectAIButton;
    public Button despawnAllButton; // Button to despawn all agents
    public TMP_Dropdown aiDropdown;
    public GameObject aiPrefab;

    private List<GameObject> spawnedAgents = new List<GameObject>(); // List to track spawned AI agents

    private void Awake()
    {
        if (memoryManager == null)
        {
            memoryManager = LLMCharacterMemoryManager.Instance;
            if (memoryManager == null)
            {
                Debug.LogError("LLMCharacterMemoryManager is not found in Awake. Ensure it is initialized in the MainMenu scene.");
            }
            else
            {
                Debug.Log("LLMCharacterMemoryManager successfully assigned in AIAgentManager.");
            }
        }
    }

    private void Start()
    {
        if (LLMCharacterMemoryManager.Instance == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("LLMCharacterMemoryManager not found. Creating a new instance for testing.");
            GameObject memoryManagerGO = new GameObject("LLMCharacterMemoryManager");
            memoryManager = memoryManagerGO.AddComponent<LLMCharacterMemoryManager>();
#else
            Debug.LogError("LLMCharacterMemoryManager not found. Ensure the Main Menu scene is loaded first.");
#endif
        }
        else
        {
            memoryManager = LLMCharacterMemoryManager.Instance;
        }

        if (memoryManager != null)
        {
            Debug.Log("LLMCharacterMemoryManager successfully assigned.");
        }

        if (selectAIButton != null)
        {
            selectAIButton.interactable = true;
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
        if (aiDropdown == null)
        {
            Debug.LogError("AI Dropdown is not assigned.");
            return;
        }

        if (AIManager.Instance != null)
        {
            AIManager.Instance.LoadAllAgents();  // Force load agents each time
            aiDropdown.ClearOptions();
            aiDropdown.AddOptions(AIManager.Instance.GetAllAgentNames());
            Debug.Log("AI Dropdown populated with saved agents.");
        }
        else
        {
            Debug.LogError("AIManager.Instance is null. Cannot populate dropdown.");
        }
    }




    public void OnSelectAIClicked()
    {
        if (aiDropdown != null && aiDropdown.options.Count > 0)
        {
            int selectedIndex = aiDropdown.value;
            string selectedAgent = aiDropdown.options[selectedIndex].text;

            Debug.Log($"Selected agent from dropdown: '{selectedAgent}'");

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
                if (PicoDialogue.Instance != null)
                {
                    PicoDialogue.Instance.InitializeReferences();

                    if (PicoDialogue.Instance.llmCharacter == null)
                    {
                        Debug.LogWarning("PicoDialogue's llmCharacter is null. Attempting to re-initialize.");
                        PicoDialogue.Instance.InitializeReferences();
                    }

                    if (PicoDialogue.Instance.llmCharacter != null)
                    {
                        agent.SetLLMCharacter(PicoDialogue.Instance.llmCharacter);
                        agentInteraction.SetAIAgent(agent);

                        PicoDialogue.Instance.SetAgentData(agent);
                        agent.llmCharacter.Load($"{agent.AgentId}_chatHistory");
                        Debug.Log($"PicoDialogue NPC data updated with agent '{agent.AgentName}', and chat history loaded.");
                    }
                    else
                    {
                        Debug.LogError("PicoDialogue's llmCharacter is still null after re-initialization. Cannot assign LLMCharacter to agent.");
                    }
                }
                else
                {
                    Debug.LogError("PicoDialogue instance is null. Cannot assign LLMCharacter to agent.");
                }
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

    public async void SaveAgentData(AIAgent agent)
    {
        if (agent.llmCharacter.chat == null || agent.llmCharacter.chat.Count == 0)
        {
            Debug.LogWarning("No chat history to save.");
            return;
        }

        foreach (var message in agent.llmCharacter.chat)
        {
            string sender = message.role == agent.llmCharacter.playerName ? "User" : agent.AgentName;
            string messageContent = message.content;
            string messageId = Guid.NewGuid().ToString();

            var messageEntry = new MessageEntry(sender, messageContent, messageId);

            await memoryManager.EmbedMessageAsync(messageEntry, agent.AgentId);
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("Application is pausing. Saving all agent data...");
            SaveAllAgentsData();
        }

    }
    public async Task<string> RetrieveContextForAgent(string agentId, string userInput)
    {
        AIAgent agent = AIManager.Instance?.GetAIAgentByName(agentId);
        if (agent == null)
        {
            Debug.LogError($"Agent with ID '{agentId}' not found.");
            return "";
        }

        return await agent.RetrieveRelevantContextAsync(userInput, agentId);
    }

    // Optional: Keep OnApplicationQuit if you want redundancy
    private void OnApplicationQuit()
    {
        Debug.Log("Application is quitting. Saving all agent data...");
        SaveAllAgentsData();
    }

    private void SaveAllAgentsData()
    {
        // Save data for each spawned agent
        foreach (GameObject agentObject in spawnedAgents)
        {
            if (agentObject != null)
            {
                AIAgentInteraction agentInteraction = agentObject.GetComponent<AIAgentInteraction>();
                if (agentInteraction != null && agentInteraction.AssignedAgent != null)
                {
                    SaveAgentData(agentInteraction.AssignedAgent);
                    Debug.Log($"Data saved for agent '{agentInteraction.AssignedAgent.AgentName}'.");
                }
            }
        }
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
        spawnedAgents.Clear();
    }
}
