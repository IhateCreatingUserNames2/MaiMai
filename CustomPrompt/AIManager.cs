using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks; 
using LLMUnity;

public class AIManager : MonoBehaviour
{
    private static AIManager _instance;
    public static AIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AIManager>();
            }
            return _instance;
        }
    }

    private Dictionary<string, AIAgent> aiAgents = new Dictionary<string, AIAgent>();
    private bool agentsLoaded = false; // Flag to track whether agents have been loaded

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            if (!agentsLoaded) // Only load agents if they haven't been loaded already
            {
                LoadAllAgents();
                agentsLoaded = true; // Set flag to true after loading
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Register an AI Agent into the system
    public void RegisterAIAgent(AIAgent agent)
    {
        if (agent == null)
        {
            Debug.LogError("Attempted to register a null AIAgent.");
            return;
        }

        if (!aiAgents.ContainsKey(agent.AgentName))
        {
            aiAgents.Add(agent.AgentName, agent);
            Debug.Log($"AI Agent '{agent.AgentName}' registered successfully.");
        }
        else
        {
            Debug.LogWarning($"AI Agent '{agent.AgentName}' is already registered.");
        }
    }

    // Return a list of all AI Agent names
    public List<string> GetAllAgentNames()
    {
        return new List<string>(aiAgents.Keys);
    }

    // Return all registered agents
    public List<AIAgent> GetAllAgents() // Method to get all registered agents
    {
        return new List<AIAgent>(aiAgents.Values);
    }

    // Return an AI Agent by name
    public AIAgent GetAIAgentByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("Attempted to get an AI Agent with an empty name.");
            return null;
        }

        if (aiAgents.ContainsKey(name))
        {
            return aiAgents[name];
        }
        else
        {
            Debug.LogError($"AI Agent with name '{name}' not found. Available agents: {string.Join(", ", aiAgents.Keys)}");
            return null;
        }
    }

    // Load all agents from saved data
    public async void LoadAllAgents()
    {
        if (agentsLoaded)
        {
            Debug.Log("Agents already loaded. Skipping redundant load.");
            return; // Prevent redundant loading
        }

        List<AIAgent> agents = await SaveSystem.LoadAllAgentsAsync();

        foreach (AIAgent agent in agents)
        {
            if (PicoDialogue.Instance != null && PicoDialogue.Instance.llmCharacter != null)
            {
                agent.SetLLMCharacter(PicoDialogue.Instance.llmCharacter);
                agent.llmCharacter.Load($"{agent.AgentId}_chatHistory");
                Debug.Log($"Chat history loaded for agent '{agent.AgentName}'.");
            }
            else
            {
                Debug.LogError("AIManager: PicoDialogue.Instance or its LLMCharacter is null.");
                continue;
            }

            RegisterAIAgent(agent);
            Debug.Log($"Loaded AI Agent '{agent.AgentName}' with chat history.");
        }

        agentsLoaded = true; // Set flag to true after loading
    }
}
