using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using LLMUnity;
using UnityEngine.Networking; // For Android compatibility

public static class SaveSystem
{
    private static string path = Path.Combine(Application.persistentDataPath, "agentData");

    public static string GetAgentDataPath()
    {
        return path;
    }

    public static void SaveAllAgents(List<AIAgent> agents)
    {
        if (agents == null || agents.Count == 0)
        {
            Debug.LogError("No agents to save.");
            return;
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        foreach (AIAgent agent in agents)
        {
            SaveAgentData(agent);
        }
    }

    public static void SaveAgentData(AIAgent agent)
    {
        if (agent == null)
        {
            Debug.LogError("SaveAgentData called with null agent.");
            return;
        }

        if (string.IsNullOrEmpty(agent.AgentId))
        {
            Debug.LogError("AgentId is null or empty.");
            return;
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string filePath = Path.Combine(path, $"{agent.AgentId}.json");

        try
        {
            AIAgentData data = new AIAgentData(agent);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
            Debug.Log($"Agent data saved successfully to {filePath}.");

            // Save LLM chat history
            if (agent.llmCharacter != null)
            {
                agent.llmCharacter.Save($"{agent.AgentId}_chatHistory");
                Debug.Log($"Chat history saved for {agent.AgentId}.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save agent data: {ex.Message}");
        }
    }

    public static List<AIAgent> LoadAllAgents()
    {
        List<AIAgent> agents = new List<AIAgent>();

        if (!Directory.Exists(path))
        {
            Debug.LogWarning($"Agent data directory '{path}' not found. No agents to load.");
            return agents;
        }

        string[] files = Directory.GetFiles(path, "*.json");

        foreach (string filePath in files)
        {
            AIAgent agent = LoadAgentData(filePath);
            if (agent != null)
            {
                agents.Add(agent);
            }
        }

        return agents;
    }

    public static AIAgent LoadAgentData(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                string json = ReadFile(filePath); // Use custom method for Android compatibility
                AIAgentData data = JsonConvert.DeserializeObject<AIAgentData>(json);
                if (data != null)
                {
                    // Replace Object.FindObjectOfType with PicoDialogue.Instance.llmCharacter
                    if (PicoDialogue.Instance == null || PicoDialogue.Instance.llmCharacter == null)
                    {
                        Debug.LogError("SaveSystem: PicoDialogue.Instance or its LLMCharacter is null.");
                        return null;
                    }
                    LLMCharacter llmCharacter = PicoDialogue.Instance.llmCharacter;

                    LLMCharacterMemoryManager memoryManager = Object.FindObjectOfType<LLMCharacterMemoryManager>();

                    if (memoryManager == null)
                    {
                        Debug.LogError("SaveSystem: LLMCharacterMemoryManager not found in the scene.");
                        return null;
                    }

                    if (llmCharacter != null && memoryManager != null)
                    {
                        // Create a new AIAgent instance
                        AIAgent agent = new AIAgent(data.agentId, data.agentName, llmCharacter, data.personality, data.background, memoryManager);

                        agent.UserConversations = data.userConversations;

                        // Load LLM chat history
                        agent.llmCharacter.Load($"{agent.AgentId}_chatHistory");
                        Debug.Log($"Chat history loaded for {agent.AgentId}.");

                        Debug.Log($"Agent data loaded successfully from {filePath}.");

                        return agent;
                    }
                    else
                    {
                        Debug.LogError("LLMCharacter or LLMCharacterMemoryManager instance not found in the scene.");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to deserialize agent data from {filePath}.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load agent data: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"No saved data for agent found at '{filePath}'.");
        }
        return null;
    }

    // Method to handle reading files, ensuring compatibility across platforms
    private static string ReadFile(string filePath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // For Android, use UnityWebRequest
        string json = "";
        UnityWebRequest request = UnityWebRequest.Get("file://" + filePath);
        request.SendWebRequest();
        while (!request.isDone) { } // Wait for request to complete

        if (request.result == UnityWebRequest.Result.Success)
        {
            json = request.downloadHandler.text;
        }
        else
        {
            Debug.LogError($"Failed to load file at {filePath}: {request.error}");
        }
        return json;
#else
        // For other platforms, use File.ReadAllText
        return File.ReadAllText(filePath);
#endif
    }
}

[System.Serializable]
public class AIAgentData
{
    public string agentId;
    public string agentName;
    public string personality;
    public string background;
    public Dictionary<string, List<string>> userConversations;

    public AIAgentData(AIAgent agent)
    {
        agentId = agent.AgentId;
        agentName = agent.AgentName;
        personality = agent.Personality;
        background = agent.Background;
        userConversations = agent.UserConversations;
    }
}
