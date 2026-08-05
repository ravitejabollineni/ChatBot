namespace ChatBot.Api.Features.Chat.Models
{
    public sealed record ChatStreamChunk(
    string Text,
    bool IsCompleted)
    {
        public static ChatStreamChunk TextMessage(string text) =>
            new(text, false);

        public static ChatStreamChunk Completed() =>
            new(string.Empty, true);
    }
}
