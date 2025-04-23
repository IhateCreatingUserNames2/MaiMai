using LLMUnity;
using MaiMai.Implementation;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ExtendedDialogueUIManager : DialogueUIManager
{
    [Header("History UI")]
    [SerializeField] private RectTransform messagesContainer;
    [SerializeField] private GameObject userBubblePrefab;
    [SerializeField] private GameObject agentBubblePrefab;
    [SerializeField] private LLMCharacter llmCharacter;


    /// <summary>
    /// Recria toda a lista de bolhas a partir do histórico.
    /// </summary>
    public void RefreshHistory(List<ChatMessage> history)
    {
        // Limpa bolhas antigas
        foreach (Transform child in messagesContainer)
            Destroy(child.gameObject);

        // Cria uma bolha para cada mensagem (pula system)
        foreach (var msg in history)
        {
            if (msg.role == "system") continue;

            var prefab = msg.role == llmCharacter.AIName
                ? agentBubblePrefab
                : userBubblePrefab;

            var go = Instantiate(prefab, messagesContainer);
            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            text.text = msg.content;
        }
    }

    /// <summary>
    /// Sobrescreve ShowMessage para também instanciar uma bolha.
    /// </summary>
    public new void ShowMessage(string message, string sender)
    {
        base.ShowMessage(message, sender);

        var prefab = sender == llmCharacter.AIName
            ? agentBubblePrefab
            : userBubblePrefab;

        var go = Instantiate(prefab, messagesContainer);
        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        text.text = message;
    }
}