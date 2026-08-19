using ChatBot.Api.AI.Routing;

namespace ChatBot.Api.AI.Configuration;

/// <summary>
/// Root configuration for the AI module. Bound from the "AI" configuration section.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "AI";
    public string DefaultProvider { get; init; } = string.Empty;

    public IReadOnlyList<ChatModelOption> AvailableModels { get; init; } = [];

    public AiProvidersOptions Providers { get; init; } = new();

    public ContextManagementOptions ContextManagement { get; init; } = new();

    /// <summary>
    /// Finds the <see cref="ChatModelOption"/> registered for <paramref name="model"/> in
    /// <see cref="AvailableModels"/> (case-insensitive), if any.
    /// </summary>
    public ChatModelOption? FindModel(string model)
        => AvailableModels.FirstOrDefault(
            m => string.Equals(m.Model, model, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Resolves the provider name that serves <paramref name="model"/>: the matching
    /// <see cref="AvailableModels"/> entry's own <see cref="ChatModelOption.Provider"/> when
    /// set, otherwise <see cref="DefaultProvider"/> — both for models with no explicit
    /// <c>Provider</c> and for a model string that isn't listed at all. Shared by
    /// <see cref="ChatBot.Api.AI.Routing.ChatProviderFactory"/> (to pick the provider) and
    /// <see cref="ChatBot.Api.AI.TokenManagement.EstimatingTokenManager"/> (to pick that
    /// provider's context-limit configuration), so the two never disagree about which
    /// provider a model resolves to.
    /// </summary>
    public string? ResolveProviderName(string model)
    {
        var configuredName = FindModel(model)?.Provider is { Length: > 0 } explicitProvider
            ? explicitProvider
            : DefaultProvider;

        return ChatProviderNames.Normalize(configuredName);
    }
}
