using UnityEngine;

public class ToggleAIUIButton : MonoBehaviour
{
    [SerializeField] private GameObject AI_UI_BUTTON; // Assigned in Inspector or dynamically found at runtime.

    // Method to toggle the AI_UI_BUTTON GameObject
    public void ToggleAIUIButtonVisibility()
    {
        // Ensure the AI_UI_BUTTON reference is valid
        if (AI_UI_BUTTON == null)
        {
            FindAIUIButton();
        }

        if (AI_UI_BUTTON != null)
        {
            Debug.Log("Button clicked: Toggling panel visibility");
            // Toggle the active state of the AI_UI_BUTTON GameObject
            bool newState = !AI_UI_BUTTON.activeSelf;
            AI_UI_BUTTON.SetActive(newState);
            Debug.Log("AI_UI_BUTTON active state is now: " + newState);
        }
        else
        {
            Debug.LogWarning("AI_UI_BUTTON is not assigned and cannot be toggled.");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Attempt to find AI_UI_BUTTON dynamically if it's not assigned
        if (AI_UI_BUTTON == null)
        {
            FindAIUIButton();
        }

        if (AI_UI_BUTTON != null)
        {
            // Optionally set the AI_UI_BUTTON to inactive at the start
            AI_UI_BUTTON.SetActive(false);
            Debug.Log("AI_UI_BUTTON set to inactive at Start");
        }
        else
        {
            Debug.LogWarning("AI_UI_BUTTON is not assigned in the Inspector or dynamically found at Start.");
        }
    }

    // Helper method to find the AI_UI_BUTTON GameObject by name
    private void FindAIUIButton()
    {
        AI_UI_BUTTON = GameObject.Find("AI_UI_BUTTON");
        if (AI_UI_BUTTON != null)
        {
            Debug.Log("AI_UI_BUTTON dynamically found and assigned: " + AI_UI_BUTTON.name);
        }
        else
        {
            Debug.LogWarning("AI_UI_BUTTON could not be found dynamically in the scene!");
        }
    }
}
