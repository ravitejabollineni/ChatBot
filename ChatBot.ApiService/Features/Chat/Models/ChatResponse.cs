using ChatBot.Api.Domain.Entities;

namespace ChatBot.Api.Features.Chat.Models;

public sealed record ChatResponse(
    string Response, DateTimeOffset CreatedAt);