using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace ChatBot.Api.Infrastructure.Providers.OpenAI;

public sealed class OpenAiChatProvider(
    IOptions<OpenAiOptions> options)
    : IChatProvider
{
    public string Name => "OpenAI";

    public bool CanHandle(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        return options.Value.Models.Contains(
            model,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<string> SendAsync(
        string model,
        IReadOnlyCollection<ConversationMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(messages);

        var client = new ChatClient(
            model: model,
            apiKey: options.Value.ApiKey);

        List<ChatMessage> chatMessages = [];

        foreach (var message in messages)
        {
            switch (message.Role)
            {
                case ChatRole.System:
                    chatMessages.Add(
                        new SystemChatMessage(message.Content));
                    break;

                case ChatRole.User:
                    chatMessages.Add(
                        new UserChatMessage(message.Content));
                    break;

                case ChatRole.Assistant:
                    chatMessages.Add(
                        new AssistantChatMessage(message.Content));
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported chat role '{message.Role}'.");
            }
        }

        ChatCompletion completion =
            await client.CompleteChatAsync(
                chatMessages,
                cancellationToken: cancellationToken);

        return completion.Content.FirstOrDefault()?.Text
               ?? string.Empty;
    }
}