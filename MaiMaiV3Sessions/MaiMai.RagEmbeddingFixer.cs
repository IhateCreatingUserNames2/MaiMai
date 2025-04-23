using UnityEngine;
using LLMUnity;
using MaiMai.Core;
using MaiMai.Implementation;

public class RAGEmbeddingFixer : MonoBehaviour
{
    [SerializeField] private LLM embeddingLLM;
    [SerializeField] private bool fixOnStart = true;

    private void Start()
    {
        if (fixOnStart)
        {
            FixRAGEmbeddings();
        }
    }

    public void FixRAGEmbeddings()
    {
        if (embeddingLLM == null)
        {
            Debug.LogError("Embedding LLM not assigned. Please assign an LLM with an embedding model.");
            return;
        }

        // Find all RAG components created by the RAGMemoryProvider
        RAG[] allRags = FindObjectsOfType<RAG>();
        if (allRags.Length == 0)
        {
            Debug.LogWarning("No RAG components found in scene.");
            return;
        }

        int fixedCount = 0;
        foreach (RAG rag in allRags)
        {
            if (rag.search != null && rag.search.llmEmbedder != null)
            {
                // Fix the embedding model reference
                rag.search.llmEmbedder.llm = embeddingLLM;
                fixedCount++;
                Debug.Log($"Fixed embeddings for RAG: {rag.name}");
            }
        }

        Debug.Log($"Fixed {fixedCount} RAG components");
    }
}