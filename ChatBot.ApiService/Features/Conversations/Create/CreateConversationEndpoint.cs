using ChatBot.Api.Features.Conversations.Contracts;
using ChatBot.Api.Features.Conversations.Create;
using FastEndpoints;

namespace ChatBot.Api.Features.Conversations.Create;

public sealed class Endpoint(
    IConversationService conversationService)
    : EndpointWithoutRequest<CreateConversationResponse>
{
    public override void Configure()
    {
        Post("/api/conversations");

        AllowAnonymous();

        Summary(summary =>
        {
            summary.Summary = "Create Conversation";
            summary.Description = "Creates a new conversation.";
        });

        Description(builder =>
        {
            builder
                .Produces<CreateConversationResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        });
    }

    public override async Task HandleAsync(
       CancellationToken ct)
    {
        var conversationId =
            await conversationService.CreateAsync(ct);

        await Send.OkAsync(
            new CreateConversationResponse(conversationId),
            ct);
    }
}