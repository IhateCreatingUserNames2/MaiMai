using RAGSearchUnity;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using LLMUnity;
using Unity.Sentis;
using System.Collections;

public class LLMCharacterMemoryManager : MonoBehaviour
{
    public static LLMCharacterMemoryManager Instance { get; private set; }

    public Embedding embeddingModel;
    private SearchEngine searchEngine;

    private Dictionary<string, HashSet<string>> embeddedMessageIds = new Dictionary<string, HashSet<string>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Enforce singleton
            return;
        }
    }

    private void Start()
    {
        // Start a coroutine to initialize the search engine
        StartCoroutine(InitializeSearchEngineCoroutine());
    }

    private IEnumerator InitializeSearchEngineCoroutine()
    {
        // Wait until the embedding model is ready
        while (embeddingModel == null || embeddingModel.GetModel() == null)
        {
            Debug.Log("LLMCharacterMemoryManager: Waiting for embedding model to be ready...");
            yield return null; // Wait for the next frame
        }

        // Now initialize the search engine
        EmbeddingModel model = embeddingModel.GetModel();
        try
        {
            searchEngine = new SearchEngine(model);
            Debug.Log("LLMCharacterMemoryManager: SearchEngine initialized successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"LLMCharacterMemoryManager: Failed to initialize SearchEngine. Exception: {ex.Message}");
        }
    }

    public async Task EmbedMessageAsync(MessageEntry messageEntry, string agentId)
    {
        // Wait until searchEngine is initialized
        while (searchEngine == null)
        {
            await Task.Yield();
        }

        if (embeddingModel != null)
        {
            if (searchEngine != null)
            {
                if (!IsMessageEmbedded(agentId, messageEntry.MessageId))
                {
                    // Asynchronously embed the message
                    await Task.Run(() => searchEngine.Add(messageEntry.Message));
                    await SaveSearchEngineAsync(agentId);

                    // Mark the message as embedded
                    MarkMessageAsEmbedded(agentId, messageEntry.MessageId);
                    Debug.Log($"Message embedded for agent '{agentId}': {messageEntry.Message}");
                }
                else
                {
                    Debug.Log("Message already embedded. Skipping duplication.");
                }
            }
            else
            {
                Debug.LogError("LLMCharacterMemoryManager: Search engine initialization failed.");
            }
        }
        else
        {
            Debug.LogError("Embedding model is not assigned.");
        }
    }

    public void LoadSearchEngine(string agentId)
    {
        if (embeddingModel == null || embeddingModel.GetModel() == null)
        {
            Debug.LogError("LLMCharacterMemoryManager: Embedding model is not ready. Cannot load search engine.");
            return;
        }

        EmbeddingModel model = embeddingModel.GetModel();
        string embeddingFilePath = GetEmbeddingFilePath(agentId);

        if (File.Exists(embeddingFilePath))
        {
            try
            {
                searchEngine = SearchEngine.Load(model, embeddingFilePath);
                Debug.Log($"Loaded embeddings for agent '{agentId}' from '{embeddingFilePath}'.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"LLMCharacterMemoryManager: Failed to load embeddings for agent '{agentId}'. Exception: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"No embeddings found for agent '{agentId}'. Starting with an empty search engine.");
            try
            {
                searchEngine = new SearchEngine(model);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"LLMCharacterMemoryManager: Failed to initialize SearchEngine. Exception: {ex.Message}");
            }
        }
    }

    public async Task SaveSearchEngineAsync(string agentId)
    {
        if (searchEngine != null)
        {
            string embeddingFilePath = GetEmbeddingFilePath(agentId);
            await Task.Run(() => searchEngine.Save(embeddingFilePath));
            Debug.Log($"Saved embeddings for agent '{agentId}' to '{embeddingFilePath}'.");
        }
        else
        {
            Debug.LogError("LLMCharacterMemoryManager: Cannot save embeddings. Search engine is not initialized.");
        }
    }

    public async Task<string[]> SearchChatHistoryAsync(string query, string agentId, int resultCount = 3, List<string> excludeMessages = null)
    {
        // Wait until searchEngine is initialized
        while (searchEngine == null)
        {
            await Task.Yield();
        }

        if (searchEngine != null)
        {
            string[] results = await Task.Run(() => searchEngine.Search(query, resultCount));

            // Exclude recent messages based on message content
            if (excludeMessages != null && excludeMessages.Count > 0)
            {
                results = results.Where(r => !excludeMessages.Contains(r)).ToArray();
            }

            return results;
        }
        else
        {
            Debug.LogError("LLMCharacterMemoryManager: Search engine is not initialized. Cannot perform search.");
        }

        return new string[0];
    }

    private bool IsMessageEmbedded(string agentId, string messageId)
    {
        if (embeddedMessageIds.TryGetValue(agentId, out var messageIds))
        {
            return messageIds.Contains(messageId);
        }
        else
        {
            return false;
        }
    }

    private void MarkMessageAsEmbedded(string agentId, string messageId)
    {
        if (!embeddedMessageIds.ContainsKey(agentId))
        {
            embeddedMessageIds[agentId] = new HashSet<string>();
        }
        embeddedMessageIds[agentId].Add(messageId);
    }

    private string GetEmbeddingFilePath(string agentId)
    {
        return Path.Combine(Application.persistentDataPath, $"{agentId}_embeddings.json");
    }
}
