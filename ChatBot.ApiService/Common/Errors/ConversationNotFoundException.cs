namespace ChatBot.Api.Common.Errors;

public sealed class ConversationNotFoundException(Guid conversationId)
    : Exception($"Conversation '{conversationId}' was not found.")
{
    public Guid ConversationId { get; } = conversationId;
}