namespace ChatBot.Api.AI.Prompts.Internal;

internal sealed class PromptFrontMatter
{
    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = "1.0.0";

    public string Description { get; init; } = string.Empty;

    public string? Author { get; init; }

    public DateOnly? Created { get; init; }

    public List<string> Tags { get; init; } = [];

    public double? Temperature { get; init; }

    public int? MaxTokens { get; init; }
}