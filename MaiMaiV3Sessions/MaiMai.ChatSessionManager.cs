using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaiMai.Core;
using LLMUnity;

namespace MaiMai.Implementation
{

    /// <summary>
    /// Gerencia múltiplas sessões de chat, cada uma com seu próprio histórico.
    /// </summary>
    public class ChatSessionManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public LLMCharacter llmCharacter;
        public ExtendedDialogueUIManager uiManager;
        public string CurrentSessionId => currentSession?.SessionId;

        private List<ChatSession> sessions = new List<ChatSession>();
        private ChatSession currentSession;
        private List<(string role, string text, DateTime time)> history =
            new List<(string role, string text, DateTime time)>();

        private void Start()
        {
            CreateNewSession();
        }

        /// <summary>
        /// Adiciona uma mensagem (user ou agent) ao histórico da sessão atual.
        /// </summary>
        public void AddMessageToCurrentSession(string role, string content)
        {
            // Only add to LLMCharacter (the single source of truth)
            if (role == "User" || role == llmCharacter.playerName)
                llmCharacter.AddPlayerMessage(content);
            else
                llmCharacter.AddAIMessage(content);

            // Save after each message to persist
            if (currentSession != null)
                _ = llmCharacter.Save($"chat_{currentSession.SessionId}");
        }

        /// <summary>
        /// Retorna o histórico completo da sessão atual.
        /// </summary>
        public List<(string role, string text, DateTime time)> GetHistory() => history;

        /// <summary>
        /// Cria uma nova sessão, limpa o histórico e alterna para ela.
        /// </summary>
        public void CreateNewSession()
        {
            history.Clear();
            var session = new ChatSession
            {
                SessionId = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.Now
            };
            sessions.Add(session);
            SwitchToSession(session.SessionId);
        }

        /// <summary>
        /// Alterna para uma sessão existente, recarregando seu histórico via LLMCharacter.
        /// </summary>
        public async void SwitchToSession(string sessionId)
        {
            currentSession = sessions.Find(s => s.SessionId == sessionId);
            if (currentSession == null)
            {
                Debug.LogError($"Sessão '{sessionId}' não encontrada");
                return;
            }

            // Usa uma chave única por sessão para persistência
            string saveKey = $"chat_{sessionId}";
            llmCharacter.save = saveKey;

            // Limpa e recarrega (se existir) via LLMCharacter
            llmCharacter.ClearChat();
            await llmCharacter.Load(saveKey);

            // Atualiza a UI com o histórico carregado pelo LLMCharacter
            uiManager.RefreshHistory(llmCharacter.chat);
        }

        /// <summary>
        /// Retorna todas as sessões já criadas.
        /// </summary>
        public List<ChatSession> GetSessions() => sessions;
    }

    /// <summary>
    /// Extensão de DialogueUIManager que exibe todo o histórico em bolhas.
   
}
