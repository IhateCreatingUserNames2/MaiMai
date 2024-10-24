using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using LLMUnity;
using UnityEngine.Networking;

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
            Debug.LogError("No agents to save.");
            return;
        }

        foreach (AIAgent agent in agents)
        {
            await SaveAgentDataAsync(agent);
        }

        await SaveManifestAsync(agents); // Save the manifest file
    }

    public static async Task SaveAgentDataAsync(AIAgent agent)
    {
        Directory.CreateDirectory(path);

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

        string filePath = Path.Combine(path, $"{agent.AgentId}.json");

        try
        {
            AIAgentData data = new AIAgentData(agent);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            await WriteFileAsync(filePath, json);
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

    public static async Task SaveManifestAsync(List<AIAgent> agents)
    {
        try
        {
            List<string> agentIds = agents.Select(agent => agent.AgentId).ToList();
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
        if (FileExists(filePath))
        {
            try
            {
                string json = await ReadFileAsync(filePath);
                AIAgentData data = JsonConvert.DeserializeObject<AIAgentData>(json);
                if (data != null)
                {
                    if (PicoDialogue.Instance == null || PicoDialogue.Instance.llmCharacter == null)
                    {
                        Debug.LogError("SaveSystem: PicoDialogue.Instance or its LLMCharacter is null.");
                        return null;
                    }
                    LLMCharacter llmCharacter = PicoDialogue.Instance.llmCharacter;

                    LLMCharacterMemoryManager memoryManager = LLMCharacterMemoryManager.Instance;

                    if (memoryManager == null)
                    {
                        Debug.LogError("SaveSystem: LLMCharacterMemoryManager not found in the scene.");
                        return null;
                    }

                    if (llmCharacter != null && memoryManager != null)
                    {
                        // Create a new AIAgent instance
                        AIAgent agent = new AIAgent(data.agentId, data.agentName, llmCharacter, data.personality, data.background, memoryManager);

                        agent.UserConversations = data.userConversations.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value
                        );

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

    // Asynchronous method to handle reading files
    private static async Task<string> ReadFileAsync(string filePath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        UnityWebRequest request = UnityWebRequest.Get("file://" + filePath);
        await request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            return request.downloadHandler.text;
        }
        else
        {
            Debug.LogError($"Failed to load file at {filePath}: {request.error}");
            return "";
        }
#else
        return await Task.Run(() => File.ReadAllText(filePath));
#endif
    }

    // Asynchronous method to handle writing files
    private static async Task WriteFileAsync(string filePath, string content)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(file))
                {
                    await writer.WriteAsync(content);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to write file at {filePath}: {ex.Message}");
        }
#else
        await Task.Run(() => File.WriteAllText(filePath, content));
#endif
    }

    // Method to check if a file exists, compatible with Android
    private static bool FileExists(string filePath)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        UnityWebRequest request = UnityWebRequest.Head("file://" + filePath);
        request.SendWebRequest();
        while (!request.isDone) { }
        return !(request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError);
#else
        return File.Exists(filePath);
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
    public Dictionary<string, List<MessageEntry>> userConversations;

    public AIAgentData(AIAgent agent)
    {
        agentId = agent.AgentId;
        agentName = agent.AgentName;
        personality = agent.Personality;
        background = agent.Background;
        userConversations = agent.UserConversations;
    }
}
