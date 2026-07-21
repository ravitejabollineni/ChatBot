using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Models;
using ChatBot.Api.Features.Conversations.Contracts;

namespace ChatBot.Api.Features.Chat.Services;

public sealed class ChatService(
    IConversationService conversationService,
    IChatProviderFactory providerFactory,
    ITokenManager tokenManager,
    TimeProvider timeProvider)
    : IChatService
{
    public async Task<ChatResponse> SendAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversation = await conversationService.GetRequiredAsync(
            request.ConversationId,
            cancellationToken);

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

        var tokenUsage = await tokenManager.CalculateAsync(
            conversation.Messages,
            assistantResponse,
            request.Model,
            cancellationToken);

        var assistantMessage = new ConversationMessage(
            ChatRole.Assistant,
            assistantResponse,
            timeProvider.GetUtcNow(),
            tokenUsage);

        conversation.AddMessage(
            assistantMessage,
            assistantMessage.CreatedAt);

        await conversationService.SaveAsync(
            conversation,
            cancellationToken);

        return new ChatResponse(
            assistantMessage.Content,
            assistantMessage.CreatedAt,
            tokenUsage);
    }
}