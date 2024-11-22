using LLMUnity;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Collider))] // Ensure the GameObject has a Collider for proximity detection
public class AIAgentInteraction : MonoBehaviour
{
    private AIAgent assignedAgent;
    private PicoDialogue picoDialogue;

    [Header("Fixed Agent Settings")]
    public bool isFixedAgent = false; // Toggle to indicate if the agent is predefined
    public string fixedAgentName; // Field for the agent's name (predefined)
    [TextArea]
    public string fixedAgentPrompt; // Field for the agent's custom prompt (predefined)

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



    // Method to assign an AI agent to this interaction system
    public void SetAIAgent(AIAgent agent)
    {
        assignedAgent = agent;
        Debug.Log($"AI Agent '{agent.AgentName}' has been assigned to this interaction.");
    }

    // Interaction method
    public async Task Interact(string userId, string message, System.Action<string> onResponseComplete)
    {
        if (assignedAgent == null)
        {
            Debug.LogError("No AI agent assigned to interact.");
            return;
        }

        string response = await assignedAgent.Interact(userId, message);
        onResponseComplete?.Invoke(response);
    }

    // Handle player proximity to trigger interaction
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject player = other.gameObject;
            string playerName = player.name;
            Debug.Log($"Player '{playerName}' interacted with AI Agent '{gameObject.name}'.");

            if (assignedAgent != null)
            {
                if (picoDialogue != null)
                {
                    picoDialogue.SetAgentData(assignedAgent);
                    picoDialogue.OpenPlayerInputUI();
                    Debug.Log($"Interacting with AI Agent '{assignedAgent.AgentName}' successfully.");
                }
                else
                {
                    Debug.LogError("AIAgentInteraction: picoDialogue is not assigned.");
                }
            }
            else
            {
                Debug.LogError("AIAgentInteraction: No AI agent is assigned to this interaction.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (assignedAgent != null)
            {
                if (picoDialogue != null)
                {
                    Debug.Log($"Player left interaction zone of AI Agent '{assignedAgent.AgentName}'.");
                    picoDialogue.HidePlayerInputUI();
                }
                else
                {
                    Debug.LogError("AIAgentInteraction: picoDialogue is not assigned.");
                }
            }
        }
    }
}
