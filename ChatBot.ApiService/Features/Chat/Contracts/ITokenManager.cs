using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.ValueObjects;

namespace ChatBot.Api.Features.Chat.Contracts;

public interface ITokenManager
{
    int EstimateTokenCount(IReadOnlyCollection<ConversationMessage> messages);

    Task<TokenUsage> CalculateAsync(
        IReadOnlyCollection<ConversationMessage> requestMessages,
        string assistantResponse,
        string model,
        CancellationToken cancellationToken = default);
}
