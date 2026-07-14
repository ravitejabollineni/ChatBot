using ChatBot.Api.Features.Conversations.Contracts;
using ChatBot.Api.Features.Conversations.List;
using FastEndpoints;

namespace ChatBot.Api.Features.Conversations.List;

public sealed class ListConversationsEndpoint(
    IConversationService conversationService)
    : EndpointWithoutRequest<IReadOnlyCollection<ListConversationResponse>>
{
    public override void Configure()
    {
        Get("/api/conversations");

        AllowAnonymous();

        Summary(summary =>
        {
            summary.Summary = "List Conversations";
            summary.Description = "Returns all conversations.";
        });
    }

    public override async Task HandleAsync(
        CancellationToken ct)
    {
        var conversations =
            await conversationService.ListAsync(ct);

        var response =
            conversations
                .Select(x => x.ToResponse())
                .ToArray();

        await Send.OkAsync(response, ct);
    }
}