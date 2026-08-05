using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatBot.Web.Contracts.Chat;

namespace ChatBot.Web.Features.Chat.Services.Api;

// Refit has no support for text/event-stream responses, so streaming can't go through
// IChatApi like the rest of the chat endpoints. This is a plain typed HttpClient that
// posts the request and parses the SSE response body directly with the BCL's
// System.Net.ServerSentEvents.SseParser<T>, which ships in the net10.0 shared framework
// and needs no extra NuGet package.
public sealed class ChatStreamClient(HttpClient httpClient)
{
    public async IAsyncEnumerable<ChatStreamChunk> StreamMessageAsync(
        SendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat/messages/stream")
        {
            Content = JsonContent.Create(request, options: JsonSerializerOptions.Web),
        };

        // ResponseHeadersRead is essential: without it HttpClient buffers the entire body
        // before returning, and nothing streams.
        using var response = await httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        // Fail fast on 4xx/5xx rather than trying to parse an error body as SSE.
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var parser = SseParser.Create(stream, static (_, data) =>
            JsonSerializer.Deserialize<ChatStreamChunk>(data, JsonSerializerOptions.Web)!);

        await foreach (var item in parser.EnumerateAsync(cancellationToken))
        {
            yield return item.Data;

            if (item.Data.IsCompleted)
            {
                yield break;
            }
        }
    }
}
