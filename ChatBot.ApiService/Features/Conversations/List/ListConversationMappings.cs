using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Conversations.List;

namespace ChatBot.Api.Features.Conversations.List;

public static class ListConversationMappings
{
    public static ListConversationResponse ToResponse(
        this Conversation conversation)
    {
        return new ListConversationResponse(
            ConversationId: conversation.Id,
            CreatedAt: conversation.CreatedAt,
            LastUpdatedAt: conversation.LastUpdatedAt);
    }
}