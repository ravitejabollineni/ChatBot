using ChatBot.Web.Contracts.Chat;
using ChatBot.Web.Features.Chat.Contracts.Conversation;
using ChatBot.Web.Features.Chat.Services.Api;
using ChatBot.Web.Features.Chat.State;

namespace ChatBot.Web.Features.Chat.Services;

public sealed class ChatUiService(
    IConversationApi conversationApi,
    IChatApi chatApi,
    ChatState chatState)
{
    public async Task InitializeAsync()
    {
        try
        {
            var conversations =
                await conversationApi.GetConversationsAsync();

            chatState.SetConversations(conversations);
            chatState.SetError(null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to load conversations: {ex.Message}");
            chatState.SetConversations([]);
            chatState.SetError("Unable to load conversations. Please refresh the page.");
        }
    }

    public async Task CreateConversationAsync()
    {
        try
        {
            var response =
                await conversationApi.CreateConversationAsync();

            await LoadConversationAsync(response.ConversationId);

            await RefreshConversationListAsync();

            chatState.SetError(null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to create a conversation: {ex.Message}");
            chatState.SetError("Unable to start a new chat. Please try again.");
        }
    }

    public async Task LoadConversationAsync(Guid conversationId)
    {
        try
        {
            var conversation =
                await conversationApi.GetConversationAsync(conversationId);

            chatState.SetSelectedConversation(conversationId);

            chatState.SetConversation(conversation);
            chatState.SetError(null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to load conversation {conversationId}: {ex.Message}");
            chatState.SetError("Unable to load that conversation. Please try again.");
        }
    }

    public async Task SendMessageAsync(
        string model,
        string message)
    {
        if (chatState.SelectedConversationId is null)
            return;

        try
        {
            await chatApi.SendMessageAsync(
                new SendMessageRequest(
                    chatState.SelectedConversationId.Value,
                    model,
                    message));

            await LoadConversationAsync(
                chatState.SelectedConversationId.Value);

            chatState.SetError(null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to send message: {ex.Message}");
            chatState.SetError("Unable to send that message. Please try again.");
            throw;
        }
    }

    private async Task RefreshConversationListAsync()
    {
        try
        {
            var conversations =
                await conversationApi.GetConversationsAsync();

            chatState.SetConversations(conversations);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to refresh conversations: {ex.Message}");
            chatState.SetError("Unable to refresh the conversation list.");
        }
    }
}