// LLMCharacterMemoryManager.cs
using RAGSearchUnity;
using System.IO;
using UnityEngine;
using LLMUnity; // Include LLMUnity
using Unity.Sentis;
using System.Collections.Generic;

public class LLMCharacterMemoryManager : MonoBehaviour
{
    public static LLMCharacterMemoryManager Instance { get; private set; }

    public Embedding embeddingModel;
    private SearchEngine searchEngine;

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

        // Attempt to find the Embeddings component on RagSearchGO
        if (embeddingModel == null)
        {
            GameObject ragSearchGO = GameObject.Find("RagSearchGO");
            if (ragSearchGO != null)
            {
                embeddingModel = ragSearchGO.GetComponent<Embedding>();
                if (embeddingModel == null)
                {
                    Debug.LogError("LLMCharacterMemoryManager: Embedding component not found on RagSearchGO.");
                }
            }
            else
            {
                Debug.LogError("LLMCharacterMemoryManager: RagSearchGO GameObject not found.");
            }
        }

        // Initialization moved to Start()
    }

    private void Start()
    {
        InitializeSearchEngine();
    }

    private void InitializeSearchEngine()
    {
        // Fix 1: Ensure EmbeddingModel is properly initialized before using it.
        if (embeddingModel != null)
        {
            EmbeddingModel model = embeddingModel.GetModel();
            if (model != null)
            {
                // Ensure to handle cases where creating a new SearchEngine might fail
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
            else
            {
                Debug.LogError("LLMCharacterMemoryManager: EmbeddingModel.GetModel() returned null. Please ensure the embedding model is loaded properly.");
            }
        }
        else
        {
            Debug.LogError("LLMCharacterMemoryManager: Embedding model is not assigned. Cannot initialize SearchEngine without embeddings.");
        }
    }

    public void LoadSearchEngine(string agentId)
    {
        // Fix 2: Add checks to ensure embeddingModel is loaded and initialized properly.
        EmbeddingModel model = embeddingModel?.GetModel();
        if (model == null)
        {
            Debug.LogError("LLMCharacterMemoryManager: EmbeddingModel.GetModel() returned null. Cannot load search engine without a valid embedding model.");
            return;
        }

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

    public void SaveSearchEngine(string agentId)
    {
        // Fix 3: Validate that searchEngine exists before saving.
        if (searchEngine != null)
        {
            string embeddingFilePath = GetEmbeddingFilePath(agentId);
            searchEngine.Save(embeddingFilePath);
            Debug.Log($"Saved embeddings for agent '{agentId}' to '{embeddingFilePath}'.");
        }
        else
        {
            Debug.LogError("LLMCharacterMemoryManager: Cannot save embeddings. Search engine is not initialized.");
        }
    }

    public void EmbedChatHistory(string chatHistory, string agentId)
    {
        if (embeddingModel != null)
        {
            // Ensure the search engine is initialized
            if (searchEngine == null)
            {
                LoadSearchEngine(agentId);
            }

            if (searchEngine != null) // Add this check to ensure searchEngine was successfully initialized
            {
                searchEngine.Add(chatHistory);
                SaveSearchEngine(agentId);
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


    private string GetEmbeddingFilePath(string agentId)
    {
        return Path.Combine(Application.persistentDataPath, $"{agentId}_embeddings.json");
    }

    public string SearchChatHistory(string query, string agentId, int resultCount = 1)
    {
        LoadSearchEngine(agentId); // Ensure embeddings are loaded

        // Fix 4: Ensure the search engine is properly initialized before performing the search.
        if (searchEngine != null)
        {
            string[] results = searchEngine.Search(query, resultCount);
            if (results.Length > 0)
            {
                return results[0];
            }
            else
            {
                Debug.LogWarning($"LLMCharacterMemoryManager: No search results found for query '{query}' with agent '{agentId}'.");
            }
        }
        else
        {
            Debug.LogError("LLMCharacterMemoryManager: Search engine is not initialized. Cannot perform search.");
        }

        return string.Empty;
    }
}
