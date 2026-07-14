using OpenAI.Chat;

namespace ChatBot.Api.Infrastructure.Providers.OpenAI
{
    public interface IOpenAiClientFactory
    {
        ChatClient Create(string model);
    }
}
