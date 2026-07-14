namespace ChatBot.Api.Infrastructure.Configuration;

public sealed class AzureOpenAiOptions
{
    public const string SectionName = "ChatProviders:AzureOpenAI";

    public string Endpoint { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string DeploymentName { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Models { get; init; } = [];
}