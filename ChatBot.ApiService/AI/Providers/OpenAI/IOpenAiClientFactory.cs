using OpenAI.Chat;

namespace ChatBot.Api.AI.Providers.OpenAI
{
    public interface IOpenAiClientFactory
    {
        ChatClient Create(string model);
    }
}
