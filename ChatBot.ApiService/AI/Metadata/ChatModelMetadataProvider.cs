using ChatBot.Api.AI.Configuration;
using ChatBot.Api.AI.Routing;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Domain.ValueObjects;
using ChatBot.Api.Features.Chat.Contracts;
using Microsoft.Extensions.Options;

namespace ChatBot.Api.AI.Metadata;

/// <summary>
/// Resolves <see cref="ChatModelMetadata"/> from <see cref="AiOptions"/>. This is the only
/// place context-limit tiering and capability defaulting live — <see cref="ITokenManager"/>
/// and <see cref="IChatContextManager"/> only ever read the result, never provider/model
/// names.
/// </summary>
public sealed class ChatModelMetadataProvider(
    IOptions<AiOptions> aiOptions,
    ILogger<ChatModelMetadataProvider> logger) : IChatModelMetadataProvider
{
    /// <summary>
    /// Last-resort fallback when a model has neither an explicit
    /// <see cref="ChatModelOption.ContextLimit"/> nor (for Ollama) a configured
    /// <see cref="OllamaOptions.NumCtx"/>. Reaching this is a configuration gap, not a
    /// supported steady state, and is always logged.
    /// </summary>
    private const int DefaultContextLimit = 128_000;

    /// <summary>
    /// Every registered <see cref="IChatProvider"/> implements both <c>SendAsync</c> and
    /// <c>StreamAsync</c>, so this baseline is a structural fact rather than a per-model
    /// guess. Anything beyond it (tool calling, structured output, embeddings) must be
    /// verified and set explicitly via <see cref="ChatModelOption.Capabilities"/>.
    /// </summary>
    private const ChatModelCapabilities BaselineCapabilities =
        ChatModelCapabilities.Chat | ChatModelCapabilities.Streaming;

    public ChatModelMetadata GetMetadata(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var options = aiOptions.Value;
        var modelOption = options.FindModel(model);

        var providerName = options.ResolveProviderName(model)
            ?? throw new InvalidOperationException(
                $"No known provider is configured for model '{model}'. "
                + $"Expected one of: {string.Join(", ", ChatProviderNames.All)}.");

        var contextLimit = ResolveContextLimit(model, modelOption, providerName, options);
        var capabilities = modelOption?.Capabilities ?? BaselineCapabilities;

        return new ChatModelMetadata(providerName, model, contextLimit, capabilities);
    }

    private int ResolveContextLimit(
        string model, ChatModelOption? modelOption, string providerName, AiOptions options)
    {
        if (modelOption?.ContextLimit is > 0)
        {
            return modelOption.ContextLimit.Value;
        }

        if (string.Equals(providerName, ChatProviderNames.Ollama, StringComparison.Ordinal)
            && options.Providers.Ollama.NumCtx is > 0)
        {
            return options.Providers.Ollama.NumCtx.Value;
        }

        logger.LogWarning(
            "No context limit configured for model '{Model}' (provider '{Provider}'); "
            + "falling back to the default of {DefaultContextLimit} tokens.",
            model,
            providerName,
            DefaultContextLimit);

        return DefaultContextLimit;
    }
}
