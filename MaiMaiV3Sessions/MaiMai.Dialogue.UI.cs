using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaiMai.Core;

namespace MaiMai.Implementation
{
    /// <summary>
    /// Manages the UI for agent dialogue interactions
    /// </summary>
    public class DialogueUIManager : MonoBehaviour, IDialogueUI
    {
        public static DialogueUIManager Instance { get; private set; }
        public event Action<string> OnUserMessageSubmitted;

        [Header("Main UI")]
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI agentNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private MonoBehaviour playerInputController;

        [Header("Input UI")]
        [SerializeField] private GameObject inputPanel;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeButton;

        [Header("Visual Settings")]
        [SerializeField] private float typingSpeed = 0.02f;
        [SerializeField] private bool useTypingEffect = true;
        [SerializeField] private Color userTextColor = Color.blue;
        [SerializeField] private Color agentTextColor = Color.black;

        [Header("Audio Settings")]
        [SerializeField] private bool useTextToSpeech = false;
        [SerializeField] private AudioSource audioSource;


        private Coroutine typingCoroutine;
        private bool isProcessing;
        private string currentAgentName;
        public static string CurrentInteractingAgentName;

        public bool IsDialogueOpen { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        private void Start()
        {
            InitializeUI();
            HideDialogue();
        }

        private void InitializeUI()
        {
            if (inputField != null)
            {
                inputField.onSubmit.AddListener(OnInputSubmit);
                inputField.onSelect.AddListener(_ => DisablePlayerControls());
                inputField.onDeselect.AddListener(_ => EnablePlayerControls());
            }
            else Debug.LogError("InputField not assigned");

            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendButtonClicked);
            else Debug.LogError("SendButton not assigned");

            if (closeButton != null)
                closeButton.onClick.AddListener(HideDialogue);
            else Debug.LogError("CloseButton not assigned");
        }

        private void Update()
        {
            // Press ESC to close dialogue and re-enable controls
            if (IsDialogueOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                HideDialogue();
            }
        }

        public void ShowMessage(string message, string sender)
        {
            if (string.IsNullOrEmpty(message)) return;

            dialoguePanel.SetActive(true);
            bool isUser = sender != currentAgentName;
            dialogueText.color = isUser ? userTextColor : agentTextColor;

            if (useTypingEffect && !isUser)
            {
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeMessage(message));
            }
            else
            {
                dialogueText.text = message;
            }

            if (useTextToSpeech && !isUser && audioSource != null)
            {
                // Implement TTS here
            }
        }

        private IEnumerator TypeMessage(string message)
        {
            dialogueText.text = string.Empty;
            foreach (char c in message)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
            typingCoroutine = null;
        }

        public void ShowInputField(bool show)
        {
            if (inputPanel == null) return;
            inputPanel.SetActive(show);
            if (show)
            {
                inputField.text = string.Empty;
                inputField.ActivateInputField();
            }
        }

        public void SetAgentName(string agentName)
        {
            currentAgentName = agentName;
            if (agentNameText != null) agentNameText.text = agentName;
        }

        public void SetProcessingState(bool processing)
        {
            isProcessing = processing;
            if (sendButton != null) sendButton.interactable = !processing;
            if (inputField != null) inputField.interactable = !processing;
        }

        public void ShowDialogue()
        {
            dialogueCanvas.gameObject.SetActive(true);
            dialoguePanel.SetActive(true);
            ShowInputField(true);
            IsDialogueOpen = true;
            DisablePlayerControls();
        }

        public void HideDialogue()
        {
            dialogueCanvas.gameObject.SetActive(false);
            IsDialogueOpen = false;
            EnablePlayerControls();
        }

        private void OnInputSubmit(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || isProcessing) return;
            SubmitMessage(input);
        }

        private void OnSendButtonClicked()
        {
            if (inputField == null || string.IsNullOrWhiteSpace(inputField.text) || isProcessing) return;
            SubmitMessage(inputField.text);
        }

        private void SubmitMessage(string message)
        {
            ShowMessage(message, "User");
            inputField.text = string.Empty;
            inputField.ActivateInputField();
            OnUserMessageSubmitted?.Invoke(message);
        }

        private void DisablePlayerControls()
        {
            if (playerInputController != null)
                playerInputController.enabled = false;
        }

        private void EnablePlayerControls()
        {
            if (playerInputController != null)
                playerInputController.enabled = true;
        }
    }
}
