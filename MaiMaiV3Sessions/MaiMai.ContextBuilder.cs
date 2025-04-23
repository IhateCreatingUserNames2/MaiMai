using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MaiMai.Core;

namespace MaiMai.Implementation
{
    /// <summary>
    /// Builds formatted context for LLM interactions based on various inputs
    /// </summary>
    public class DefaultContextBuilder : IContextBuilder
    {
        // Maximum length controls
        private readonly int _maxTotalLength;
        private readonly int _maxConversationLength;
        private readonly int _maxContextLength;
        
        /// <summary>
        /// Creates a new context builder with specified constraints
        /// </summary>
        public DefaultContextBuilder(
            int maxTotalLength = 4096,
            int maxConversationLength = 2048,
            int maxContextLength = 1024)
        {
            _maxTotalLength = maxTotalLength;
            _maxConversationLength = maxConversationLength;
            _maxContextLength = maxContextLength;
        }

        /// <summary>
        /// Builds a complete context string for LLM interaction
        /// </summary>
        public Task<string> BuildContextAsync(
            string agentName,
            string systemPrompt,
            string userId,
            string userInput,
            List<MessageEntry> recentConversation,
            string retrievedContext,
            string fixedContext)
        {
            StringBuilder context = new StringBuilder();
            
            // Add agent identity and system instructions
            AppendWithHeader(context, "System", GetSystemPrompt(agentName, systemPrompt));
            
            // Add retrieved context if available
            if (!string.IsNullOrEmpty(retrievedContext))
            {
                string trimmedContext = TrimToMaxLength(retrievedContext, _maxContextLength);
                AppendWithHeader(context, "Memory", trimmedContext);
            }
            
            // Add fixed context if available
            if (!string.IsNullOrEmpty(fixedContext))
            {
                string trimmedFixedContext = TrimToMaxLength(fixedContext, _maxContextLength);
                AppendWithHeader(context, "Background", trimmedFixedContext);
            }
            
            // Add conversation history
            if (recentConversation != null && recentConversation.Count > 0)
            {
                string conversationHistory = FormatConversation(recentConversation);
                string trimmedConversation = TrimToMaxLength(conversationHistory, _maxConversationLength);
                AppendWithHeader(context, "Conversation History", trimmedConversation);
            }

            // Add current user input
            context.AppendLine($"User message: {userInput}");
            context.AppendLine();
            context.AppendLine("Your response:");

            // Ensure total context is within limits
            string result = TrimToMaxLength(context.ToString(), _maxTotalLength);
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Formats the system prompt with agent name
        /// </summary>
        private string GetSystemPrompt(string agentName, string systemPrompt)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(systemPrompt);
            sb.AppendLine();
            sb.AppendLine("Guidelines for your responses:");
            sb.AppendLine("1. Respond directly to the user.");
            sb.AppendLine("2. Do not simulate both sides of the conversation.");
            sb.AppendLine("3. Do not prefix your response with 'From [name]:' or similar tags.");
            sb.AppendLine("4. Simply provide your answer to the user's most recent message.");

            return sb.ToString();
        }

        /// <summary>
        /// Formats a conversation into a readable string
        /// </summary>
        private string FormatConversation(List<MessageEntry> conversation)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var message in conversation)
            {
                // More explicit labeling to help the model understand
                if (message.Sender == "User")
                    sb.AppendLine($"User message: {message.Message}");
                else
                    sb.AppendLine($"Assistant response: {message.Message}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Appends a section with a header to the context
        /// </summary>
        private void AppendWithHeader(StringBuilder sb, string header, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            
            sb.AppendLine($"### {header}:");
            sb.AppendLine(content);
            sb.AppendLine();
        }
        
        /// <summary>
        /// Trims text to ensure it doesn't exceed maximum length
        /// </summary>
        private string TrimToMaxLength(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }
            
            // Simple trim from the beginning to preserve the most recent content
            return "..." + text.Substring(text.Length - maxLength + 3);
        }
    }
}