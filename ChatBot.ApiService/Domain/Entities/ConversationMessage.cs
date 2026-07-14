using ChatBot.Api.Domain.Enums;

namespace ChatBot.Api.Domain.Entities;

public sealed class ConversationMessage
{
    public Guid Id { get; }

    public ChatRole Role { get; }

    public string Content { get; }

    public DateTimeOffset CreatedAt { get; }

    public ConversationMessage(
        ChatRole role,
        string content,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id = Guid.CreateVersion7();
        Role = role;
        Content = content;
        CreatedAt = createdAt;
    }
}