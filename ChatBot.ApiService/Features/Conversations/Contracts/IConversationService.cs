using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Conversations.Get;
using ChatBot.Api.Features.Conversations.Create;

namespace ChatBot.Api.Features.Conversations.Contracts
{
    public interface IConversationService
    {
        Task<Guid> CreateAsync(
        CancellationToken cancellationToken = default);

        Task<Conversation?> GetAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Conversation>> ListAsync(
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default);

    }
}
