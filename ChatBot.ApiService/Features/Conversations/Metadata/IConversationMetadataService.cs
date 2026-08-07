using ChatBot.Api.Domain.Entities;

namespace ChatBot.Api.Features.Conversations.Metadata;

public interface IConversationMetadataService
{
    /// <summary>
    /// Synchronous, in-memory only: derives a plain-text preview from
    /// <paramref name="assistantMessage"/> and decides whether this conversation is now
    /// eligible for title generation. Must be called before the conversation's turn is saved,
    /// so both land in the same persisted write.
    /// </summary>
    void ApplyPreview(Conversation conversation, ConversationMessage assistantMessage);

    /// <summary>
    /// Dispatches a detached background title-generation attempt if
    /// <paramref name="conversation"/>'s metadata was just marked eligible by
    /// <see cref="ApplyPreview"/>. Returns immediately — the actual LLM call and follow-up
    /// save happen off to the side, after the turn's own save has already completed.
    /// </summary>
    Task ScheduleTitleGenerationAsync(
        Conversation conversation,
        string model,
        CancellationToken cancellationToken = default);
}
