using ChatBot.Api.AI.Configuration;
using ChatBot.Api.AI.Providers.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

public sealed class OpenAiClientFactory(
    IOptions<OpenAiOptions> options)
    : IOpenAiClientFactory
{
    public ChatClient Create(string model)
        => new(model, options.Value.ApiKey);
}