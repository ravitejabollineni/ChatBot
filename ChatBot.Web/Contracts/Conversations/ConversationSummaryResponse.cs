namespace ChatBot.Web.Features.Chat.Contracts.Conversation;

public sealed record ConversationSummaryResponse(
    Guid ConversationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt,
    string Title,
    string? Preview);