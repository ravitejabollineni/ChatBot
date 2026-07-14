using ChatBot.Api.Domain.Entities;

namespace ChatBot.Api.Features.Chat.Contracts;

public interface IChatProvider
{
    string Name { get; }

    bool CanHandle(string model);

    Task<string> SendAsync(
        string model,
        IReadOnlyCollection<ConversationMessage> messages,
        CancellationToken cancellationToken = default);
}