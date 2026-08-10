using ChatBot.Api.AI.Common;
using ChatBot.Api.AI.Configuration;
using ChatBot.Api.AI.Routing;
using ChatBot.Api.Common.Errors;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Models;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace ChatBot.Api.AI.Providers.Gemini;

public sealed class GeminiChatProvider(
    Client client,
    IOptions<GeminiOptions> options)
    : IChatProvider
{
    public string Name => ChatProviderNames.GeminiAI;

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

        var (systemInstruction, contents) = ChatMessageMapper.ToGeminiContents(messages);

        try
        {
            var response = await client.Models.GenerateContentAsync(
                model,
                contents,
                CreateConfig(systemInstruction),
                cancellationToken);

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

    public async IAsyncEnumerable<ChatStreamChunk> StreamAsync(
        string model,
        IReadOnlyCollection<ConversationMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentNullException.ThrowIfNull(messages);

        var (systemInstruction, contents) = ChatMessageMapper.ToGeminiContents(messages);

        var stream = client.Models.GenerateContentStreamAsync(
            model,
            contents,
            CreateConfig(systemInstruction),
            cancellationToken);

        // yield return cannot appear inside a try block that has a catch, so MoveNextAsync
        // (where the provider call actually happens) is isolated in its own try/catch with no
        // yield, and the yield stays in the outer try/finally that only disposes the enumerator.
        var enumerator = stream.GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {                
                GenerateContentResponse chunk;                
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    chunk = enumerator.Current;
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

                if (!string.IsNullOrEmpty(chunk.Text))
                {
                    yield return ChatStreamChunk.TextMessage(chunk.Text);
                }
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        yield return ChatStreamChunk.Completed();
    }

    private static GenerateContentConfig CreateConfig(string? systemInstruction)
        => new()
        {
            SystemInstruction = systemInstruction is null
                ? null
                : new Content
                {
                    Role = "user",
                    Parts = [Part.FromText(systemInstruction)]
                }
        };
}
