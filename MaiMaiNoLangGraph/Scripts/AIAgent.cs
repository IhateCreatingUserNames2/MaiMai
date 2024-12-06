using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;

[System.Serializable]
public class AIAgent
{
    public string AgentId { get; private set; }
    public string AgentName { get; private set; } // For user control/display
    public LLMCharacter llmCharacter { get; private set; }
    public string systemPrompt;

    // Stores user conversations
    public Dictionary<string, List<MessageEntry>> UserConversations { get; set; }
    private Dictionary<string, LLMCharacter> userCharacters = new Dictionary<string, LLMCharacter>();

    // Memory Manager for managing chat history
    public LLMCharacterMemoryManager memoryManager;

    private const int MaxContextLength = 2048;

    // Updated constructor to accept AgentName
    public AIAgent(string agentId, string agentName, LLMCharacter character, string customPrompt, LLMCharacterMemoryManager memoryMgr)
    {
        AgentId = agentId;
        AgentName = agentName; // Assign AgentName for control/display
        llmCharacter = character ?? throw new ArgumentNullException(nameof(character), "llmCharacter cannot be null");
        systemPrompt = customPrompt;
        llmCharacter.AIName = "assistant";
        llmCharacter.playerName = "user";
        UserConversations = new Dictionary<string, List<MessageEntry>>();
        userCharacters = new Dictionary<string, LLMCharacter>();
        memoryManager = memoryMgr ?? LLMCharacterMemoryManager.Instance;

        if (memoryManager == null)
        {
            Debug.LogError("AIAgent constructor: LLMCharacterMemoryManager instance is null.");
        }

        SetupSystemPrompt(agentId, customPrompt);
    }

    public void SetLLMCharacter(LLMCharacter character)
    {
        llmCharacter = character ?? throw new ArgumentNullException(nameof(character), "llmCharacter cannot be null");
        llmCharacter.AIName = "assistant";
    }

    public void SetupSystemPrompt(string userId, string customPrompt)
    {
        systemPrompt = customPrompt;
        llmCharacter.SetPrompt(systemPrompt.Trim(), clearChat: true);

        Debug.Log($"System Prompt Set for user {userId}: \n{customPrompt}");
    }

    public async Task<string> Interact(string userId, string message, string context = "")
    {
        if (userCharacters == null)
        {
            userCharacters = new Dictionary<string, LLMCharacter>();
        }

        if (llmCharacter == null)
        {
            Debug.LogError("llmCharacter is null. Ensure it is properly assigned before cloning.");
            return "Error: AI agent is not properly initialized.";
        }

        if (!userCharacters.ContainsKey(userId))
        {
            LLMCharacter userCharacter = UnityEngine.Object.Instantiate(llmCharacter);
            userCharacter.AIName = AgentName;
            userCharacter.playerName = "User";
            userCharacter.llm = llmCharacter.llm;
            userCharacters[userId] = userCharacter;
        }

        LLMCharacter userLlmCharacter = userCharacters[userId];

        // Retrieve context dynamically (if RAG is available)
        string retrievedContext = await RetrieveRelevantContextAsync(message, userId);

        // If context is empty, check and retrieve fixed memory context
        if (string.IsNullOrEmpty(context) && memoryManager != null)
        {
            Debug.Log("No context passed; attempting to retrieve fixed memory context.");
            context = await memoryManager.RetrieveFixedMemoryContext(message);
        }


        string combinedContext = await RetrieveRelevantContextAsync(message, userId);
        string prompt = ConstructConversationPromptWithEmbedding(userId, combinedContext, context);


        // Trim the prompt if it exceeds maximum length
        prompt = TrimPromptToMaxLength(prompt, userId);

        // Set the custom prompt for this interaction
        userLlmCharacter.SetPrompt(prompt, clearChat: false);

        string aiResponse = "";
        try
        {
            aiResponse = await userLlmCharacter.Chat(message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during LLM chat: {ex.Message}");
            return "Sorry, there was an error processing your request.";
        }

        // Update the conversation history
        if (!UserConversations.ContainsKey(userId))
        {
            UserConversations[userId] = new List<MessageEntry>();
        }
        UserConversations[userId].Add(new MessageEntry("User", message, Guid.NewGuid().ToString()));
        UserConversations[userId].Add(new MessageEntry(AgentName, aiResponse, Guid.NewGuid().ToString()));

        // Save the conversation to memory using RAG
        await SaveConversationAsync(userId);

        return aiResponse;
    }






    public async Task<string> RetrieveRelevantContextAsync(string query, string userId)
    {
        if (memoryManager == null)
        {
            Debug.LogError("Memory Manager is not initialized.");
            return "";
        }

        // Perform searches in both Fixed Memory and RAG memory in parallel
        var ragSearchTask = memoryManager.SearchChatHistoryAsync(query, 5);
        var fixedMemorySearchTask = memoryManager.RetrieveFixedMemoryContext(query);

        await Task.WhenAll(ragSearchTask, fixedMemorySearchTask);

        string[] ragResults = ragSearchTask.Result ?? Array.Empty<string>();
        string fixedMemoryResults = fixedMemorySearchTask.Result;

        // Combine results
        StringBuilder combinedContext = new StringBuilder("Relevant Context:\n");

        // Add RAG results
        if (ragResults.Length > 0)
        {
            combinedContext.AppendLine("From RAG Memory:");
            foreach (var result in ragResults)
            {
                combinedContext.AppendLine($"- {result}");
            }
        }
        else
        {
            combinedContext.AppendLine("No results found in RAG Memory.");
        }

        // Add Fixed Memory results
        if (!string.IsNullOrEmpty(fixedMemoryResults))
        {
            combinedContext.AppendLine("\nFrom Fixed Memory:");
            combinedContext.AppendLine(fixedMemoryResults);
        }
        else
        {
            combinedContext.AppendLine("\nNo results found in Fixed Memory.");
        }

        return combinedContext.ToString();
    }



    private async Task SaveConversationAsync(string userId)
    {
        if (memoryManager == null)
        {
            Debug.LogWarning("Memory Manager is not initialized. Conversation is not saved.");
            return;
        }

        // Get recent messages to embed in RAG
        List<MessageEntry> messagesToEmbed = GetRecentMessages(userId, excludeAI: false);

        foreach (var messageEntry in messagesToEmbed)
        {
            // Add labeled context to RAG
            string labeledMessage = $"From {messageEntry.Sender}: \"{messageEntry.Message}\"";
            await memoryManager.EmbedMessageAsync(new MessageEntry(messageEntry.Sender, labeledMessage, messageEntry.MessageId), AgentId);
        }
    }

    private string ConstructConversationPromptWithEmbedding(string userId, string retrievedContext, string context)
    {
        // Get recent messages for conversation history
        List<MessageEntry> recentConversation = GetRecentMessages(userId, maxMessages: 6);
        StringBuilder conversationHistory = new StringBuilder();

        // Format the conversation history
        foreach (var entry in recentConversation)
        {
            string senderLabel = entry.Sender == "User" ? "User" : AgentName;
            conversationHistory.AppendLine($"{senderLabel}: \"{entry.Message.Trim()}\"");
        }

        // Label the retrieved context (if any)
        string retrievedContextSection = string.IsNullOrEmpty(retrievedContext)
            ? ""
            : $"Retrieved Context:\n{retrievedContext}\n\n";

        // Label the fixed memory context (if any)
        string fixedContextSection = string.IsNullOrEmpty(context)
            ? ""
            : $"Context from File:\n{context}\n\n";

        // Safeguard: If no context (retrieved or from file) is available
        if (string.IsNullOrEmpty(retrievedContextSection) && string.IsNullOrEmpty(fixedContextSection))
        {
            Debug.LogWarning("No context found: Falling back to default behavior.");
            retrievedContextSection = "Note: No additional context was found for this query.\n\n";
        }

        // Build the final prompt
        string prompt =
            $"Your name is: {AgentName}\n" +
            $"Your Custom Prompt: {systemPrompt}\n\n" +
            $"{retrievedContextSection}" +
            $"{fixedContextSection}" +
            $"As {AgentName}, please provide an appropriate response to the user's last message.\n\n" +
            "Respond in first person and do not include any conversation markers or role labels in your response.\n" +
            "Conversation History:\n\n" +
            conversationHistory.ToString();

        return prompt.Trim();
    }




    private List<MessageEntry> GetRecentMessages(string userId, int maxMessages = 10, bool excludeAI = false)
    {
        if (UserConversations.ContainsKey(userId))
        {
            var conversationList = UserConversations[userId];
            if (excludeAI)
            {
                conversationList = conversationList.Where(m => m.Sender != AgentName).ToList();
            }

            int startIndex = Math.Max(0, conversationList.Count - maxMessages);
            return conversationList.GetRange(startIndex, conversationList.Count - startIndex);
        }
        return new List<MessageEntry>();
    }

    private string TrimPromptToMaxLength(string prompt, string userId)
    {
        int promptLength = prompt.Length;
        if (promptLength <= MaxContextLength)
        {
            return prompt;
        }

        string[] sections = prompt.Split(new[] { "Conversation History:" }, StringSplitOptions.None);
        if (sections.Length < 2)
        {
            return prompt.Substring(prompt.Length - MaxContextLength);
        }

        if (!UserConversations.ContainsKey(userId) || UserConversations[userId].Count == 0)
        {
            return prompt.Substring(prompt.Length - MaxContextLength);
        }

        string header = sections[0];
        string conversation = sections[1];

        string[] conversationLines = conversation.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int linesToRemove = 0;

        while (promptLength > MaxContextLength && linesToRemove < conversationLines.Length)
        {
            linesToRemove++;
            string trimmedConversation = string.Join("\n", conversationLines.Skip(linesToRemove));

            prompt = $"{header}Conversation History:\n{trimmedConversation}\n\nUser: \"{UserConversations[userId].Last().Message.Trim()}\"\n\nAs {AgentName}, provide an appropriate response to the user's last message.\n\nPlease respond in first person and do not include any conversation markers or role labels in your response.";

            promptLength = prompt.Length;
        }

        return prompt;
    }
}

[System.Serializable]
public class MessageEntry
{
    public string Sender;
    public string Message;
    public string MessageId;

    public MessageEntry(string sender, string message, string messageId)
    {
        Sender = sender;
        Message = message;
        MessageId = messageId;
    }
}
