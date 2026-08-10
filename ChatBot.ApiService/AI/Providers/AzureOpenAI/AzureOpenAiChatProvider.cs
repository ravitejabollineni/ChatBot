using ChatBot.Api.AI.Common;
using ChatBot.Api.AI.Routing;
using ChatBot.Api.Common.Errors;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace ChatBot.Api.AI.Providers.AzureOpenAI;

/// <summary>
/// Chat provider backed by an Azure OpenAI deployment, reached through
/// Microsoft.Extensions.AI's <see cref="IChatClient"/> abstraction.
/// </summary>
/// <remarks>
/// <paramref name="chatClient"/> comes from the keyed "AzureOpenAI" registration rather
/// than an unkeyed <see cref="IChatClient"/>. Every provider that needs a client resolves
/// its own keyed one, so no two registrations can overwrite each other, and the client this
/// provider depends on is only ever built when this provider is the selected one.
/// </remarks>
public sealed class AzureOpenAiChatProvider(
    [FromKeyedServices(ChatProviderNames.AzureOpenAI)] IChatClient chatClient)
    : IChatProvider
{
    // Must match the key used by AI:DefaultProvider and ChatProviderFactory's lookup.
    public string Name => ChatProviderNames.AzureOpenAI;

    // Retained for interface compatibility; ChatProviderFactory no longer calls this
    // to select a provider (see ChatProviderFactory's remarks). Azure OpenAI resources
    // are typically single-deployment, so accepting any model string here is correct
    // regardless.
    public bool CanHandle(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        return true;
    }

    public async Task<string> SendAsync(string model, IReadOnlyCollection<ConversationMessage> messages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(model);

        var history = ChatMessageMapper.ToAiMessages(messages);

        try
        {
            var response = await chatClient.GetResponseAsync(
                history,
                cancellationToken: cancellationToken);

            return response.Text ?? string.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ChatProviderException(
                "The chat provider failed to generate a response.",
                Name,
                model,
                ex);
        }
    }

    public IAsyncEnumerable<ChatStreamChunk> StreamAsync(string model, IReadOnlyCollection<ConversationMessage> messages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(model);

        var history = ChatMessageMapper.ToAiMessages(messages);

        return ChatStreamMapper.ToStreamChunks(
            chatClient.GetStreamingResponseAsync(history, cancellationToken: cancellationToken),
            Name,
            model,
            cancellationToken);
    }
}