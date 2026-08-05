namespace ChatBot.Api.AI.Routing;

public static class ChatProviderNames
{
    public const string Ollama = "Ollama";

    public const string AzureOpenAI = "AzureOpenAI";

    public const string OpenAI = "OpenAI";

    public static readonly string[] All = [Ollama, AzureOpenAI, OpenAI];
    public static string? Normalize(string? name)
        => All.FirstOrDefault(provider =>
            string.Equals(provider, name, StringComparison.OrdinalIgnoreCase));
}
