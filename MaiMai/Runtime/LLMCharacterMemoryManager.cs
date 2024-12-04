using LLMUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class LLMCharacterMemoryManager : MonoBehaviour
{
    public static LLMCharacterMemoryManager Instance { get; private set; }
    public RAG rag;

    private Dictionary<string, HashSet<string>> embeddedMessageIds = new Dictionary<string, HashSet<string>>();
    private List<string> fixedMemoryData = new List<string>(); //  fixed memory storage here

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

    public void InitializeFixedMemoryData(List<string> memoryData)
    {
        fixedMemoryData = memoryData ?? new List<string>();
        Debug.Log($"Fixed memory initialized with {fixedMemoryData.Count} entries.");
    }

    public async Task AddFixedMemoryToRAGAsync(List<string> fixedMemoryData)
    {
        if (fixedMemoryData == null || fixedMemoryData.Count == 0)
        {
            Debug.LogWarning("No fixed memory data provided.");
            return;
        }

        foreach (var memory in fixedMemoryData)
        {
            await rag.Add(memory, "fixed");
        }

        Debug.Log($"Added {fixedMemoryData.Count} fixed memory items to RAG.");
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



    public async Task<(string[] similarPhrases, float[] distances)> SearchFixedMemory(string query, int topResults = 3)
    {
        if (!EnsureRAGInitialized())
        {
            Debug.LogError("RAG is not initialized.");
            return (new string[0], new float[0]);
        }

        Debug.Log($"Searching fixed memory for query: {query}");

        // Use RAG to search the "fixed" memory group
        (string[] results, float[] distances) = await rag.Search(query, topResults, "fixed");

        Debug.Log($"Search results from fixed memory: {string.Join(", ", results)} with scores: {string.Join(", ", distances)}");

        return (results, distances);
    }


    public async Task<(string[] results, float[] distances)> SearchCombinedMemoryAsync(string query, int resultCount = 3)
    {
        if (!EnsureRAGInitialized())
        {
            Debug.LogError("RAG is not initialized.");
            return (new string[0], new float[0]);
        }

        // Search dynamic memory
        var dynamicSearch = rag.Search(query, resultCount);

        // Search fixed memory
        var fixedSearch = rag.Search(query, resultCount, "fixed");

        // Wait for both searches to complete
        await Task.WhenAll(dynamicSearch, fixedSearch);

        // Combine results
        var dynamicResults = await dynamicSearch;
        var fixedResults = await fixedSearch;

        string[] combinedResults = dynamicResults.Item1.Concat(fixedResults.Item1).Take(resultCount).ToArray();
        float[] combinedDistances = dynamicResults.Item2.Concat(fixedResults.Item2).Take(resultCount).ToArray();

        return (combinedResults, combinedDistances);
    }


    public async Task EmbedFileDataAsync(string memoryFileContent)
    {
        if (!EnsureRAGInitialized())
        {
            Debug.LogError("RAG is not initialized.");
            return;
        }

        string[] entries = memoryFileContent.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            string formattedEntry = entry.Trim();
            if (!string.IsNullOrEmpty(formattedEntry))
            {
                await rag.Add(formattedEntry, "fixed");
                Debug.Log($"Embedded file entry into RAG: {formattedEntry}");
            }
        }

        Debug.Log("All file entries embedded into RAG.");
    }



    private float CosineSimilarity(List<float> vectorA, List<float> vectorB)
    {
        if (vectorA.Count != vectorB.Count)
        {
            Debug.LogError("Embedding vectors must have the same length.");
            return 0;
        }

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < vectorA.Count; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0) return 0;

        return dotProduct / (Mathf.Sqrt(magnitudeA) * Mathf.Sqrt(magnitudeB));
    }

    public async Task EmbedFileMessageAsync(string memoryFileContent, string agentId)
    {
        if (!EnsureRAGInitialized())
        {
            Debug.LogError("Failed to embed file messages: RAG is not initialized.");
            return;
        }

        // Split the file content into separate entries using a delimiter (e.g., "---")
        string[] entries = memoryFileContent.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var entry in entries)
        {
            string formattedEntry = entry.Trim();
            if (string.IsNullOrEmpty(formattedEntry)) continue;

            // Wrap the entry with [start] and [/end] tags
            string taggedEntry = $"[start]\n{formattedEntry}\n[/end]";

            // Generate a unique ID for the entry (if not already provided)
            string entryId = Guid.NewGuid().ToString();

            // Check if the entry is already embedded
            if (!IsMessageEmbedded(agentId, entryId))
            {
                await rag.Add(taggedEntry); // Add the tagged entry to the RAG system
                MarkMessageAsEmbedded(agentId, entryId); // Track embedding
                Debug.Log($"Embedded tagged entry: {taggedEntry}");
            }
            else
            {
                Debug.Log($"Tagged entry already embedded: {taggedEntry}");
            }
        }

        Debug.Log($"All entries from file successfully embedded for agent '{agentId}'.");
    }




    public async Task<string> RetrieveFixedMemoryContext(string query)
    {
        if (fixedMemoryData == null || fixedMemoryData.Count == 0)
        {
            Debug.LogWarning("Fixed memory data is empty.");
            return "";
        }

        Debug.Log($"Starting search for query: {query} in fixed memory.");

        (string[] similarPhrases, float[] distances) = await SearchFixedMemory(query, 3); // Fixed method signature

        if (similarPhrases == null || similarPhrases.Length == 0)
        {
            Debug.LogWarning("No relevant context found in fixed memory.");
            return "";
        }

        Debug.Log($"Found {similarPhrases.Length} matching entries in fixed memory: {string.Join(", ", similarPhrases)}");
        return string.Join("\n", similarPhrases);
    }


}
