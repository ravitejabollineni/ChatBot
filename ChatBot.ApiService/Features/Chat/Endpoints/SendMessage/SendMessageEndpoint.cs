using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Models;
using FastEndpoints;

namespace ChatBot.Api.Features.Chat.Endpoints.SendMessage;

public sealed class SendMessageEndpoint(
    IChatService chatService)
    : Endpoint<SendMessageRequest, SendMessageResponse>
{
    public override void Configure()
    {
        Post("/api/chat/messages");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Send Message";
            summary.Description = "Sends a message to the selected model.";
        });
    }

    public override async Task HandleAsync(
        SendMessageRequest request,
        CancellationToken ct)
    {
        var response = await chatService.SendAsync(
            new ChatRequest(
                request.ConversationId,
                request.Model,
                request.Message),
            ct);

        await Send.OkAsync(
            new SendMessageResponse(
                ConversationId: request.ConversationId,
                Model: request.Model,
                Response: response.Response,
                CreatedAt: response.CreatedAt,
                TokenUsage: response.TokenUsage));
    }
}