using ChatBot.Api.Domain.Entities;

namespace ChatBot.Api.Features.Chat.Contracts;

/// <summary>
/// Selects the largest valid prefix of a conversation's messages that fits a model's
/// input-token budget, dropping whole oldest history turns first. Always preserves the
/// system prompt (if present) and the current user message.
/// </summary>
public interface IChatContextManager
{
    /// <param name="messages">
    /// The full chronological prompt: an optional leading System message, then zero or more
    /// older history messages, then the current turn's User message last.
    /// </param>
    /// <param name="model">Model name, used to look up the context limit.</param>
    /// <exception cref="ChatBot.Api.Common.Errors.ConversationContextTooLargeException">
    /// The system prompt and current user message alone exceed the model's input budget.
    /// </exception>
    IReadOnlyList<ConversationMessage> SelectContext(
        IReadOnlyCollection<ConversationMessage> messages,
        string model);
}
