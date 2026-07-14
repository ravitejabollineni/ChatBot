using ChatBot.Api.Domain.Entities;

namespace ChatBot.Api.Infrastructure.Persistence
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetByIdAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Conversation>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Conversation conversation,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default);
    }
}
