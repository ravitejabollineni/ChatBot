using ChatBot.Api.AI.Configuration;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace ChatBot.Api.Features.Chat.Endpoints.GetModels;

/// <summary>
/// Lists the models the chat UI may select between, driven entirely by
/// <c>AI:AvailableModels</c> — see that property's remarks for how each entry maps onto
/// the active provider.
/// </summary>
public sealed class GetModelsEndpoint(
    IOptions<AiOptions> aiOptions)
    : EndpointWithoutRequest<GetModelsResponse>
{
    public override void Configure()
    {
        Get("/api/chat/models");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get Available Models";
            summary.Description = "Lists the models the chat UI may select from, and which one is the default.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // The client only ever needs the model names to populate the picker — which
        // provider serves each one is resolved server-side, per request, by
        // ChatProviderFactory.
        var models = aiOptions.Value.AvailableModels
            .Select(m => m.Model)
            .ToList();

        // The list is empty in a deployment that never set AI:AvailableModels — there's
        // nothing to pick between, but the response still has to be well-formed rather
        // than handing the client a default that isn't in its own Models list.
        var defaultModel = models.Count > 0
            ? models[0]
            : string.Empty;

        await Send.OkAsync(new GetModelsResponse(models, defaultModel));
    }
}
