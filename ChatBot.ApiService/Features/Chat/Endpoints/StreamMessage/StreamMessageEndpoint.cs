using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Models;
using FastEndpoints;

namespace ChatBot.Api.Features.Chat.Endpoints.StreamMessage;

public sealed class StreamMessageEndpoint(IChatService chatService): Endpoint<StreamMessageRequest>
{
    private const string EventName = "chunk";

    public override void Configure()
    {
        Post("/api/chat/messages/stream");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Stream Message";
            summary.Description = "Streams an AI response token-by-token as server-sent events. Each " + $"'{EventName}' event carries a JSON ChatStreamChunk; the final chunk has " +
                "IsCompleted set to true.";
        });
        Description(builder => builder
                 .Accepts<StreamMessageRequest>("application/json")
                .Produces<ChatStreamChunk>(StatusCodes.Status200OK, "text/event-stream"),
            clearDefaults: true);
    }

    public override async Task HandleAsync(
        StreamMessageRequest request,
        CancellationToken ct)
    {
        // Awaited here, before Send.EventStreamAsync starts the SSE response, so a rejection
        // (e.g. context-too-large) surfaces as a normal 400 ProblemDetails instead of being
        // stranded inside an already-committed 200 SSE stream.
        var preparedContext = await chatService.PrepareStreamAsync(
            new ChatRequest(request.ConversationId, request.Model, request.Message),
            ct);

        await Send.EventStreamAsync(EventName, chatService.StreamAsync(preparedContext, ct), ct);
    }
}
