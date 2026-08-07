using ChatBot.Api.Common.Text;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Domain.ValueObjects;
using ChatBot.Api.Features.Conversations.Contracts;

namespace ChatBot.Api.Features.Conversations.Metadata;

public sealed class ConversationMetadataService(
    IServiceScopeFactory scopeFactory,
    ILogger<ConversationMetadataService> logger)
    : IConversationMetadataService
{
    private const int PreviewMaxLength = 140;

    public void ApplyPreview(Conversation conversation, ConversationMessage assistantMessage)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(assistantMessage);

        var preview = MarkdownPlainTextConverter.ToPreview(assistantMessage.Content, PreviewMaxLength);

        var titleStatus = conversation.Metadata.TitleStatus == ConversationTitleStatus.NotGenerated
            && IsEligibleForTitleGeneration(conversation)
                ? ConversationTitleStatus.Generating
                : conversation.Metadata.TitleStatus;

        conversation.UpdateMetadata(conversation.Metadata with
        {
            Preview = preview,
            TitleStatus = titleStatus,
        });
    }

    public Task ScheduleTitleGenerationAsync(
        Conversation conversation,
        string model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        if (conversation.Metadata.TitleStatus != ConversationTitleStatus.Generating)
        {
            return Task.CompletedTask;
        }

        var conversationId = conversation.Id;

        // Detached on purpose: the request/stream this turn belongs to may finish (and its
        // scoped services get disposed) well before an LLM title call would return, so this
        // runs in its own scope with its own lifetime, the same rationale
        // ChatService.PersistConversationTurnAsync already uses for CancellationToken.None.
        _ = Task.Run(
            () => GenerateAndPersistTitleAsync(conversationId, model),
            CancellationToken.None);

        return Task.CompletedTask;
    }

    private async Task GenerateAndPersistTitleAsync(Guid conversationId, string model)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
            var titleGenerator = scope.ServiceProvider.GetRequiredService<IConversationTitleGenerator>();

            var conversation = await conversationService.GetAsync(conversationId, CancellationToken.None);

            if (conversation is null)
            {
                return;
            }

            var title = await titleGenerator.TryGenerateTitleAsync(conversation, model, CancellationToken.None);

            conversation.UpdateMetadata(conversation.Metadata with
            {
                Title = title ?? conversation.Metadata.Title,
                TitleStatus = title is not null
                    ? ConversationTitleStatus.Generated
                    : ConversationTitleStatus.NotGenerated,
            });

            await conversationService.SaveAsync(conversation, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate title for conversation {ConversationId}.", conversationId);
        }
    }

    // Deliberately permissive: ChatGPT/Claude title every conversation, including short
    // greetings ("Hello" becomes something like "Friendly Greeting") — the LLM-based
    // generator handles trivial exchanges fine, so the only real requirement is that there's
    // an actual exchange to title from.
    private static bool IsEligibleForTitleGeneration(Conversation conversation)
    {
        var firstUserMessage = conversation.Messages.FirstOrDefault(m => m.Role == ChatRole.User);
        var firstAssistantMessage = conversation.Messages.FirstOrDefault(m => m.Role == ChatRole.Assistant);

        return firstUserMessage is not null
            && firstAssistantMessage is not null
            && !string.IsNullOrWhiteSpace(firstUserMessage.Content)
            && !string.IsNullOrWhiteSpace(firstAssistantMessage.Content);
    }
}
