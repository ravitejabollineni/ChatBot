using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Models;
using ChatBot.Api.Infrastructure.Persistence;

namespace ChatBot.Api.Features.Chat.Services;

public sealed class ChatService(
    IConversationRepository repository,
    IChatProviderFactory providerFactory,
    TimeProvider timeProvider)
    : IChatService
{
    public async Task<ChatResponse> SendAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository.GetByIdAsync(
            request.ConversationId,
            cancellationToken);

        if (conversation is null)
        {
            throw new InvalidOperationException(
                $"Conversation '{request.ConversationId}' was not found.");
        }

        var now = timeProvider.GetUtcNow();

        var userMessage = new ConversationMessage(
            role: ChatRole.User,
            content: request.UserMessage,
            createdAt: now);

        conversation.AddMessage(
            userMessage,
            now);

        var provider = providerFactory.GetProvider(request.Model);

        var assistantResponse = await provider.SendAsync(
            request.Model,
            conversation.Messages,
            cancellationToken);

        var assistantMessage = new ConversationMessage(
            ChatRole.Assistant,
            assistantResponse,
            timeProvider.GetUtcNow());

        conversation.AddMessage(
            assistantMessage,
            assistantMessage.CreatedAt);

        await repository.UpdateAsync(
            conversation,
            cancellationToken);

        return new ChatResponse(
            assistantMessage.Content, assistantMessage.CreatedAt);
    }
}