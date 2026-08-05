using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Chat.Models;
using System.Runtime.CompilerServices;

namespace ChatBot.Api.Features.Chat.Contracts;

public interface IChatProvider
{
    string Name { get; }

    bool CanHandle(string model);

    Task<string> SendAsync(string model, IReadOnlyCollection<ConversationMessage> messages, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatStreamChunk> StreamAsync(string model, IReadOnlyCollection<ConversationMessage> messages, CancellationToken cancellationToken = default);
}