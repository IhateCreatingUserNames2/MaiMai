using LLMUnity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class LLMCharacterMemoryManager : MonoBehaviour
{
    public static LLMCharacterMemoryManager Instance { get; private set; }
    public RAG rag;

    private Dictionary<string, HashSet<string>> embeddedMessageIds = new Dictionary<string, HashSet<string>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Persist across scenes if required
            Debug.Log("LLMCharacterMemoryManager instance created.");
        }
        else if (Instance != this)
        {
            Debug.Log("Duplicate LLMCharacterMemoryManager instance detected and destroyed.");
            Destroy(gameObject); // Enforce singleton
        }
    }

    private void Start()
    {
        InitializeRAG();
    }

    private void InitializeRAG()
    {
        try
        {
            if (rag == null)
            {
                rag = gameObject.AddComponent<RAG>();
                rag.Init(SearchMethods.DBSearch, ChunkingMethods.SentenceSplitter);
                Debug.Log("LLMCharacterMemoryManager: RAG initialized successfully.");
            }
            else
            {
                Debug.Log("LLMCharacterMemoryManager: RAG already initialized.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"LLMCharacterMemoryManager: Failed to initialize RAG. Exception: {ex.Message}");
        }
    }

    public async Task EmbedMessageAsync(MessageEntry messageEntry, string agentId)
    {
        if (!EnsureRAGInitialized())
        {
            Debug.LogError("Failed to embed message: RAG is not initialized.");
            return;
        }

        if (!IsMessageEmbedded(agentId, messageEntry.MessageId))
        {
            await rag.Add(messageEntry.Message);
            MarkMessageAsEmbedded(agentId, messageEntry.MessageId);
            Debug.Log($"Message embedded in RAG for agent '{agentId}': {messageEntry.Message}");
        }
        else
        {
            Debug.Log("Message already embedded. Skipping duplication.");
        }
    }

    public async Task<string[]> SearchChatHistoryAsync(string query, int resultCount = 3, List<string> excludeMessages = null)
    {
        if (!EnsureRAGInitialized())
        {
            Debug.LogError("Failed to perform search: RAG is not initialized.");
            return new string[0];
        }

        (string[] results, float[] distances) = await rag.Search(query, resultCount);

        // Exclude recent messages based on message content
        if (excludeMessages != null && excludeMessages.Count > 0)
        {
            results = results.Where(r => !excludeMessages.Contains(r)).ToArray();
        }

        return results;
    }

    private bool IsMessageEmbedded(string agentId, string messageId)
    {
        if (embeddedMessageIds.TryGetValue(agentId, out var messageIds))
        {
            return messageIds.Contains(messageId);
        }
        return false;
    }

    private void MarkMessageAsEmbedded(string agentId, string messageId)
    {
        if (!embeddedMessageIds.ContainsKey(agentId))
        {
            embeddedMessageIds[agentId] = new HashSet<string>();
        }
        embeddedMessageIds[agentId].Add(messageId);
    }

    private bool EnsureRAGInitialized()
    {
        if (rag == null)
        {
            Debug.LogWarning("RAG is missing. Attempting to initialize...");
            try
            {
                InitializeRAG();
                return rag != null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize RAG. Exception: {ex.Message}");
                return false;
            }
        }
        return true;
    }
}
