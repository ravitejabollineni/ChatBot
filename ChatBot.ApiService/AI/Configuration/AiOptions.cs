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
}
