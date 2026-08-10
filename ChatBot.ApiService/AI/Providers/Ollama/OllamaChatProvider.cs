using ChatBot.Api.AI.Common;
using ChatBot.Api.AI.Configuration;
using ChatBot.Api.AI.Routing;
using ChatBot.Api.Common.Errors;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;

namespace ChatBot.Api.AI.Providers.Ollama;

/// <summary>
/// Chat provider backed by a local Ollama server, reached through Microsoft.Extensions.AI's
/// <see cref="IChatClient"/> abstraction (OllamaSharp's <c>OllamaApiClient</c> implements it
/// directly). Message mapping is shared with
/// <see cref="ChatBot.Api.AI.Providers.AzureOpenAI.AzureOpenAiChatProvider"/> via
/// <see cref="ChatBot.Api.AI.Common.ChatMessageMapper"/> — only the client, the request
/// options and the model-matching rule differ, per the existing provider pattern.
/// </summary>
/// <remarks>
/// <paramref name="chatClient"/> is resolved from the keyed "Ollama" registration. Every
/// provider that needs a client resolves its own keyed one, so no two registrations can
/// overwrite each other in the container.
/// </remarks>
public sealed class OllamaChatProvider(
    [FromKeyedServices(ChatProviderNames.Ollama)] IChatClient chatClient,
    IOptions<OllamaOptions> options)
    : IChatProvider
{
    public string Name => ChatProviderNames.Ollama;

    /// <summary>
    /// Matches the single chat model configured for this provider
    /// (<c>AI:Providers:Ollama:ChatModel</c>), the same exact-match convention
    /// <see cref="ChatBot.Api.AI.Providers.OpenAI.OpenAiChatProvider"/> uses for its configured model.
    /// </summary>
    /// <remarks>
    /// Not consulted by <c>ChatProviderFactory</c>, which selects on
    /// <c>AI:DefaultProvider</c> instead — see that type's remarks.
    /// </remarks>
    public bool CanHandle(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        return string.Equals(
            options.Value.ChatModel,
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

        try
        {
            var response = await chatClient.GetResponseAsync(
                ChatMessageMapper.ToAiMessages(messages),
                CreateChatOptions(model),
                cancellationToken);

            return StripReasoning(response.Text);
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

    private static string StripReasoning(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        const string closingTag = "</think>";

        var end = text.LastIndexOf(closingTag, StringComparison.OrdinalIgnoreCase);

        if (end < 0)
        {
            return text;
        }

        var answer = text[(end + closingTag.Length)..].Trim();

        // A model that reasoned but produced no answer after the tag would otherwise come
        // back as an empty bubble; the monologue is more useful to the user than nothing.
        return answer.Length > 0 ? answer : text.Trim();
    }

    public IAsyncEnumerable<ChatStreamChunk> StreamAsync(
        string model,
        IReadOnlyCollection<ConversationMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(messages);

        var history = ChatMessageMapper.ToAiMessages(messages);

        return ChatStreamMapper.ToStreamChunks(
            chatClient.GetStreamingResponseAsync(
                history,
                CreateChatOptions(model),
                cancellationToken),
            Name,
            model,
            cancellationToken);
    }

    private ChatOptions CreateChatOptions(string model)
    {
        var chatOptions = new ChatOptions
        {
            ModelId = model,
            Reasoning = new ReasoningOptions
            {
                Effort = options.Value.EnableThinking
                    ? ReasoningEffort.Medium
                    : ReasoningEffort.None
            }
        };

        if (options.Value.NumCtx is int numCtx && numCtx > 0)
        {
            chatOptions.AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [OllamaOption.NumCtx.Name] = numCtx
            };
        }

        return chatOptions;
    }
}
