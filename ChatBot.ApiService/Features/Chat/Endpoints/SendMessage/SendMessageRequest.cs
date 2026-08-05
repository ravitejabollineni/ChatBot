using System.ComponentModel.DataAnnotations;

namespace ChatBot.Api.Features.Chat.Endpoints.SendMessage;

public sealed record SendMessageRequest(
    Guid ConversationId,

    [property: Required]
    string Model,

    [property: Required]
    string Message);