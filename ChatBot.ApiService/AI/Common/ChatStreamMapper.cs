using ChatBot.Api.Features.Chat.Models;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace ChatBot.Api.AI.Common;

public static class ChatStreamMapper
{
    public static async IAsyncEnumerable<ChatStreamChunk> ToStreamChunks(
        IAsyncEnumerable<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        await foreach (var update in updates)
        {
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

        yield return ChatStreamChunk.Completed();
    }
}
