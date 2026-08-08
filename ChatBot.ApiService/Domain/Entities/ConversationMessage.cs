using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Domain.ValueObjects;

namespace ChatBot.Api.Domain.Entities;

public sealed class ConversationMessage
{
    public Guid Id { get; }

    public ChatRole Role { get; }

    public string Content { get; }

    public DateTimeOffset CreatedAt { get; }

    public Guid ConversationId { get; }

    public TokenUsage? TokenUsage { get; }

    private ConversationMessage()
    {
        
    }

    public ConversationMessage(
    Guid conversationId,
    ChatRole role,
    string content,
    DateTimeOffset createdAt,
    TokenUsage? tokenUsage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id = Guid.CreateVersion7();
        ConversationId = conversationId;
        Role = role;
        Content = content;
        CreatedAt = createdAt;
        TokenUsage = tokenUsage;
    }
}