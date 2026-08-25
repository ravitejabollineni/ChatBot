using ChatBot.Api.Domain.ValueObjects;

namespace ChatBot.Api.Features.Chat.Contracts;

/// <summary>
/// Single source of truth for a model's effective context limit and capabilities. Resolves
/// the provider the same way <see cref="IChatProviderFactory"/> does (via
/// <see cref="ChatBot.Api.AI.Configuration.AiOptions.ResolveProviderName"/>), so callers never
/// need their own provider/model name checks.
/// </summary>
public interface IChatModelMetadataProvider
{
    /// <exception cref="InvalidOperationException">
    /// No provider is configured for <paramref name="model"/>.
    /// </exception>
    ChatModelMetadata GetMetadata(string model);
}
