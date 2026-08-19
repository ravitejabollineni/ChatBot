namespace ChatBot.Api.Common.Errors;

public sealed class ConversationContextTooLargeException(
    int requiredTokenCount,
    int availableTokenCount)
    : Exception(
        $"The system prompt and current message require an estimated {requiredTokenCount} " +
        $"tokens, but only {availableTokenCount} are available in the model's context window.")
{
    public int RequiredTokenCount { get; } = requiredTokenCount;

    public int AvailableTokenCount { get; } = availableTokenCount;
}
