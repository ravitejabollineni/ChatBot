using ChatBot.Api.Features.Chat.Models;

namespace ChatBot.Api.Features.Chat.Contracts;

public interface IChatService
{
    Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ChatStreamChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
}