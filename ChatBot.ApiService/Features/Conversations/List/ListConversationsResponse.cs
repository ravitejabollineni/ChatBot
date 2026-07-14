namespace ChatBot.Api.Features.Conversations.List;

public sealed record ListConversationResponse(
    Guid ConversationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt);