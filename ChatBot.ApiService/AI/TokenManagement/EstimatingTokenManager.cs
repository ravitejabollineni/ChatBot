using ChatBot.Api.AI.Configuration;
using ChatBot.Api.AI.Routing;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.ValueObjects;
using ChatBot.Api.Features.Chat.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatBot.Api.AI.TokenManagement;

/// <summary>
/// Estimates token counts from raw character length instead of a model-specific tokenizer.
/// Swap this for a real tokenizer (e.g. Tiktoken) behind <see cref="ITokenManager"/> without
/// touching callers. Context limits, by contrast, are not this class's knowledge to hardcode:
/// they come from <see cref="AiOptions"/> (an explicit <see cref="ChatModelOption.ContextLimit"/>,
/// or — for Ollama — the provider's own configured <see cref="OllamaOptions.NumCtx"/>), keyed
/// off the same model/provider resolution <see cref="ChatProviderFactory"/> uses for routing.
/// </summary>
public sealed class EstimatingTokenManager(
    IOptions<AiOptions> aiOptions,
    ILogger<EstimatingTokenManager> logger) : ITokenManager
{
    private const double CharactersPerToken = 4.0;

    /// <summary>
    /// Last-resort fallback when a model has neither an explicit
    /// <see cref="ChatModelOption.ContextLimit"/> nor (for Ollama) a configured
    /// <see cref="OllamaOptions.NumCtx"/>. Reaching this is a configuration gap, not a
    /// supported steady state, and is always logged.
    /// </summary>
    private const int DefaultContextLimit = 128_000;

    public int GetContextLimit(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var options = aiOptions.Value;
        var explicitLimit = options.FindModel(model)?.ContextLimit;

        if (explicitLimit is > 0)
        {
            return explicitLimit.Value;
        }

        var providerName = options.ResolveProviderName(model);

        if (string.Equals(providerName, ChatProviderNames.Ollama, StringComparison.Ordinal)
            && options.Providers.Ollama.NumCtx is > 0)
        {
            return options.Providers.Ollama.NumCtx.Value;
        }

        logger.LogWarning(
            "No context limit configured for model '{Model}' (provider '{Provider}'); "
            + "falling back to the default of {DefaultContextLimit} tokens.",
            model,
            providerName ?? "unknown",
            DefaultContextLimit);

        return DefaultContextLimit;
    }

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
