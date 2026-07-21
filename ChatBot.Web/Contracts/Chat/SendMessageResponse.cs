namespace ChatBot.Web.Contracts.Chat;

public sealed record SendMessageResponse(
    Guid ConversationId,
    string Model,
    string Response,
    DateTimeOffset CreatedAt,
    TokenUsageResponse TokenUsage);
