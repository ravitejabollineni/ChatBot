using ChatBot.Api.Domain.Entities;
using Google.GenAI.Types;
using OpenAI.Chat;
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using AiChatRole = Microsoft.Extensions.AI.ChatRole;
using DomainChatRole = ChatBot.Api.Domain.Enums.ChatRole;
using GeminiContent = Google.GenAI.Types.Content;
using OpenAiChatMessage = OpenAI.Chat.ChatMessage;

namespace ChatBot.Api.AI.Common;

/// <summary>
/// Translates domain <see cref="ConversationMessage"/> history into the message shapes the
/// SDKs behind each <c>IChatProvider</c> expect. Providers built on Microsoft.Extensions.AI
/// (Azure OpenAI, Ollama) share <see cref="ToAiMessages"/>; the OpenAI SDK has its own
/// per-role message hierarchy and uses <see cref="ToOpenAiMessages"/>; the Google GenAI SDK
/// has its own again and uses <see cref="ToGeminiContents"/>.
/// </summary>
public static class ChatMessageMapper
{
    /// <summary>
    /// Maps history to Microsoft.Extensions.AI messages, preserving order.
    /// </summary>
    public static List<AiChatMessage> ToAiMessages(
        IReadOnlyCollection<ConversationMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        List<AiChatMessage> history = new(messages.Count);

        foreach (var message in messages)
        {
            history.Add(
                new AiChatMessage(
                    ToAiRole(message.Role),
                    message.Content));
        }

        return history;
    }

    /// <summary>
    /// Maps history to OpenAI SDK messages, preserving order.
    /// </summary>
    public static List<OpenAiChatMessage> ToOpenAiMessages(
        IReadOnlyCollection<ConversationMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        List<OpenAiChatMessage> chatMessages = new(messages.Count);

        foreach (var message in messages)
        {
            chatMessages.Add(
                message.Role switch
                {
                    DomainChatRole.System
                        => new SystemChatMessage(message.Content),

                    DomainChatRole.User
                        => new UserChatMessage(message.Content),

                    DomainChatRole.Assistant
                        => new AssistantChatMessage(message.Content),

                    _ => throw new NotSupportedException(
                        $"Unsupported chat role '{message.Role}'.")
                });
        }

        return chatMessages;
    }

    public static (string? SystemInstruction, List<GeminiContent> Contents) ToGeminiContents(
        IReadOnlyCollection<ConversationMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        string? systemInstruction = null;
        List<GeminiContent> contents = new(messages.Count);

        foreach (var message in messages)
        {
            if (message.Role == DomainChatRole.System)
            {
                systemInstruction = message.Content;
                continue;
            }

            contents.Add(new GeminiContent
            {
                Role = message.Role == DomainChatRole.Assistant ? "model" : "user",
                Parts = [Part.FromText(message.Content)]
            });
        }

        return (systemInstruction, contents);
    }

    private static AiChatRole ToAiRole(DomainChatRole role)
        => role switch
        {
            DomainChatRole.System => AiChatRole.System,
            DomainChatRole.User => AiChatRole.User,
            DomainChatRole.Assistant => AiChatRole.Assistant,
            _ => throw new NotSupportedException(
                $"Unsupported chat role '{role}'.")
        };
}
