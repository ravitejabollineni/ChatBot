using ChatBot.Api.AI.Configuration;
using ChatBot.Api.Features.Chat.Contracts;
using Microsoft.Extensions.Options;

namespace ChatBot.Api.AI.Routing;

/// <summary>
/// Resolves which registered <see cref="IChatProvider"/> handles a chat request, keyed on
/// the requested <c>model</c>. A model is matched (case-insensitively) against
/// <c>AI:AvailableModels</c>; that entry's own <see cref="ChatModelOption.Provider"/> wins
/// when set, and <c>AI:DefaultProvider</c> is the fallback — both for models with no
/// explicit <c>Provider</c> and for a model string that isn't listed at all. Every known
/// provider is unconditionally registered in DI (see
/// <c>ServiceCollectionExtensions.AddInfrastructure</c>); this factory alone decides which
/// one actually serves a given request. Switching providers, or which model routes to
/// which, is a configuration change, not a code change.
/// </summary>
/// <remarks>
/// <para>
/// Providers are resolved from <em>keyed</em> registrations one at a time rather than
/// injected together as an <see cref="IEnumerable{T}"/>. That is deliberate: enumerating
/// the set forces the container to construct every provider, including ones the current
/// configuration never uses. Azure's provider needs an <c>IChatClient</c> built from
/// <c>AI:Providers:AzureOpenAI:Endpoint</c>, so with an unconfigured Azure section that
/// eager construction took down the whole app at startup — even when nothing actually
/// routed to Azure. Keyed lookup means a provider no <c>AvailableModels</c> entry (and
/// <c>DefaultProvider</c>) names is never built and its missing configuration never
/// matters — see the matching <c>IsSelected</c> check in
/// <c>ServiceCollectionExtensions.AddInfrastructure</c>, which requires config for exactly
/// the providers this method could resolve to.
/// </para>
/// <para>
/// <see cref="IChatProvider.CanHandle"/> is intentionally not consulted here. It used to
/// be the de facto (and broken) selection mechanism: Azure's <c>CanHandle</c>
/// unconditionally returns <see langword="true"/>, so it always won regardless of the
/// requested model or configuration, while OpenAI's and Ollama's exact-match
/// <c>CanHandle</c> could never be reached. <c>CanHandle</c> stays on the interface —
/// nothing is removed — but routing here goes through configuration instead.
/// </para>
/// </remarks>
public sealed class ChatProviderFactory(
    IServiceProvider serviceProvider,
    IOptions<AiOptions> aiOptions)
    : IChatProviderFactory
{
    public IChatProvider GetProvider(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var options = aiOptions.Value;
        var providerKey = options.ResolveProviderName(model)
            ?? throw new InvalidOperationException(
                $"No known provider is configured for model '{model}'. "
                + $"Expected one of: {string.Join(", ", ChatProviderNames.All)}.");

        return serviceProvider.GetKeyedService<IChatProvider>(providerKey)
            ?? throw new InvalidOperationException(
                $"No IChatProvider is registered under the key '{providerKey}' "
                + $"for model '{model}'.");
    }
}
