namespace ChatBot.Api.Features.Chat.Models
{
    public sealed record ChatStreamChunk(
    string Text,
    bool IsCompleted,
    bool IsError = false,
    string? ErrorMessage = null)
    {
        public static ChatStreamChunk TextMessage(string text) =>
            new(text, false);

        public static ChatStreamChunk Completed() =>
            new(string.Empty, true);

        public static ChatStreamChunk Error(string errorMessage) =>
            new(string.Empty, true, true, errorMessage);
    }
}
