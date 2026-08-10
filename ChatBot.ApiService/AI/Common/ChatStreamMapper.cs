using ChatBot.Api.Common.Errors;
using ChatBot.Api.Features.Chat.Models;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace ChatBot.Api.AI.Common;

public static class ChatStreamMapper
{
    public static async IAsyncEnumerable<ChatStreamChunk> ToStreamChunks(
        IAsyncEnumerable<ChatResponseUpdate> updates,
        string provider,
        string? model,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        // yield return cannot appear inside a try block that has a catch, so MoveNextAsync
        // (where the provider call actually happens) is isolated in its own try/catch with no
        // yield, and the yield stays in the outer try/finally that only disposes the enumerator.
        var enumerator = updates.GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                ChatResponseUpdate update;

                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    update = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ChatProviderException(
                        "The chat provider failed to generate a response.",
                        provider,
                        model,
                        ex);
                }

                foreach (var content in update.Contents)
                {
                    if (content is not TextContent textContent)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(textContent.Text))
                    {
                        continue;
                    }

                    yield return ChatStreamChunk.TextMessage(textContent.Text);
                }
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        yield return ChatStreamChunk.Completed();
    }
}
