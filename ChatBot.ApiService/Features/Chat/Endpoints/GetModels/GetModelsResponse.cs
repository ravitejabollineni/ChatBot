namespace ChatBot.Api.Features.Chat.Endpoints.GetModels;

public sealed record GetModelsResponse(
    IReadOnlyList<string> Models,
    string DefaultModel);
