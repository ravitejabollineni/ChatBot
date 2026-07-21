using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.ValueObjects;
using ChatBot.Api.Features.Chat.Contracts;

namespace ChatBot.Api.Infrastructure.TokenManagement;

/// <summary>
/// Estimates token counts from raw character length instead of a model-specific
/// tokenizer. Swap this for a real tokenizer (e.g. Tiktoken) behind <see cref="ITokenManager"/>
/// without touching callers.
/// </summary>
public sealed class EstimatingTokenManager : ITokenManager
{
    private const double CharactersPerToken = 4.0;
    private const int DefaultContextLimit = 128_000;

    private static readonly IReadOnlyDictionary<string, int> ContextLimitsByModelHint =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["gpt-5.4"] = 400_000,
            ["gpt-5"] = 272_000,
            ["gpt-4.1"] = 1_047_576,
            ["gpt-4o"] = 128_000,
            ["gpt-4"] = 8_192,
            ["gpt-3.5"] = 16_385,
            ["o3"] = 200_000,
            ["o1"] = 200_000,
        };

    public int GetContextLimit(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var match = ContextLimitsByModelHint
            .Where(hint => model.Contains(hint.Key, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(hint => hint.Key.Length)
            .Select(hint => (int?)hint.Value)
            .FirstOrDefault();

        return match ?? DefaultContextLimit;
    }

    public Task<TokenUsage> CalculateAsync(
        IReadOnlyCollection<ConversationMessage> requestMessages,
        string assistantResponse,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestMessages);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var inputTokenCount = requestMessages.Sum(
            message => EstimateTokenCount(message.Content));

        var outputTokenCount = EstimateTokenCount(assistantResponse);

        var contextLimit = GetContextLimit(model);
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
