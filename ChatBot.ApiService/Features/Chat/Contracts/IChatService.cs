using ChatBot.Api.Features.Chat.Models;

namespace ChatBot.Api.Features.Chat.Contracts;

public interface IChatService
{
    Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the conversation, builds the prompt, and selects/validates the context window —
    /// everything that can throw <see cref="ChatBot.Api.Common.Errors.ConversationContextTooLargeException"/>
    /// or a not-found error. This is a plain async method (not an iterator), so callers that
    /// await it before starting an SSE response get a normal, eagerly-observable exception
    /// instead of one buried inside the stream.
    /// </summary>
    Task<ChatStreamContext> PrepareStreamAsync(ChatRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatStreamChunk> StreamAsync(ChatStreamContext preparedContext, CancellationToken cancellationToken = default);
}