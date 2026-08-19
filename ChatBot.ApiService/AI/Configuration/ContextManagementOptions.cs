namespace ChatBot.Api.AI.Configuration;

/// <summary>
/// Settings controlling how much of a model's context window is reserved for the response
/// before <see cref="ChatBot.Api.Features.Chat.Contracts.IChatContextManager"/> selects how
/// much history fits in what's left.
/// </summary>
public sealed class ContextManagementOptions
{
    /// <summary>
    /// Estimated output tokens reserved out of the model's context limit. The remaining
    /// budget (<c>contextLimit - OutputReserveTokens</c>) is what's available for the system
    /// prompt, the current user message, and any retained history.
    /// </summary>
    public int OutputReserveTokens { get; init; } = 2048;
}
