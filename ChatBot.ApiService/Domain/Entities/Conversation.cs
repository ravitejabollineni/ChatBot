using ChatBot.Api.Domain.ValueObjects;

namespace ChatBot.Api.Domain.Entities;

public sealed class Conversation
{
    private readonly List<ConversationMessage> _messages = [];

    public Guid Id { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset LastUpdatedAt { get; private set; }

    public ConversationMetadata Metadata { get; private set; } = ConversationMetadata.CreateDefault();

    public IReadOnlyList<ConversationMessage> Messages => _messages.AsReadOnly();

    public Conversation(DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        CreatedAt = createdAt;
        LastUpdatedAt = createdAt;
    }

    public void AddMessage(
        ConversationMessage message,
        DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(message);

        _messages.Add(message);

        LastUpdatedAt = updatedAt;
    }

    public void Clear(DateTimeOffset updatedAt)
    {
        _messages.Clear();

        LastUpdatedAt = updatedAt;
    }

    public void UpdateMetadata(ConversationMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        Metadata = metadata;
    }
}