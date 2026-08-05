namespace ChatBot.Api.Features.Chat.Endpoints.StreamMessage;

public sealed record StreamMessageRequest(
    Guid ConversationId,
    string Model,
    string Message);
