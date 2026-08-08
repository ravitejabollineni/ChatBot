using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Conversations.Contracts;
using ChatBot.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Api.Features.Conversations;

public sealed class ConversationService(
    ChatBotDbContext dbContext,
    TimeProvider timeProvider)
    : IConversationService
{
    public async Task<Guid> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var conversation = new Conversation(now);

        await dbContext.Conversations.AddAsync(
            conversation,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return conversation.Id;
    }

    public async Task<Conversation?> GetAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .Include(x => x.Messages)
            .SingleOrDefaultAsync(
                x => x.Id == conversationId,
                cancellationToken);
    }

    public async Task<Conversation> GetRequiredAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetAsync(
            conversationId,
            cancellationToken);

        if (conversation is null)
        {
            throw new InvalidOperationException(
                $"Conversation '{conversationId}' was not found.");
        }

        return conversation;
    }

    public async Task<IReadOnlyCollection<Conversation>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .AsNoTracking()
            .OrderByDescending(x => x.LastUpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        dbContext.Conversations.Update(conversation);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(
                x => x.Id == conversationId,
                cancellationToken);

        if (conversation is null)
        {
            return;
        }

        dbContext.Conversations.Remove(conversation);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}