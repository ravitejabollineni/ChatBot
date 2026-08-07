using ChatBot.Api.Domain.Entities;

namespace ChatBot.Api.Features.Conversations.Metadata;

public interface IConversationTitleGenerator
{
    /// <summary>
    /// Calls the LLM to produce a short title summarizing <paramref name="conversation"/>'s
    /// first exchange. Returns <c>null</c> if the conversation has no completed exchange yet,
    /// or if the model's response sanitizes down to nothing usable.
    /// </summary>
    Task<string?> TryGenerateTitleAsync(
        Conversation conversation,
        string model,
        CancellationToken cancellationToken = default);
}
