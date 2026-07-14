namespace ChatBot.Api.Features.Chat.Contracts;

public interface IChatProviderFactory
{
    IChatProvider GetProvider(string model);
}