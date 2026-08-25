using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.ValueObjects;
using ChatBot.Api.Features.Chat.Contracts;

namespace ChatBot.Api.AI.TokenManagement;

/// <summary>
/// Estimates token counts from raw character length instead of a model-specific tokenizer.
/// Swap this for a real tokenizer (e.g. Tiktoken) behind <see cref="ITokenManager"/> without
/// touching callers. Provider/model knowledge (context limits) is not this class's concern —
/// it only reads the resolved <see cref="ChatModelMetadata.ContextLimit"/> from
/// <see cref="IChatModelMetadataProvider"/>.
/// </summary>
public sealed class EstimatingTokenManager(
    IChatModelMetadataProvider metadataProvider) : ITokenManager
{
    private const double CharactersPerToken = 4.0;

    public int EstimateTokenCount(IReadOnlyCollection<ConversationMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        return messages.Sum(message => EstimateTokenCount(message.Content));
    }

    public Task<TokenUsage> CalculateAsync(
        IReadOnlyCollection<ConversationMessage> requestMessages,
        string assistantResponse,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestMessages);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var inputTokenCount = EstimateTokenCount(requestMessages);

        var outputTokenCount = EstimateTokenCount(assistantResponse);

        var contextLimit = metadataProvider.GetMetadata(model).ContextLimit;
        var totalUsed = inputTokenCount + outputTokenCount;
        var remaining = Math.Max(0, contextLimit - totalUsed);
        var percentageUsed = contextLimit > 0
            ? Math.Round(totalUsed * 100d / contextLimit, 2)
            : 0d;

        var usage = new TokenUsage(
            inputTokenCount,
            outputTokenCount,
            contextLimit,
            remaining,
            percentageUsed);

        return Task.FromResult(usage);
    }

    private static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return Math.Max(1, (int)Math.Ceiling(text.Length / CharactersPerToken));
    }
}
