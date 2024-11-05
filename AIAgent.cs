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

    private string systemPrompt;

    // Stores user conversations
    public Dictionary<string, List<MessageEntry>> UserConversations { get; set; }
    private Dictionary<string, LLMCharacter> userCharacters = new Dictionary<string, LLMCharacter>();

    // Change to public to allow SaveSystem to access it
    public LLMCharacterMemoryManager memoryManager;

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


    public void SetupSystemPrompt()
    {
        systemPrompt = $@"
You are now embodying the character **{AgentName}**. Your role is to interact with users, adhering strictly to the following guidelines:

**Personality Traits**:
{Personality}

**Background Story**:
{Background}

**Communication Style**:
- Speak in the first person singular.
- Maintain a consistent tone that reflects your personality.
- Be immersive and stay in character throughout the conversation.
- Avoid revealing that you are an AI language model.

**Instructions**:
- Always respond as {AgentName} would.
- Use knowledge from your background to inform your responses.
- If unsure about a topic, respond in a way that {AgentName} would naturally react.

Begin the interaction below.
";

        // Set the prompt in the llmCharacter
        llmCharacter.SetPrompt(systemPrompt.Trim(), clearChat: true);

        Debug.Log($"System Prompt Set for {AgentName} with Personality and Background: \n{systemPrompt}");
    }


    



    public async Task<string> Interact(string userId, string message)
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
            userCharacter.AIName = "assistant";
            userCharacter.playerName = "user";
            userCharacter.debugPrompt = true;

            // Set the custom prompt
            userCharacter.SetPrompt(systemPrompt.Trim(), clearChat: true);

            userCharacters[userId] = userCharacter;
        }

        LLMCharacter userLlmCharacter = userCharacters[userId];
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

        if (!UserConversations.ContainsKey(userId))
        {
            UserConversations[userId] = new List<MessageEntry>();
        }
        UserConversations[userId].Add(new MessageEntry(userLlmCharacter.playerName, message, Guid.NewGuid().ToString()));
        UserConversations[userId].Add(new MessageEntry(userLlmCharacter.AIName, aiResponse, Guid.NewGuid().ToString()));

        return aiResponse;
    }

    private async Task<string> RetrieveRelevantContextAsync(string query, string userId)
    {
        string[] retrievedResults = await memoryManager?.SearchChatHistoryAsync(query, 3);

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
        if (memoryManager != null)
        {
            List<MessageEntry> messagesToEmbed = GetRecentMessages(userId, excludeAI: false);

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
        List<MessageEntry> recentConversation = GetRecentMessages(userId, maxMessages: 6);
        StringBuilder conversationHistory = new StringBuilder();

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

        var lastUserMessage = recentConversation.LastOrDefault(m => m.Sender == "User")?.Message.Trim();

        string contextSection = string.IsNullOrEmpty(retrievedContext)
            ? ""
            : $"Here is some relevant context:\n{retrievedContext}\n\n";

        string prompt =
            $"You are {AgentName}, an AI assistant with the personality: {Personality} and background: {Background}. You are engaging in a conversation with a user.\n\n" +
            $"{contextSection}" +
            "Conversation History:\n" +
            $"{conversationHistory}\n\n" +
            $"User: \"{lastUserMessage}\"\n\n" +
            $"As {AgentName} ({Background}), please provide an appropriate response to the user's last message.\n\n" +
            "Respond in first person and do not include any conversation markers or role labels in your response.";

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

    private string PostProcessResponse(string response)
    {
        string[] unwantedMarkers = { "<|endoftext|>", "<|assistant|>", "<|user|>", "<|system|>" };
        foreach (var marker in unwantedMarkers)
        {
            response = response.Replace(marker, "");
        }

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
