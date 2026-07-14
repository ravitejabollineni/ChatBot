namespace ChatBot.Api.Infrastructure.Configuration;

public sealed class OpenAiOptions
{
    public const string SectionName = "ChatProviders:OpenAI";

    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Models { get; init; } = [];
}