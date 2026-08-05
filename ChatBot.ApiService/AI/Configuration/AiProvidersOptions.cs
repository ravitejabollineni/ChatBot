namespace ChatBot.Api.AI.Configuration;

/// <summary>
/// Container for every provider's strongly typed settings, bound from "AI:Providers".
/// Reuses the same option types that individual providers/services consume directly
/// via <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>, so this exists
/// purely as the aggregate/validation view of the "AI" section — it introduces no new
/// provider abstraction.
/// </summary>
public sealed class AiProvidersOptions
{
    public OllamaOptions Ollama { get; init; } = new();

    public AzureOpenAiOptions AzureOpenAI { get; init; } = new();

    public OpenAiOptions OpenAI { get; init; } = new();
}
