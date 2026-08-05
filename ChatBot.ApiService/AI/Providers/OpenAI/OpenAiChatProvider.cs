using ChatBot.Api.AI.Common;
using ChatBot.Api.AI.Configuration;
using ChatBot.Api.AI.Routing;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Runtime.CompilerServices;

namespace ChatBot.Api.AI.Providers.OpenAI;

public sealed class OpenAiChatProvider(
    IOptions<OpenAiOptions> options)
    : IChatProvider
{
    public string Name => ChatProviderNames.OpenAI;

    public bool CanHandle(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        return string.Equals(
            options.Value.Model,
            model,
            StringComparison.OrdinalIgnoreCase);
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

        var chatMessages = ChatMessageMapper.ToOpenAiMessages(messages);

        ChatCompletion completion =
            await client.CompleteChatAsync(
                chatMessages,
                cancellationToken: cancellationToken);

        return completion.Content.FirstOrDefault()?.Text
               ?? string.Empty;
    }

    public async IAsyncEnumerable<ChatStreamChunk> StreamAsync(
        string model,
        IReadOnlyCollection<ConversationMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(messages);

        var chatMessages = ChatMessageMapper.ToOpenAiMessages(messages);
        var client = new ChatClient(
            model: model,
            apiKey: options.Value.ApiKey);

        await foreach (var chunk in client.CompleteChatStreamingAsync(chatMessages, cancellationToken: cancellationToken))
        {
            foreach (var part in chunk.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return ChatStreamChunk.TextMessage(part.Text);
                }
            }
        }

        yield return ChatStreamChunk.Completed();
    }
}