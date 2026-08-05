namespace ChatBot.Web.Contracts.Chat;

public sealed record GetModelsResponse(
    IReadOnlyList<string> Models,
    string DefaultModel);
