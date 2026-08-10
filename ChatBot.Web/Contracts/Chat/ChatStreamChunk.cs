namespace ChatBot.Web.Contracts.Chat;

public sealed record ChatStreamChunk(
    string Text,
    bool IsCompleted,
    bool IsError = false,
    string? ErrorMessage = null);
