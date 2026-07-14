namespace ChatBot.Web.Features.Chat.Contracts.Conversation;

public sealed record GetConversationResponse(
    Guid ConversationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt,
    IReadOnlyCollection<MessageResponse> Messages);