using System;
using UnityEngine;
using MaiMai.Core;

namespace MaiMai.Implementation
{
    /// <summary>
    /// Adapts the DialogueManager to implement the IDialogueUI interface
    /// required by the MaiMai AI system
    /// </summary>
    public class MaiMaiDialogueAdapter : MonoBehaviour, IDialogueUI
    {
        // Reference to the dialogue manager
        private DialogueManager _dialogueManager;

        // Event required by IDialogueUI
        public event Action<string> OnUserMessageSubmitted;

        private void Awake()
        {
            // Get reference to the DialogueManager
            _dialogueManager = GetComponent<DialogueManager>();

            if (_dialogueManager == null)
            {
                Debug.LogError("DialogueManager component not found on GameObject");
                return;
            }

            // Subscribe to the DialogueManager's message event
            _dialogueManager.OnMessageSubmitted += HandleMessageSubmitted;
        }

        private void OnDestroy()
        {
            // Unsubscribe from the DialogueManager's message event
            if (_dialogueManager != null)
            {
                _dialogueManager.OnMessageSubmitted -= HandleMessageSubmitted;
            }
        }

        private void HandleMessageSubmitted(string message)
        {
            // Forward the message to subscribers of the IDialogueUI event
            OnUserMessageSubmitted?.Invoke(message);
        }

        // IDialogueUI implementation

        public void ShowMessage(string message, string sender)
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.ShowMessage(message, sender);
            }
        }

        public void ShowInputField(bool show)
        {
            if (_dialogueManager != null && _dialogueManager.inputPanel != null)
            {
                _dialogueManager.inputPanel.SetActive(show);
            }
        }

        public void SetAgentName(string agentName)
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.SetAgentName(agentName);
            }
        }

        public void SetProcessingState(bool isProcessing)
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.SetProcessing(isProcessing);
            }
        }
    }
}