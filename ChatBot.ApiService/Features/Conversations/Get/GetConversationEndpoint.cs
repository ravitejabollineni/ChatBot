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
            await service.GetAsync(
                request.ConversationId,
                ct);

        if (conversation is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(
            conversation.ToResponse(),
            ct);
    }
}