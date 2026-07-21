using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Features.Conversations.Get;
using ChatBot.Api.Features.Conversations.Create;
using ChatBot.Api.Infrastructure.Configuration;
using ChatBot.Api.Infrastructure.Persistence;
using ChatBot.Api.Features.Conversations.Contracts;
using Microsoft.Extensions.Options;

namespace ChatBot.Api.Features.Conversations;

public sealed class ConversationService(
    IConversationRepository repository,
    TimeProvider timeProvider,
    IOptions<SystemPromptOptions> chatOptions)
    : IConversationService
{
    public async Task<Guid> CreateAsync(
       CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var conversation = new Conversation(now);

        var systemPrompt = chatOptions.Value.SystemPrompt;

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            conversation.AddMessage(
                new ConversationMessage(ChatRole.System, systemPrompt, now),
                now);
        }

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

    public Task SaveAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        return repository.UpdateAsync(
            conversation,
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