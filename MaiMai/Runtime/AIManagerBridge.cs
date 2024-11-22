using UnityEngine;
using MFPS;
using LLMUnity;
using System.Threading.Tasks;

public static class AIManagerBridge
{
    // Static method to get an AIAgent by ID
    public static AIAgent GetAIAgentById(string agentId)
    {
        return AIManager.Instance.GetAIAgentById(agentId);
    }

    // Static method to get an AIAgent by Name (optional)
    public static AIAgent GetAIAgentByName(string agentName)
    {
        return AIManager.Instance.GetAIAgentByName(agentName);
    }

    // Static method to retrieve agent context
    public static async Task<string> RetrieveAgentContextAsync(string agentId, string userInput)
    {
        var agent = GetAIAgentById(agentId);
        if (agent != null)
        {
            return await agent.RetrieveRelevantContextAsync(userInput, agentId);
        }
        else
        {
            Debug.LogError($"Agent with ID '{agentId}' not found.");
            return "";
        }
    }
}
