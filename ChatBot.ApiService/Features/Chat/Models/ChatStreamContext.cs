using ChatBot.Api.Domain.Entities;

namespace ChatBot.Api.Features.Chat.Models;

/// <summary>
/// Result of the eager pre-flight work for a streaming request: the conversation is loaded
/// and the prompt context is selected (and validated against the model's token budget) once,
/// before any SSE response starts, then threaded through to <c>ChatService.StreamAsync</c>
/// so it never re-loads the conversation or re-runs context selection.
/// </summary>
public sealed record ChatStreamContext(
    Conversation Conversation,
    ConversationMessage UserMessage,
    string Model,
    IReadOnlyList<ConversationMessage> PromptContext);
