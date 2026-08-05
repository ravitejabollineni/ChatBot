namespace ChatBot.Api.AI.Configuration;

/// <summary>
/// Settings for the Azure OpenAI provider. Bound from "AI:Providers:AzureOpenAI".
/// </summary>
public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AI:Providers:AzureOpenAI";

    public string Endpoint { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string DeploymentName { get; init; } = string.Empty;
}
