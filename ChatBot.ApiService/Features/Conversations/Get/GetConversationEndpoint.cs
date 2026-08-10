using ChatBot.Api.Common.Errors;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Conversations.Contracts;
using ChatBot.Api.Features.Conversations.Get;
using FastEndpoints;

namespace ChatBot.Api.Features.Conversations.Get;

public sealed class GetConversationEndpoint(
    IConversationService service)
    : Endpoint<GetConversationRequest, GetConversationResponse>
{
    public override void Configure()
    {
        Get("/api/conversations/{conversationId:guid}");

        AllowAnonymous();
    }

    public override async Task HandleAsync(
        GetConversationRequest request,
        CancellationToken ct)
    {
        var conversation =
            await service.GetRequiredAsync(
                request.ConversationId,
                ct);

        await Send.OkAsync(
            conversation.ToResponse(),
            ct);
    }
}