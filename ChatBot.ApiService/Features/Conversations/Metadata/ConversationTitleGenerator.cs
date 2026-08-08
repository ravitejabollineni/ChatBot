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
    private const int MaxContextCharacters = 2000;

    public async Task<string?> TryGenerateTitleAsync(
        Conversation conversation,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var firstUserMessage = conversation.Messages
            .FirstOrDefault(message => message.Role == ChatRole.User);

        var firstAssistantMessage = conversation.Messages
            .FirstOrDefault(message => message.Role == ChatRole.Assistant);

        // A useful title requires both sides of the first exchange.
        if (firstUserMessage is null || firstAssistantMessage is null)
        {
            return null;
        }

        var userContent = Truncate(firstUserMessage.Content);
        var assistantContent = Truncate(firstAssistantMessage.Content);

        var titleContext =
            $"User: {userContent}\n" +
            $"Assistant: {assistantContent}";

        var wrapper = new ConversationMessage(
            conversation.Id,
            ChatRole.User,
            titleContext,
            timeProvider.GetUtcNow());

        var promptHistory = await conversationBuilder.BuildAsync(
            PromptNames.ConversationTitle,
            conversation.Id,
            [wrapper],
            cancellationToken);

        var provider = providerFactory.GetProvider(model);

        var rawTitle = await provider.SendAsync(
            model,
            promptHistory,
            cancellationToken);

        return Sanitize(rawTitle);
    }

    private static string Truncate(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length <= MaxContextCharacters)
        {
            return value;
        }

        return value[..MaxContextCharacters];
    }

    private static string? Sanitize(string? rawTitle)
    {
        if (string.IsNullOrWhiteSpace(rawTitle))
        {
            return null;
        }

        var title = rawTitle.Trim();

        // Convert multiline model output into a single line.
        title = title.ReplaceLineEndings(" ");

        // Remove common "Title:" prefix.
        if (title.StartsWith(
                "title:",
                StringComparison.OrdinalIgnoreCase))
        {
            title = title["title:".Length..].Trim();
        }

        // Remove Markdown heading markers.
        title = title.TrimStart('#').TrimStart();

        // Remove surrounding quotes.
        title = title.Trim(
            '"',
            '\'',
            '“',
            '”',
            '‘',
            '’');

        // Remove punctuation from the end.
        title = title.TrimEnd(
            '.',
            '!',
            '?',
            ';',
            ',',
            ':');

        // Normalize whitespace.
        title = string.Join(
            ' ',
            title.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        // Maximum five words.
        var words = title.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        if (words.Length > MaxWords)
        {
            words = words[..MaxWords];
        }

        title = string.Join(' ', words);

        // Maximum approximately 60 characters.
        if (title.Length > MaxLength)
        {
            title = title[..MaxLength].TrimEnd();
        }

        return string.IsNullOrWhiteSpace(title)
            ? null
            : title;
    }
}