using ChatBot.Api.Domain.ValueObjects;

namespace ChatBot.Api.Features.Conversations.Get;

public sealed record GetConversationResponse(
    Guid ConversationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt,
    string Title,
    string? Preview,
    IReadOnlyCollection<MessageResponse> Messages);

public sealed record MessageResponse(
    Guid MessageId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    TokenUsage? TokenUsage);