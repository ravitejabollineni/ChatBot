namespace ChatBot.Web.Features.Chat.Contracts.Conversation;

public sealed record MessageResponse(
    Guid MessageId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt);