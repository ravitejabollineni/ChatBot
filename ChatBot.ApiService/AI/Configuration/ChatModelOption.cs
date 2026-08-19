namespace ChatBot.Api.AI.Configuration;

/// <summary>
/// One entry in <c>AI:AvailableModels</c>: a model name the chat UI can select, and which
/// provider serves it.
/// </summary>
/// <param name="Model">
/// Passed through verbatim to the resolved provider — e.g. an Ollama tag such as
/// <c>"phi3:mini"</c>, or an Azure OpenAI/OpenAI model name.
/// </param>
/// <param name="Provider">
/// One of <see cref="ChatBot.Api.AI.Routing.ChatProviderNames"/>'s keys. Optional: when
/// omitted, <see cref="AiOptions.DefaultProvider"/> serves this model, so a single-provider
/// deployment doesn't need to repeat it on every entry.
/// </param>
/// <param name="ContextLimit">
/// The model's context window in tokens, used by <see cref="ChatBot.Api.Features.Chat.Contracts.ITokenManager"/>.
/// Optional: a locally-hosted model (e.g. Ollama) that already declares its own window via
/// <see cref="OllamaOptions.NumCtx"/> doesn't need this repeated here; it's primarily for
/// providers with no other configured signal for it (Azure OpenAI, Gemini).
/// </param>
public sealed record ChatModelOption(
    string Model,
    string? Provider = null,
    int? ContextLimit = null);
