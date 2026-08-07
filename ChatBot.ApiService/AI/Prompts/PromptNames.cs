namespace ChatBot.Api.AI.Prompts;

public static class PromptNames
{
    /// <summary>
    /// Default prompt used for general chat conversations.
    /// </summary>
    public const string Chat = "Chat";

    /// <summary>
    /// Prompt used for resume analysis.
    /// </summary>
    public const string Resume = "Resume";

    /// <summary>
    /// Prompt used for SQL assistance.
    /// </summary>
    public const string Sql = "Sql";

    /// <summary>
    /// Prompt used for code reviews.
    /// </summary>
    public const string CodeReview = "CodeReview";

    /// <summary>
    /// Prompt used for research-related tasks.
    /// </summary>
    public const string Research = "Research";

    /// <summary>
    /// Prompt used to generate a short conversation title from its first exchange.
    /// </summary>
    public const string ConversationTitle = "ConversationTitle";
}