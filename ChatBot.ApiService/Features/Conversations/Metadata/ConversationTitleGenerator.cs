using ChatBot.Api.AI.Prompting.Contracts;
using ChatBot.Api.AI.Prompts;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Features.Chat.Contracts;

namespace ChatBot.Api.Features.Conversations.Metadata;

public sealed class ConversationTitleGenerator(
    IConversationBuilder conversationBuilder,
    IChatProviderFactory providerFactory,
    TimeProvider timeProvider)
    : IConversationTitleGenerator
{
    private const int MaxWords = 5;
    private const int MaxLength = 60;

    public async Task<string?> TryGenerateTitleAsync(
        Conversation conversation,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        var firstUserMessage = conversation.Messages.FirstOrDefault(m => m.Role == ChatRole.User);
        var firstAssistantMessage = conversation.Messages.FirstOrDefault(m => m.Role == ChatRole.Assistant);

        if (firstUserMessage is null || firstAssistantMessage is null)
        {
            return null;
        }

        var wrapper = new ConversationMessage(
            ChatRole.User,
            $"User: {firstUserMessage.Content}\nAssistant: {firstAssistantMessage.Content}",
            timeProvider.GetUtcNow());

        var promptHistory = await conversationBuilder.BuildAsync(
            PromptNames.ConversationTitle,
            [wrapper],
            cancellationToken);

        var provider = providerFactory.GetProvider(model);

        var rawTitle = await provider.SendAsync(model, promptHistory, cancellationToken);

        return Sanitize(rawTitle);
    }

    private static string? Sanitize(string? rawTitle)
    {
        if (string.IsNullOrWhiteSpace(rawTitle))
        {
            return null;
        }

        var title = rawTitle.Trim();

        // A model that ignores "no label" instructions typically prefixes with exactly this.
        if (title.StartsWith("title:", StringComparison.OrdinalIgnoreCase))
        {
            title = title["title:".Length..].Trim();
        }

        // A model that ignores "no Markdown" typically wraps the whole title as a heading.
        title = title.TrimStart('#').TrimStart();

        title = title.Trim('"', '\'', '“', '”', '‘', '’');
        title = title.TrimEnd('.', '!', '?', ';', ',');

        var words = title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length > MaxWords)
        {
            words = words[..MaxWords];
        }

        title = string.Join(' ', words);

        if (title.Length > MaxLength)
        {
            title = title[..MaxLength].TrimEnd();
        }

        return string.IsNullOrWhiteSpace(title) ? null : title;
    }
}
