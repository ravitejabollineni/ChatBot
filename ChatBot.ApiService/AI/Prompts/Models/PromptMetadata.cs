namespace ChatBot.Api.AI.Prompts.Models
{
    public sealed record PromptMetadata(
    string Name,
    Version Version,
    string Description,
    string? Author,
    DateOnly? Created,
    IReadOnlyCollection<string> Tags,
    double? Temperature,
    int? MaxTokens);
}
