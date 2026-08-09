using ChatBot.Api.Features.Conversations.Contracts;
using ChatBot.Api.Features.Conversations.Delete;
using FastEndpoints;

namespace ChatBot.Api.Features.Conversations.Delete;

public sealed class DeleteConversationEndpoint(
    IConversationService service)
    : Endpoint<DeleteConversationRequest>
{
    public override void Configure()
    {
        Delete("/api/conversations/{conversationId:guid}");

        AllowAnonymous();

        Summary(summary =>
        {
            summary.Summary = "Delete Conversation";
            summary.Description = "Deletes a conversation and its messages.";
        });
    }

    public override async Task HandleAsync(
        DeleteConversationRequest request,
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

        await service.DeleteAsync(
            request.ConversationId,
            ct);

        await Send.NoContentAsync(ct);
    }
}
