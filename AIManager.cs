// AIManager.cs
using System.Collections.Generic;
using UnityEngine;
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

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllAgents(); // Load agents when the game starts
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

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

    public List<string> GetAllAgentNames()
    {
        return new List<string>(aiAgents.Keys);
    }

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

    public void LoadAllAgents()
    {
        List<AIAgent> agents = SaveSystem.LoadAllAgents();
        foreach (AIAgent agent in agents)
        {
            if (PicoDialogue.Instance != null && PicoDialogue.Instance.llmCharacter != null)
            {
                agent.SetLLMCharacter(PicoDialogue.Instance.llmCharacter);
                // Load chat history
                agent.llmCharacter.Load($"{agent.AgentId}_chatHistory");
                Debug.Log($"Chat history loaded for agent '{agent.AgentName}'.");
            }
            else
            {
                Debug.LogError("AIManager: PicoDialogue.Instance or its LLMCharacter is null.");
                continue; // Skip registering this agent
            }

            RegisterAIAgent(agent);
            Debug.Log($"Loaded AI Agent '{agent.AgentName}' with chat history.");
        }
    }

}
