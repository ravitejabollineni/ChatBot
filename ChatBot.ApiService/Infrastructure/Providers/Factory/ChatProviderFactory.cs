using ChatBot.Api.Features.Chat.Contracts;

namespace ChatBot.Api.Infrastructure.Providers.Factory;

public sealed class ChatProviderFactory(
    IEnumerable<IChatProvider> providers)
    : IChatProviderFactory
{
    public IChatProvider GetProvider(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        return providers.FirstOrDefault(p => p.CanHandle(model))
               ?? throw new InvalidOperationException(
                   $"No provider registered for model '{model}'.");
    }
}