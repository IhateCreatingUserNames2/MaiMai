using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using LLMUnity;

public static class SaveSystem
{
    private static string path = Path.Combine(Application.persistentDataPath, "agentData");

    public static string GetAgentDataPath()
    {
        return path;
    }

    public static async Task SaveAllAgentsAsync(List<AIAgent> agents)
    {
        if (agents == null || agents.Count == 0)
        {
            Debug.LogWarning("No agents to save.");
            return;
        }

        foreach (AIAgent agent in agents)
        {
            if (!IsAgentValid(agent))
            {
                Debug.LogWarning($"Skipping invalid agent: {agent?.AgentName ?? "Unnamed"}.");
                continue;
            }

            await SaveAgentDataAsync(agent);
        }

        await SaveManifestAsync(agents.Where(IsAgentValid).ToList()); // Save only valid agents
    }

    public static async Task SaveAgentDataAsync(AIAgent agent)
    {
        if (agent == null || string.IsNullOrEmpty(agent.AgentId))
        {
            Debug.LogError("Cannot save null or invalid agent data.");
            return;
        }

        Directory.CreateDirectory(path);
        string filePath = Path.Combine(path, $"{agent.AgentId}.json");

        try
        {
            // Save agent's basic data
            AIAgentData data = new AIAgentData(agent);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            await WriteFileAsync(filePath, json);
            Debug.Log($"Agent data saved successfully to {filePath}.");

            // Save LLM chat history
            if (agent.llmCharacter != null)
            {
                await agent.llmCharacter.Save($"{agent.AgentId}_chatHistory");
                Debug.Log($"Chat history saved for {agent.AgentId}.");
            }

            // Save the RAG state
            if (agent.memoryManager?.rag != null)
            {
                agent.memoryManager.rag.Save($"{agent.AgentId}_ragState");
                Debug.Log($"RAG state saved for agent '{agent.AgentId}'.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save agent data: {ex.Message}");
        }
    }

    public static async Task SaveManifestAsync(List<AIAgent> agents)
    {
        try
        {
            List<string> agentIds = agents.Where(IsAgentValid).Select(agent => agent.AgentId).ToList();
            string manifestPath = Path.Combine(path, "agent_manifest.json");
            string json = JsonConvert.SerializeObject(agentIds, Formatting.Indented);
            await WriteFileAsync(manifestPath, json);
            Debug.Log("Agent manifest saved.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save agent manifest: {ex.Message}");
        }
    }

    public static async Task<List<AIAgent>> LoadAllAgentsAsync()
    {
        Debug.Log("Loading all agents from disk...");
        List<AIAgent> agents = new List<AIAgent>();

        string manifestPath = Path.Combine(path, "agent_manifest.json");

        if (!FileExists(manifestPath))
        {
            Debug.LogWarning("No agent manifest found.");
            return agents;
        }

        try
        {
            string json = await ReadFileAsync(manifestPath);
            List<string> agentIds = JsonConvert.DeserializeObject<List<string>>(json);

            foreach (string agentId in agentIds)
            {
                string filePath = Path.Combine(path, $"{agentId}.json");
                AIAgent agent = await LoadAgentDataAsync(filePath);

                if (agent != null)
                {
                    agents.Add(agent);
                }
                else
                {
                    Debug.LogWarning($"Skipping invalid or incomplete agent data for ID: {agentId}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load agent manifest: {ex.Message}");
        }

        return agents;
    }

    public static async Task<AIAgent> LoadAgentDataAsync(string filePath)
    {
        if (!FileExists(filePath))
        {
            Debug.LogWarning($"No saved data for agent found at '{filePath}'.");
            return null;
        }

        try
        {
            string json = await ReadFileAsync(filePath);
            AIAgentData data = JsonConvert.DeserializeObject<AIAgentData>(json) ?? new AIAgentData();

            // Log warnings for missing critical fields
            if (string.IsNullOrEmpty(data.agentId))
            {
                Debug.LogWarning($"Agent data at {filePath} is missing an AgentId. Assigning default value.");
                data.agentId = "unknown_id";
            }

            if (string.IsNullOrEmpty(data.agentName))
            {
                Debug.LogWarning($"Agent data at {filePath} is missing an AgentName. Assigning default value.");
                data.agentName = "Unnamed Agent";
            }

            // Create the agent
            if (PicoDialogue.Instance?.llmCharacter == null)
            {
                Debug.LogError("PicoDialogue or its LLMCharacter is null.");
                return null;
            }

            LLMCharacterMemoryManager memoryManager = LLMCharacterMemoryManager.Instance;

            if (memoryManager == null)
            {
                Debug.LogError("LLMCharacterMemoryManager not found in the scene.");
                return null;
            }

            AIAgent agent = new AIAgent(data.agentId, data.agentName, PicoDialogue.Instance.llmCharacter, data.customPrompt, memoryManager);

            agent.UserConversations = data.userConversations.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value ?? new List<MessageEntry>() // Ensure no null lists
            );

            // Load LLM chat history
            if (agent.llmCharacter != null)
            {
                await agent.llmCharacter.Load($"{agent.AgentId}_chatHistory");
                Debug.Log($"Chat history loaded for {agent.AgentId}.");
            }

            // Load the RAG state
            if (agent.memoryManager?.rag != null)
            {
                await agent.memoryManager.rag.Load($"{agent.AgentId}_ragState");
                Debug.Log($"RAG state loaded for agent '{agent.AgentId}'.");
            }

            Debug.Log($"Agent data loaded successfully from {filePath}.");
            return agent;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load agent data: {ex.Message}");
        }

        return null;
    }


    private static bool IsAgentValid(AIAgent agent)
    {
        return agent != null && !string.IsNullOrEmpty(agent.AgentId);
    }

    private static async Task<string> ReadFileAsync(string filePath)
    {
        return await Task.Run(() => File.ReadAllText(filePath));
    }

    private static async Task WriteFileAsync(string filePath, string content)
    {
        await Task.Run(() => File.WriteAllText(filePath, content));
    }

    private static bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }
}

[System.Serializable]
public class AIAgentData
{
    public string agentId;
    public string agentName;
    public string customPrompt;
    public Dictionary<string, List<MessageEntry>> userConversations;

    public AIAgentData(AIAgent agent)
    {
        agentId = agent?.AgentId ?? "unknown_id";
        agentName = agent?.AgentName ?? "Unnamed Agent";
        customPrompt = agent?.systemPrompt ?? "Default system prompt";
        userConversations = agent?.UserConversations ?? new Dictionary<string, List<MessageEntry>>();
    }

    // Fallback constructor for deserialization
    public AIAgentData()
    {
        agentId = "unknown_id";
        agentName = "Unnamed Agent";
        customPrompt = "Default system prompt";
        userConversations = new Dictionary<string, List<MessageEntry>>();
    }
}
