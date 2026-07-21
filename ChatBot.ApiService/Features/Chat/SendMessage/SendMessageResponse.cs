using ChatBot.Api.Domain.ValueObjects;

namespace ChatBot.Api.Features.Chat.SendMessage;

public sealed record SendMessageResponse(
    Guid ConversationId,
    string Model,
    string Response,
    DateTimeOffset CreatedAt,
    TokenUsage TokenUsage);