using ChatBot.Api.Domain.Entities;

namespace ChatBot.Api.Features.Chat.Models;

public sealed record ChatContext(
    string Model,
    IReadOnlyCollection<ConversationMessage> History);