using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Infrastructure.Persistence;
using System.Collections.Concurrent;

namespace ChatBot.Api.Infrastructure.Persistence;

public sealed class InMemoryConversationRepository
    : IConversationRepository
{
    private readonly ConcurrentDictionary<Guid, Conversation> _conversations = [];

    public Task AddAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        if (!_conversations.TryAdd(conversation.Id, conversation))
        {
            throw new InvalidOperationException(
                $"Conversation '{conversation.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<Conversation?> GetByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        _conversations.TryGetValue(
            conversationId,
            out var conversation);

        return Task.FromResult(conversation);
    }

    public Task<IReadOnlyCollection<Conversation>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Conversation> conversations =
            _conversations.Values.ToArray();

        return Task.FromResult(conversations);
    }

    public Task UpdateAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        _conversations[conversation.Id] = conversation;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        _conversations.TryRemove(
            conversationId,
            out _);

        return Task.CompletedTask;
    }
}