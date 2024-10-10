// LLMCharacterMemoryManager.cs
using RAGSearchUnity;
using System.IO;
using UnityEngine;
using LLMUnity; // Include LLMUnity
using Unity.Sentis;
using System.Collections.Generic;

public class LLMCharacterMemoryManager : MonoBehaviour
{
    public Embedding embeddingModel;
    private SearchEngine searchEngine;

    private void Start()
    {
        if (embeddingModel != null)
        {
            EmbeddingModel model = embeddingModel.GetModel();
            searchEngine = new SearchEngine(model);
        }
        else
        {
            Debug.LogError("Embedding model is not assigned.");
        }
    }

    public void LoadSearchEngine(string agentId)
    {
        string embeddingFilePath = GetEmbeddingFilePath(agentId);
        if (File.Exists(embeddingFilePath))
        {
            EmbeddingModel model = embeddingModel.GetModel();
            searchEngine = SearchEngine.Load(model, embeddingFilePath);
            Debug.Log($"Loaded embeddings for agent '{agentId}'.");
        }
        else
        {
            Debug.LogWarning($"No embeddings found for agent '{agentId}'. Starting with an empty search engine.");
            EmbeddingModel model = embeddingModel.GetModel();
            searchEngine = new SearchEngine(model);
        }
    }

    public void SaveSearchEngine(string agentId)
    {
        string embeddingFilePath = GetEmbeddingFilePath(agentId);
        searchEngine.Save(embeddingFilePath);
        Debug.Log($"Saved embeddings for agent '{agentId}' to '{embeddingFilePath}'.");
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

            // Add to the search engine
            searchEngine.Add(chatHistory);

            // Save the updated search engine
            SaveSearchEngine(agentId);
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

        string[] results = searchEngine.Search(query, resultCount);
        if (results.Length > 0)
        {
            return results[0];
        }
        return string.Empty;
    }
}
