using UnityEngine;
using UnityEngine.UI;

public class ToggleAIUIButton : MonoBehaviour
{
    [SerializeField] private GameObject AI_UI_BUTTON;

    // Method to toggle the AI_UI_BUTTON GameObject
    public void ToggleAIUIButtonVisibility()
    {
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
            Debug.LogWarning("AI_UI_BUTTON is not assigned in the Inspector");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (AI_UI_BUTTON != null)
        {
            // Optionally set the AI_UI_BUTTON to inactive at the start
            AI_UI_BUTTON.SetActive(false);
            Debug.Log("AI_UI_BUTTON set to inactive at Start");
        }
        else
        {
            Debug.LogWarning("AI_UI_BUTTON is not assigned in the Inspector at Start");
        }
    }
}
