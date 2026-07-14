namespace ChatBot.Web.Contracts.Chat;

public sealed record SendMessageRequest(
    Guid ConversationId,
    string Model,
    string Message);