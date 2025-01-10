using LLMUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Collider))] // Ensure the GameObject has a Collider for proximity detection
public class AIAgentInteraction : MonoBehaviour
{

    public enum InteractionMode { Proximity, KeyTrigger }

    [Header("Interaction Settings")]
    public InteractionMode interactionMode = InteractionMode.Proximity;
    public KeyCode keyTrigger = KeyCode.F; // Default key for KeyTrigger mode

    private bool playerInRange = false; // Tracks if player is in range for interaction


    private AIAgent assignedAgent;
    private PicoDialogue picoDialogue;
    public RAG rag;

    [Header("Fixed Agent Settings")]
    public bool isFixedAgent = false; // Toggle to indicate if the agent is predefined
    public string fixedAgentName; // Field for the agent's name (predefined)
    [TextArea]
    public string fixedAgentPrompt; // Field for the agent's custom prompt (predefined)

    [Header("Fixed Memory Settings")]
    public bool hasFixedMemory = false; // Toggle for fixed memory usage
    public List<TextAsset> memoryFiles; // Text files for RAG context

    private List<string> fixedMemoryData = new List<string>(); // Stores preloaded memory data

    public AIAgent AssignedAgent => assignedAgent;

    void Start()
    {
        picoDialogue = PicoDialogue.Instance;
        if (picoDialogue == null)
        {
            Debug.LogError("AIAgentInteraction: PicoDialogue.Instance is null. Ensure PicoDialogue is present in the scene.");
            return;
        }

        Debug.Log("PicoDialogue successfully assigned in AIAgentInteraction.");

        if (isFixedAgent)
        {
            StartCoroutine(RetryInitialization());
        }

        if (hasFixedMemory)
        {
            Debug.Log("Initializing fixed memory...");
            StartCoroutine(InitializeFixedMemoryFlow());
        }
    }

    void Update()
    {
        // Key Trigger Activation
        if (interactionMode == InteractionMode.KeyTrigger && playerInRange && Input.GetKeyDown(keyTrigger))
        {
            TriggerInteraction();
        }
    }


    private IEnumerator InitializeFixedMemoryFlow()
    {
        Debug.Log("Loading fixed memory...");
        yield return LoadFixedMemory();

        Debug.Log("Fixed memory successfully loaded and embedded.");
    }

    private IEnumerator RetryInitialization()
    {
        int retries = 10;
        while (retries > 0)
        {
            LLMCharacter llmCharacter = FindObjectOfType<LLMCharacter>();
            LLMCharacterMemoryManager memoryManager = LLMCharacterMemoryManager.Instance;

            if (llmCharacter != null && memoryManager != null)
            {
                Debug.Log("LLMCharacter and MemoryManager successfully found.");
                InitializeFixedAgent(llmCharacter, memoryManager);
                yield break;
            }

            retries--;
            yield return new WaitForSeconds(0.2f); // Retry after a short delay
        }

        Debug.LogError("LLMCharacter or MemoryManager could not be found after retries.");
    }

    public void SetAIAgent(AIAgent agent)
    {
        assignedAgent = agent;
        Debug.Log($"AI Agent '{agent.AgentName}' has been assigned to this interaction.");
    }

    private void InitializeFixedAgent(LLMCharacter llmCharacter, LLMCharacterMemoryManager memoryManager)
    {
        if (string.IsNullOrEmpty(fixedAgentName))
        {
            Debug.LogError("Fixed agent name is empty.");
            return;
        }

        if (AIManager.Instance == null)
        {
            Debug.LogError("AIManager instance is not found.");
            return;
        }

        // Check if agent already exists
        AIAgent existingAgent = AIManager.Instance.GetAIAgentByName(fixedAgentName);
        if (existingAgent != null)
        {
            Debug.Log($"Agent '{fixedAgentName}' already exists in AIManager.");
            assignedAgent = existingAgent;
            return;
        }

        // Create and register the agent
        assignedAgent = new AIAgent(
            System.Guid.NewGuid().ToString(),
            fixedAgentName,
            llmCharacter,
            fixedAgentPrompt,
            memoryManager
        );

        AIManager.Instance.RegisterAIAgent(assignedAgent);
        Debug.Log($"Fixed agent '{fixedAgentName}' successfully initialized.");
    }

    private async Task LoadFixedMemory()
    {
        Debug.Log($"Loading fixed memory for agent '{assignedAgent.AgentName}'...");
        fixedMemoryData.Clear();

        if (memoryFiles == null || memoryFiles.Count == 0)
        {
            Debug.LogWarning("No memory files provided.");
            return;
        }

        foreach (var file in memoryFiles)
        {
            if (file != null)
            {
                string fileContent = file.text;
                await LLMCharacterMemoryManager.Instance.EmbedFileMessageAsync(fileContent, assignedAgent.AgentId);
                fixedMemoryData.Add(fileContent);
                Debug.Log($"File '{file.name}' embedded into memory for agent '{assignedAgent.AgentName}'.");
            }
        }
        Debug.Log($"Fixed memory loaded with {fixedMemoryData.Count} entries for agent '{assignedAgent.AgentName}'.");
    }







    private bool EnsureRAGInitialized()
    {
        if (rag == null)
        {
            rag = gameObject.GetComponent<RAG>() ?? gameObject.AddComponent<RAG>();
            rag.Init(SearchMethods.DBSearch, ChunkingMethods.SentenceSplitter);
            Debug.Log("RAG system initialized.");
        }
        return rag != null;
    }



    public async Task Interact(string userId, string message, System.Action<string> onResponseComplete)
    {
        Debug.Log($"Interact called with userId: {userId}, message: {message}");

        if (assignedAgent == null)
        {
            Debug.LogError("No AI agent assigned to interact.");
            return;
        }

        // Ensure memory is loaded and embedded
        if (hasFixedMemory && (fixedMemoryData == null || fixedMemoryData.Count == 0))
        {
            Debug.LogWarning("Fixed memory not ready. Waiting for initialization...");
            await LoadFixedMemory(); // Ensure memory is loaded
        }

        // Pass an empty context to allow AIAgent to retrieve it
        string context = "";

        string response = await assignedAgent.Interact(userId, message, context);
        onResponseComplete?.Invoke(response);
    }





    // Handle player proximity to trigger interaction
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            // Shared logic: Set agent data and prepare PicoDialogue
            if (assignedAgent != null && picoDialogue != null)
            {
                picoDialogue.SetAgentData(assignedAgent);

                if (interactionMode == InteractionMode.Proximity)
                {
                    // Proximity: Trigger immediately
                    picoDialogue.OpenPlayerInputUI();
                    Debug.Log($"Proximity interaction: Interacting with AI Agent '{assignedAgent.AgentName}'.");
                }
                else if (interactionMode == InteractionMode.KeyTrigger)
                {
                    // KeyTrigger: Wait for user to press the key
                    Debug.Log($"Player in range. Press '{keyTrigger}' to interact with AI Agent '{assignedAgent.AgentName}'.");
                }
            }
            else
            {
                Debug.LogError("AIAgentInteraction: Missing assigned agent or PicoDialogue.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            picoDialogue?.HidePlayerInputUI();
            Debug.Log($"Player left interaction range of AI Agent '{gameObject.name}'.");
        }
    }


    private void TriggerInteraction()
    {
        if (assignedAgent != null && picoDialogue != null)
        {
            picoDialogue.SetAgentData(assignedAgent);
            picoDialogue.OpenPlayerInputUI();
            Debug.Log($"Interacting with AI Agent '{assignedAgent.AgentName}'.");
        }
        else
        {
            Debug.LogError("AIAgentInteraction: Assigned agent or PicoDialogue is missing.");
        }
    }


}
