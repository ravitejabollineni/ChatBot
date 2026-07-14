using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Conversations.Get;
using ChatBot.Api.Features.Conversations.Create;
using ChatBot.Api.Infrastructure.Persistence;
using ChatBot.Api.Features.Conversations.Contracts;

namespace ChatBot.Api.Features.Conversations;

public sealed class ConversationService(
    IConversationRepository repository,
    TimeProvider timeProvider)
    : IConversationService
{
    public async Task<Guid> CreateAsync(
       CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var conversation = new Conversation(now);

        await repository.AddAsync(
            conversation,
            cancellationToken);

        return conversation.Id;
    }

    public async Task<Conversation?> GetAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetByIdAsync(
            conversationId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Conversation>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await repository.GetAllAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        await repository.DeleteAsync(
            conversationId,
            cancellationToken);
    }
}