using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Features.Conversations.Get;

public static class ConversationMappings
{
    public static GetConversationResponse ToResponse(this Conversation conversation)
    {
        return new GetConversationResponse(
            conversation.Id,
            conversation.CreatedAt,
            conversation.LastUpdatedAt,
            conversation.Metadata.Title,
            conversation.Metadata.Preview,
            conversation.Messages
                .Select(m =>
                    new MessageResponse(
                        m.Id,
                        m.Role.ToString(),
                        m.Content,
                        m.CreatedAt,
                        m.TokenUsage,
                        m.IsPartial))
                .ToList());
    }
}