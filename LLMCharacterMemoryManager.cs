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
            // DontDestroyOnLoad(gameObject); // Persist across scenes
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
            rag = gameObject.AddComponent<RAG>();
            rag.Init(SearchMethods.DBSearch, ChunkingMethods.SentenceSplitter);
            Debug.Log("LLMCharacterMemoryManager: RAG initialized successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"LLMCharacterMemoryManager: Failed to initialize RAG. Exception: {ex.Message}");
        }
    }

    public async Task EmbedMessageAsync(MessageEntry messageEntry, string agentId)
    {
        if (rag != null)
        {
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
        else
        {
            Debug.LogError("RAG is not initialized.");
        }
    }

    public async Task<string[]> SearchChatHistoryAsync(string query, int resultCount = 3, List<string> excludeMessages = null)
    {
        if (rag != null)
        {
            (string[] results, float[] distances) = await rag.Search(query, resultCount);

            // Exclude recent messages based on message content
            if (excludeMessages != null && excludeMessages.Count > 0)
            {
                results = results.Where(r => !excludeMessages.Contains(r)).ToArray();
            }

            return results;
        }
        else
        {
            Debug.LogError("RAG is not initialized. Cannot perform search.");
        }
        return new string[0];
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
}
