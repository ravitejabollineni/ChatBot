using ChatBot.Web.Contracts.Chat;
using Refit;

namespace ChatBot.Web.Features.Chat.Services.Api;

public interface IChatApi
{
    [Post("/api/chat/messages")]
    Task<SendMessageResponse> SendMessageAsync(
        [Body] SendMessageRequest request);

    [Get("/api/chat/models")]
    Task<GetModelsResponse> GetModelsAsync();
}