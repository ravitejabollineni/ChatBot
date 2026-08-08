using ChatBot.Api.Domain.Entities;

namespace ChatBot.Api.AI.Prompting.Contracts;

public interface IConversationBuilder
{
    Task<IReadOnlyCollection<ConversationMessage>> BuildAsync(
        string promptName,
        Guid conversationId,
        IReadOnlyCollection<ConversationMessage> conversation,
        CancellationToken cancellationToken = default);
}