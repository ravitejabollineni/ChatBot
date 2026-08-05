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

    public override Task HandleAsync(
        StreamMessageRequest request,
        CancellationToken ct)
        => Send.EventStreamAsync(EventName, chatService.StreamAsync( new ChatRequest(request.ConversationId, request.Model, request.Message), ct), ct);
}
