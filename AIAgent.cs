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
    public string AgentName { get; private set; }
    public LLMCharacter llmCharacter { get; private set; }
    private bool isPromptSet = false;
    public string Personality { get; set; }
    public string Background { get; set; }

    // Stores user conversations
    public Dictionary<string, List<MessageEntry>> UserConversations { get; set; }
    private Dictionary<string, LLMCharacter> userCharacters = new Dictionary<string, LLMCharacter>();


    private LLMCharacterMemoryManager memoryManager;

    // Maximum context length for the LLM model (adjust based on your model)
    private const int MaxContextLength = 2048;

    public AIAgent(string agentId, string agentName, LLMCharacter character, string personality, string background, LLMCharacterMemoryManager memoryMgr)
    {
        AgentId = agentId;
        AgentName = agentName;
        llmCharacter = character ?? throw new ArgumentNullException(nameof(character), "llmCharacter cannot be null");
        llmCharacter.AIName = "assistant"; // Standard role name
        llmCharacter.playerName = "user"; // Standard role name
        Personality = personality;
        Background = background;
        UserConversations = new Dictionary<string, List<MessageEntry>>();
        userCharacters = new Dictionary<string, LLMCharacter>();
        memoryManager = memoryMgr ?? LLMCharacterMemoryManager.Instance;

        if (memoryManager == null)
        {
            Debug.LogError("AIAgent constructor: LLMCharacterMemoryManager instance is null.");
        }

        SetupSystemPrompt();
    }

    public void SetLLMCharacter(LLMCharacter character)
    {
        llmCharacter = character ?? throw new ArgumentNullException(nameof(character), "llmCharacter cannot be null");
        llmCharacter.AIName = "assistant"; // Standard role name
    }

    private void SetupSystemPrompt()
    {
        // Set the system prompt for the LLMCharacter
        string systemPrompt = $@"
You are {AgentName}, an AI assistant with the following attributes:
- Personality: {Personality}
- Background: {Background}
";
        llmCharacter.SetPrompt(systemPrompt.Trim(), clearChat: true);
    }


    public async Task<string> Interact(string userId, string message)
    {
        // Ensure userCharacters is initialized
        if (userCharacters == null)
        {
            userCharacters = new Dictionary<string, LLMCharacter>();
        }

        // Ensure llmCharacter is not null
        if (llmCharacter == null)
        {
            Debug.LogError("llmCharacter is null. Ensure it is properly assigned before cloning.");
            return "Error: AI agent is not properly initialized.";
        }

        // Ensure llmCharacter for the user
        if (!userCharacters.ContainsKey(userId))
        {
            // Create a new instance for the user by cloning the existing llmCharacter
            LLMCharacter userCharacter = UnityEngine.Object.Instantiate(llmCharacter);
            userCharacter.AIName = "assistant";
            userCharacter.playerName = "user";
            userCharacter.SetPrompt(llmCharacter.prompt, clearChat: true);
            userCharacters[userId] = userCharacter;
        }

        LLMCharacter userLlmCharacter = userCharacters[userId];

        // Generate AI response
        string aiResponse = "";
        try
        {
            aiResponse = await userLlmCharacter.Chat(message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during LLM chat: {ex.Message}");
            return "Desculpe, ocorreu um erro ao processar sua solicitação.";
        }

        // Update UserConversations
        if (!UserConversations.ContainsKey(userId))
        {
            UserConversations[userId] = new List<MessageEntry>();
        }
        UserConversations[userId].Add(new MessageEntry(userLlmCharacter.playerName, message, Guid.NewGuid().ToString()));
        UserConversations[userId].Add(new MessageEntry(userLlmCharacter.AIName, aiResponse, Guid.NewGuid().ToString()));

        return aiResponse;
    }







    // Asynchronously retrieve relevant context
    private async Task<string> RetrieveRelevantContextAsync(string query, string userId)
    {
        // Get recent messages to exclude
        List<MessageEntry> recentMessages = GetRecentMessages(userId, excludeAI: false);
        List<string> excludeMessages = recentMessages.Select(m => m.Message).ToList();

        // Retrieve multiple relevant contexts
        string[] retrievedResults = await memoryManager?.SearchChatHistoryAsync(query, AgentId, 3, excludeMessages);

        // Combine the retrieved contexts
        string retrievedContext = "";
        if (retrievedResults != null && retrievedResults.Length > 0)
        {
            retrievedContext = string.Join("\n", retrievedResults).Trim();
            Debug.Log($"Retrieved Context:\n{retrievedContext}");
        }
        else
        {
            Debug.Log("No relevant context found for the current interaction.");
        }

        return retrievedContext;
    }

    private async Task SaveConversationAsync(string userId)
    {
        // Use the memoryManager to embed and save the conversation
        if (memoryManager != null)
        {
            List<MessageEntry> messagesToEmbed = GetRecentMessages(userId, excludeAI: false);

            // Embed each message individually
            foreach (var messageEntry in messagesToEmbed)
            {
                await memoryManager.EmbedMessageAsync(messageEntry, AgentId);
            }
        }
        else
        {
            Debug.LogWarning("Memory Manager is not initialized. Conversation is not saved.");
        }
    }

    public void AddToConversation(string userId, MessageEntry messageEntry)
    {
        if (!UserConversations.ContainsKey(userId))
        {
            UserConversations[userId] = new List<MessageEntry>();
        }
        UserConversations[userId].Add(messageEntry);
    }

    private string ConstructConversationPromptWithEmbedding(string userId, string retrievedContext)
    {
        // Limit conversation history to the last N messages
        List<MessageEntry> recentConversation = GetRecentMessages(userId, maxMessages: 6);

        // Build the conversation history string
        StringBuilder conversationHistory = new StringBuilder();

        // Build previous conversation pairs
        for (int i = 0; i < recentConversation.Count - 1; i += 2)
        {
            var userMessage = recentConversation[i];
            var assistantMessage = (i + 1) < recentConversation.Count ? recentConversation[i + 1] : null;

            conversationHistory.AppendLine($"User: \"{userMessage.Message.Trim()}\"");
            if (assistantMessage != null)
            {
                conversationHistory.AppendLine($"{AgentName}: \"{assistantMessage.Message.Trim()}\"");
            }
        }

        // Get the last user message
        var lastUserMessage = recentConversation.LastOrDefault(m => m.Sender == "User")?.Message.Trim();

        // Build the prompt
        string prompt = $@"
You are {AgentName}, an AI assistant with the personality: {Personality}, and background: {Background}.
You are engaging in a conversation with a user.

{(string.IsNullOrEmpty(retrievedContext) ? "" : "Here is some relevant context:\n" + retrievedContext + "\n")}

Conversation History:
{conversationHistory}

User: ""{lastUserMessage}""

As {AgentName}, provide an appropriate response to the user's last message.

Please respond in first person and do not include any conversation markers or role labels in your response.
";

        return prompt.Trim();
    }

    // Method to get recent messages
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

    // Method to trim the prompt to fit within the model's context length
    private string TrimPromptToMaxLength(string prompt, string userId)
    {
        int promptLength = prompt.Length;
        if (promptLength <= MaxContextLength)
        {
            return prompt;
        }

        // If the prompt is too long, remove older conversation history
        string[] sections = prompt.Split(new[] { "Conversation History:" }, StringSplitOptions.None);
        if (sections.Length < 2)
        {
            // Can't trim further
            return prompt.Substring(prompt.Length - MaxContextLength);
        }

        if (!UserConversations.ContainsKey(userId) || UserConversations[userId].Count == 0)
        {
            // Handle the case where there are no conversations
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

            // Ensure userId is available here
            prompt = $"{header}Conversation History:\n{trimmedConversation}\n\nUser: \"{UserConversations[userId].Last().Message.Trim()}\"\n\nAs {AgentName}, provide an appropriate response to the user's last message.\n\nPlease respond in first person and do not include any conversation markers or role labels in your response.";

            promptLength = prompt.Length;
        }

        return prompt;
    }

    // Post-process the AI's response to remove unwanted markers
    private string PostProcessResponse(string response)
    {
        // Remove any unwanted tokens or markers
        string[] unwantedMarkers = { "<|endoftext|>", "<|assistant|>", "<|user|>", "<|system|>" };
        foreach (var marker in unwantedMarkers)
        {
            response = response.Replace(marker, "");
        }

        // Trim whitespace
        response = response.Trim();

        return response;
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
