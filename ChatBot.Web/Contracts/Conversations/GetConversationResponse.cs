namespace ChatBot.Web.Features.Chat.Contracts.Conversation;

public sealed record GetConversationResponse(
    Guid ConversationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt,
    string Title,
    string? Preview,
    IReadOnlyCollection<MessageResponse> Messages);