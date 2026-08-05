namespace ChatBot.Api.Features.Chat.Contracts;

public interface IChatProviderFactory
{
    /// <summary>
    /// Returns the <see cref="IChatProvider"/> that should handle a chat request for
    /// <paramref name="model"/>. The active implementation (<c>ChatProviderFactory</c>)
    /// looks <paramref name="model"/> up in "AI:AvailableModels" to find which provider
    /// serves it, falling back to "AI:DefaultProvider" — see that type's remarks.
    /// <paramref name="model"/> is also passed through, unchanged, to whichever provider
    /// is selected.
    /// </summary>
    IChatProvider GetProvider(string model);
}