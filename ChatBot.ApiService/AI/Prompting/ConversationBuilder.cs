using ChatBot.Api.AI.Prompting.Contracts;
using ChatBot.Api.AI.Prompts;
using ChatBot.Api.AI.Prompts.Contracts;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;

namespace ChatBot.Api.AI.Prompting;

public sealed class ConversationBuilder(
    IPromptRepository promptRepository,
    TimeProvider timeProvider)
    : IConversationBuilder
{
    public async Task<IReadOnlyCollection<ConversationMessage>> BuildAsync(
        string promptName,
        IReadOnlyCollection<ConversationMessage> conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        var prompt = await promptRepository.GetAsync(promptName, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var messages = new List<ConversationMessage>(conversation.Count + 1)
        {
            new(
                ChatRole.System,
                prompt.Content,
                now)
        };

        messages.AddRange(conversation);

        return messages;
    }
}