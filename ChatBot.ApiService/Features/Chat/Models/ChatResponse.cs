using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.ValueObjects;

namespace ChatBot.Api.Features.Chat.Models;

public sealed record ChatResponse(
    string Response,
    DateTimeOffset CreatedAt,
    TokenUsage TokenUsage);