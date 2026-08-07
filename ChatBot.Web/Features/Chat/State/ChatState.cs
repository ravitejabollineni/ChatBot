using ChatBot.Web.Features.Chat.Contracts.Conversation;

namespace ChatBot.Web.Features.Chat.State;

public sealed class ChatState
{
    public Guid? SelectedConversationId { get; private set; }

    // Overwritten by SetAvailableModels once the API's configured model list loads; kept
    // as a fallback so the composer still has something valid to send if that fetch fails.
    public string SelectedModel { get; private set; } = "phi3:mini";

    public IReadOnlyList<string> AvailableModels { get; private set; } = [];

    public IReadOnlyList<ConversationSummaryResponse> Conversations
    {
        get;
        private set;
    } = [];

    /// <summary>
    /// True while the initial conversation list fetch is in flight, so the sidebar can show a
    /// loading skeleton instead of a "no conversations yet" empty state that would otherwise be
    /// indistinguishable from a genuinely empty list.
    /// </summary>
    public bool IsLoadingConversations { get; private set; } = true;

    public GetConversationResponse? CurrentConversation
    {
        get;
        private set;
    }

    public string? ErrorMessage { get; private set; }

    public bool IsGenerating { get; private set; }

    public string StreamingText { get; private set; } = string.Empty;

    // In-memory, per-circuit only — there is no backend field/endpoint for feedback, so this is
    // lost on reconnect/refresh rather than silently pretending to be persisted.
    private readonly Dictionary<Guid, bool?> messageFeedback = [];

    public event Action? StateChanged;

    public bool? GetMessageFeedback(Guid messageId) =>
        messageFeedback.TryGetValue(messageId, out var value) ? value : null;

    public void SetMessageFeedback(Guid messageId, bool isPositive)
    {
        if (GetMessageFeedback(messageId) == isPositive)
        {
            messageFeedback.Remove(messageId);
        }
        else
        {
            messageFeedback[messageId] = isPositive;
        }

        NotifyStateChanged();
    }

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

    /// <summary>
    /// Resets to a brand-new, unsaved conversation. Leaves <see cref="Conversations"/> untouched
    /// — a conversation is only added to the sidebar once the first message actually creates one
    /// server-side.
    /// </summary>
    public void StartNewConversation()
    {
        SelectedConversationId = null;
        CurrentConversation = null;
        StreamingText = string.Empty;
        IsGenerating = false;
        ErrorMessage = null;

        NotifyStateChanged();
    }

    /// <summary>
    /// Inserts or replaces a single sidebar entry in place, re-sorted by recency, instead of
    /// refetching the whole list — so a new or just-updated conversation appears immediately
    /// without a full-list reload flash.
    /// </summary>
    public void UpsertConversationSummary(ConversationSummaryResponse summary)
    {
        var withoutExisting = Conversations
            .Where(c => c.ConversationId != summary.ConversationId);

        Conversations = withoutExisting
            .Append(summary)
            .OrderByDescending(c => c.LastUpdatedAt)
            .ToList();

        NotifyStateChanged();
    }

    /// <summary>
    /// Applies a background-generated title to one sidebar entry, in place. Deliberately narrow:
    /// unlike <see cref="UpsertConversationSummary"/> this never touches Preview, never re-sorts,
    /// and has nothing to do with <see cref="IsGenerating"/> — title generation is a silent
    /// background update, not a loading state the UI should react to.
    /// </summary>
    public void UpdateConversationTitle(Guid conversationId, string title)
    {
        var changed = false;

        Conversations = Conversations
            .Select(c =>
            {
                if (c.ConversationId != conversationId || c.Title == title)
                {
                    return c;
                }

                changed = true;
                return c with { Title = title };
            })
            .ToList();

        if (changed)
        {
            NotifyStateChanged();
        }
    }

    public void SetConversations(
        IReadOnlyList<ConversationSummaryResponse> conversations)
    {
        Conversations = conversations;
        IsLoadingConversations = false;

        NotifyStateChanged();
    }

    public void SetModel(string model)
    {
        SelectedModel = model;

        NotifyStateChanged();
    }

    /// <summary>
    /// Populates the model picker from the API's configured list and selects
    /// <paramref name="defaultModel"/>. Called once, from <c>ChatUiService.InitializeAsync</c>.
    /// </summary>
    public void SetAvailableModels(IReadOnlyList<string> models, string defaultModel)
    {
        AvailableModels = models;

        if (!string.IsNullOrWhiteSpace(defaultModel))
        {
            SelectedModel = defaultModel;
        }

        NotifyStateChanged();
    }

    public void SetError(string? message)
    {
        ErrorMessage = message;

        NotifyStateChanged();
    }

    /// <summary>
    /// Adds the user's message to the current conversation straight away, before the API has
    /// been called, so it is on screen while the reply is being generated.
    /// </summary>
    /// <remarks>
    /// The view renders <see cref="CurrentConversation"/>, which is only refetched once
    /// <c>POST /api/chat/messages</c> returns. A local completion can take tens of seconds,
    /// and for all of it the user's own message was nowhere on screen — just a lone
    /// "Assistant is thinking" bubble, which reads as the message having been lost. The
    /// entry is replaced by the server's own copy when the conversation reloads, so
    /// <see cref="Guid.Empty"/> as a placeholder id is never persisted or sent anywhere.
    /// </remarks>
    public void AppendPendingUserMessage(string content)
    {
        if (CurrentConversation is null)
        {
            return;
        }

        var pending = new MessageResponse(
            MessageId: Guid.Empty,
            Role: "user",
            Content: content,
            CreatedAt: DateTimeOffset.UtcNow,
            TokenUsage: null);

        CurrentConversation = CurrentConversation with
        {
            Messages = [.. CurrentConversation.Messages, pending],
        };

        NotifyStateChanged();
    }

    /// <summary>
    /// Marks a generation as starting and clears any leftover streaming text from a previous
    /// turn. Called synchronously, before the first await in the caller, so the composer
    /// disables the instant Send is clicked.
    /// </summary>
    public void BeginGeneration()
    {
        IsGenerating = true;
        StreamingText = string.Empty;

        NotifyStateChanged();
    }

    /// <summary>
    /// Appends a batch of streamed text to the in-progress assistant reply.
    /// </summary>
    public void AppendStreamingText(string delta)
    {
        StreamingText += delta;

        NotifyStateChanged();
    }

    /// <summary>
    /// Ends a generation and swaps in the freshly refetched conversation in a single notification.
    /// </summary>
    /// <remarks>
    /// Atomicity here is load-bearing: setting <see cref="CurrentConversation"/> and clearing
    /// <see cref="IsGenerating"/>/<see cref="StreamingText"/> as two separate notifications would
    /// render an intermediate frame showing either both the persisted message and the leftover
    /// streaming bubble (duplicate flash) or neither (blank flash).
    /// </remarks>
    public void CompleteGeneration(GetConversationResponse conversation)
    {
        CurrentConversation = conversation;
        StreamingText = string.Empty;
        IsGenerating = false;

        NotifyStateChanged();
    }

    /// <summary>
    /// Clears streaming state without touching <see cref="CurrentConversation"/>, for when the
    /// post-stream refetch itself fails and there is nothing new to show.
    /// </summary>
    public void EndGeneration()
    {
        StreamingText = string.Empty;
        IsGenerating = false;

        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}