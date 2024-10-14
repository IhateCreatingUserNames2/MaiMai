using System.Collections.Generic;
using UnityEngine;
using LLMUnity;
using System.Threading.Tasks;
using System;
using System.Linq;

[System.Serializable]
public class AIAgent
{
    public string AgentId { get; private set; }
    public string AgentName { get; private set; }
    public LLMCharacter llmCharacter { get; private set; }
    private bool isPromptSet = false;
    public string Personality { get; set; }
    public string Background { get; set; }

    // Stores user conversations
    public Dictionary<string, List<string>> UserConversations { get; set; } // Key: userId

    private LLMCharacterMemoryManager memoryManager;

    public AIAgent(string agentId, string agentName, LLMCharacter character, string personality, string background, LLMCharacterMemoryManager memoryMgr)
    {
        AgentId = agentId;
        AgentName = agentName;
        llmCharacter = character;
        llmCharacter.AIName = AgentName; // Ensure AIName is set
        Personality = personality;
        Background = background;
        UserConversations = new Dictionary<string, List<string>>();
        memoryManager = memoryMgr ?? LLMCharacterMemoryManager.Instance;

        if (memoryManager == null)
        {
            Debug.LogError("AIAgent constructor: LLMCharacterMemoryManager instance is null.");
        }
        SetupSystemPrompt();
    }

    public void SetLLMCharacter(LLMCharacter character)
    {
        llmCharacter = character;
        llmCharacter.AIName = AgentName; // Set AIName to AgentName
    }

    private void SetupSystemPrompt()
    {
        if (isPromptSet)
        {
            return; // Skip if prompt is already set
        }

        string initialPrompt = $"You are {AgentName}, Your Personality Description is: {Personality}. Your Background Description is: {Background}. Below are some Chat History, If Any, And the USER INPUT. Reply to the User INPUT. Pay Attention to Chat History.";
        llmCharacter.SetPrompt(initialPrompt);
        isPromptSet = true; // Ensure this is only set once
    }

    public async Task<string> Interact(string userId, string message)
    {
        string lastPartialReply = "";
        int previousLength = 0;

        // Add user input to conversation history
        AddToConversation(userId, $"User: {message}");

        // Retrieve relevant context using RAGSearchUnity
        string retrievedContext = RetrieveRelevantContext(message, userId);

        // Check if context retrieval failed or was empty
        if (retrievedContext == null)
        {
            Debug.LogWarning("No relevant context found for the current interaction.");
            retrievedContext = ""; // Use an empty context instead of null to avoid issues
        }

        // Construct prompt with chat history and retrieved context
        string fullPrompt = ConstructConversationPromptWithEmbedding(userId, retrievedContext);

        // Log the constructed prompt
        Debug.Log($"Constructed Prompt: {fullPrompt}");

        // Enable streaming and set stop tokens
        llmCharacter.stream = true;
        llmCharacter.stop = new List<string> { "\nUser:", "\n" + AgentName + ":" };
        llmCharacter.ignoreEos = false;
        llmCharacter.cachePrompt = false;

        // Use Complete method to send the full prompt
        await llmCharacter.Complete(fullPrompt,
            partialReply =>
            {
                if (!string.IsNullOrEmpty(partialReply))
                {
                    // Extract only the new text since the last callback
                    string newText = partialReply.Substring(previousLength);
                    previousLength = partialReply.Length;

                    // Append the new text to the lastPartialReply
                    lastPartialReply += newText;
                }
            });

        string aiResponse = "";

        if (!string.IsNullOrEmpty(lastPartialReply))
        {
            // Clean the AI's response
            aiResponse = lastPartialReply.Split(new[] { "\nUser:", "\n" + AgentName + ":" }, StringSplitOptions.None)[0].Trim();

            // Add the AI's response to the conversation history
            AddToConversation(userId, $"{AgentName}: {aiResponse}");

            // Save the conversation in the SearchEngine
            string conversation = string.Join("\n", UserConversations[userId]);
            SaveConversation(conversation);
        }

        return aiResponse;
    }

    // Retrieve relevant context using RAGSearchUnity
    private string RetrieveRelevantContext(string query, string userId)
    {
        // Use the memoryManager to search for relevant context
        string retrievedContext = memoryManager?.SearchChatHistory(query, AgentId);

        // Ensure retrievedContext is not null
        if (retrievedContext == null)
        {
            retrievedContext = ""; // Set to empty if no context is found
        }

        // Exclude recent chat history from retrieved context
        if (UserConversations.ContainsKey(userId))
        {
            var recentConversation = UserConversations[userId];
            foreach (var entry in recentConversation)
            {
                retrievedContext = retrievedContext.Replace(entry, "");
            }
        }

        // Clean up any extra whitespace
        retrievedContext = retrievedContext.Trim();

        return retrievedContext;
    }

    private void SaveConversation(string conversation)
    {
        // Use the memoryManager to embed and save the conversation
        if (memoryManager != null)
        {
            memoryManager.EmbedChatHistory(conversation, AgentId);
        }
        else
        {
            Debug.LogWarning("Memory Manager is not initialized. Conversation is not saved.");
        }
    }

    public void AddToConversation(string userId, string message)
    {
        if (!UserConversations.ContainsKey(userId))
        {
            UserConversations[userId] = new List<string>();
        }
        UserConversations[userId].Add(message);
    }

    private string ConstructConversationPromptWithEmbedding(string userId, string retrievedContext)
    {
        // Limit conversation history to the last 5 messages (10 lines: User and AI)
        string previousConversation = "";
        if (UserConversations.ContainsKey(userId))
        {
            var conversationList = UserConversations[userId];
            int maxLines = 10; // 5 messages * 2 lines (User and AI)
            int startIndex = Math.Max(0, conversationList.Count - maxLines);
            previousConversation = string.Join("\n", conversationList.GetRange(startIndex, conversationList.Count - startIndex));
        }

        // Remove overlap between retrievedContext and previousConversation
        if (!string.IsNullOrEmpty(retrievedContext))
        {
            // Split both texts into lines for comparison
            var contextLines = retrievedContext.Split('\n').Select(line => line.Trim()).ToList();
            var historyLines = previousConversation.Split('\n').Select(line => line.Trim()).ToList();

            // Remove lines from context that are present in history
            contextLines.RemoveAll(line => historyLines.Contains(line));

            // Reconstruct the retrieved context without duplicates
            retrievedContext = string.Join("\n", contextLines).Trim();
        }

        string context = string.IsNullOrEmpty(retrievedContext) ? "" : $"\n\n*** Relevant Context ***\n{retrievedContext}";

        // Construct the prompt
        string prompt = $"You are {AgentName}, an AI assistant. Personality: {Personality}. Background: {Background}{context}\n\n*** Chat History ***\n{previousConversation}\n\n{AgentName}:";

        // Trim leading and trailing whitespace
        prompt = prompt.Trim();

        // Log the constructed prompt
        Debug.Log($"Constructed Prompt: {prompt}");

        return prompt;
    }
}
