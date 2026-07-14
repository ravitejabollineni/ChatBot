namespace ChatBot.Api.Features.Chat.SendMessage;

public sealed record SendMessageResponse(
    Guid ConversationId,
    string Model,
    string Response,
    DateTimeOffset CreatedAt);