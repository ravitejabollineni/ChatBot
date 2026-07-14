using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Features.Chat.Contracts;
using Microsoft.Extensions.AI;

namespace ChatBot.Api.Infrastructure.Providers.AzureOpenAI;

public sealed class AzureOpenAiChatProvider(
    IChatClient chatClient)
    : IChatProvider
{
    public string Name => "Azure OpenAI";

    public bool CanHandle(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        return true;
    }

    public async Task<string> SendAsync(
        string model,
        IReadOnlyCollection<ConversationMessage> messages,
        CancellationToken cancellationToken = default)
    {
        List<ChatMessage> history = [];

        foreach (var message in messages)
        {
            history.Add(
                new ChatMessage(
                    message.Role switch
                    {
                        ChatBot.Api.Domain.Enums.ChatRole.System
                            => Microsoft.Extensions.AI.ChatRole.System,

                        ChatBot.Api.Domain.Enums.ChatRole.User
                            => Microsoft.Extensions.AI.ChatRole.User,

                        ChatBot.Api.Domain.Enums.ChatRole.Assistant
                            => Microsoft.Extensions.AI.ChatRole.Assistant,

                        _ => throw new NotSupportedException()
                    },
                    message.Content));
        }

        var response = await chatClient.GetResponseAsync(
            history,
            cancellationToken: cancellationToken);

        return response.Text ?? string.Empty;
    }
}