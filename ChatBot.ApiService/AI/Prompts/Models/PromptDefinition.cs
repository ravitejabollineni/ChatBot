namespace ChatBot.Api.AI.Prompts.Models
{
    public sealed record PromptDefinition(
    PromptMetadata Metadata,
    string Content);
}
