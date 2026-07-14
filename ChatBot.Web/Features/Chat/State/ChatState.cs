using ChatBot.Web.Features.Chat.Contracts.Conversation;

namespace ChatBot.Web.Features.Chat.State;

public sealed class ChatState
{
    public Guid? SelectedConversationId { get; private set; }

    public string SelectedModel { get; private set; } = "gpt-4.1-mini";

    public IReadOnlyList<ConversationSummaryResponse> Conversations
    {
        get;
        private set;
    } = [];

    public GetConversationResponse? CurrentConversation
    {
        get;
        private set;
    }

    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    public void SetSelectedConversation(Guid conversationId)
    {
        SelectedConversationId = conversationId;

        NotifyStateChanged();
    }

    public void SetConversation(GetConversationResponse conversation)
    {
        CurrentConversation = conversation;

        NotifyStateChanged();
    }

    public void SetConversations(
        IReadOnlyList<ConversationSummaryResponse> conversations)
    {
        Conversations = conversations;

        NotifyStateChanged();
    }

    public void SetModel(string model)
    {
        SelectedModel = model;

        NotifyStateChanged();
    }

    public void SetError(string? message)
    {
        ErrorMessage = message;

        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}