namespace ChatBot.Api.Features.Chat.Models;

public sealed record ChatRequest(
    Guid ConversationId,
    string Model,
    string UserMessage);