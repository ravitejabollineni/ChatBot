namespace ChatBot.Api.AI.Configuration;

/// <summary>
/// Settings for the OpenAI provider. Bound from "AI:Providers:OpenAI".
/// </summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "AI:Providers:OpenAI";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;
}
