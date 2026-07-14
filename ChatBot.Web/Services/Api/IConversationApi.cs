using ChatBot.Web.Contracts.Conversations;
using ChatBot.Web.Features.Chat.Contracts.Conversation;
using Refit;

namespace ChatBot.Web.Features.Chat.Services.Api;

public interface IConversationApi
{
    [Post("/api/conversations")]
    Task<CreateConversationResponse> CreateConversationAsync();

    [Get("/api/conversations")]
    Task<IReadOnlyList<ConversationSummaryResponse>> GetConversationsAsync();

    [Get("/api/conversations/{conversationId}")]
    Task<GetConversationResponse> GetConversationAsync(Guid conversationId);
}