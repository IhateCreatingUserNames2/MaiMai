// AIAgentInteraction.cs
using LLMUnity;
using System.Threading.Tasks;
using UnityEngine;

public class AIAgentInteraction : MonoBehaviour
{
    private AIAgent assignedAgent;
    private PicoDialogue picoDialogue;


    void Awake()
    {
        if (picoDialogue == null)
        {
            picoDialogue = PicoDialogue.Instance; // Ensure PicoDialogue implements a singleton
            if (picoDialogue == null)
            {
                Debug.LogError("AIAgentInteraction: PicoDialogue.Instance is null. Ensure PicoDialogue is present in the scene.");
            }
            else
            {
                Debug.Log("PicoDialogue successfully assigned in AIAgentInteraction.");
            }
        }
    }



    // Method to assign an AI agent to this interaction system
    public void SetAIAgent(AIAgent agent)
    {
        assignedAgent = agent;
        Debug.Log($"AI Agent '{agent.AgentName}' has been assigned to this interaction.");
    }

    // Interaction method
    // AIAgentInteraction.cs
    public async Task Interact(string userId, string message, System.Action<string> onResponseComplete)
    {
        if (assignedAgent == null)
        {
            Debug.LogError("No AI agent assigned to interact.");
            return;
        }

        // Update to match the new signature
        string response = await assignedAgent.Interact(userId, message);
        onResponseComplete?.Invoke(response);
    }


    // Track player proximity to determine active agent
    // Modify OnTriggerEnter method
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
                    picoDialogue.SetAgentData(assignedAgent); // Update active agent in PicoDialogue
                    picoDialogue.OpenPlayerInputUI(); // Open the input UI for interaction
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

    // Modify OnTriggerExit method
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (assignedAgent != null)
            {
                if (picoDialogue != null)
                {
                    Debug.Log($"Player left interaction zone of AI Agent '{assignedAgent.AgentName}'.");
                    picoDialogue.HidePlayerInputUI(); // Hide the input UI when the player leaves
                }
                else
                {
                    Debug.LogError("AIAgentInteraction: picoDialogue is not assigned.");
                }
            }
        }
    }
}

