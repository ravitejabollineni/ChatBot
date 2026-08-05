namespace ChatBot.Api.AI.Configuration;

public sealed class SystemPromptOptions
{
    public const string SectionName = "Chat";

    public string SystemPrompt { get; init; } = string.Empty;
}
