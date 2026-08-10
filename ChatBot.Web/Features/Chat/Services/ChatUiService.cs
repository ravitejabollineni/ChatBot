using System.Text;
using ChatBot.Web.Contracts.Chat;
using ChatBot.Web.Features.Chat.Contracts.Conversation;
using ChatBot.Web.Features.Chat.Services.Api;
using ChatBot.Web.Features.Chat.State;
using Refit;

namespace ChatBot.Web.Features.Chat.Services;

public sealed class ChatUiService(
    IConversationApi conversationApi,
    IChatApi chatApi,
    ChatStreamClient chatStreamClient,
    ChatState chatState) : IDisposable
{
    // Matches ChatBot.Api's ConversationDefaults.UntitledTitle. Duplicated rather than shared
    // across the process boundary — Web only ever sees this string over the wire, the same as
    // any other DTO value.
    private const string UntitledConversationTitle = "New Conversation";

    // Local/slow models can take tens of seconds to generate a title, especially for a long
    // first exchange (the whole first user + assistant message is re-sent as context) — 60s
    // total comfortably covers that instead of giving up while the server is still working.
    private static readonly TimeSpan TitlePollInterval = TimeSpan.FromSeconds(3);
    private const int TitlePollMaxAttempts = 20;

    // Owns the lifetime of the in-flight stream's cancellation. Cancelling it aborts the
    // underlying HTTP connection (see ChatStreamClient), which is the only way to actually
    // stop a running completion — there's nothing to "pause" server-side.
    private CancellationTokenSource? streamCts;

    // Guards against piling up duplicate pollers for the same conversation — e.g. a second
    // message sent in the same conversation before the first turn's title poll has finished.
    private readonly HashSet<Guid> titlePollsInFlight = [];

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

        await LoadAvailableModelsAsync();
    }

    /// <summary>
    /// Failure here is isolated from <see cref="InitializeAsync"/>'s conversation load: a
    /// broken models endpoint shouldn't also block the conversation list from rendering, so
    /// this leaves <see cref="ChatState"/>'s hardcoded fallback model in place rather than
    /// surfacing <see cref="ChatState.SetError"/> and clobbering whatever that call already set.
    /// </summary>
    private async Task LoadAvailableModelsAsync()
    {
        try
        {
            var response = await chatApi.GetModelsAsync();

            chatState.SetAvailableModels(response.Models, response.DefaultModel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to load available models: {ex.Message}");
        }
    }

    public async Task CreateConversationAsync()
    {
        try
        {
            var response =
                await conversationApi.CreateConversationAsync();

            await LoadConversationAsync(response.ConversationId);

            if (chatState.CurrentConversation is not null)
            {
                chatState.UpsertConversationSummary(ToSummary(chatState.CurrentConversation));
            }

            chatState.SetError(null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to create a conversation: {ex.Message}");
            chatState.SetError("Unable to start a new chat. Please try again.");
        }
    }

    /// <summary>
    /// Resets to a fresh, unsaved conversation. Purely client-side and synchronous-safe to call
    /// at any time, including mid-stream — cancelling first means the abandoned stream's eventual
    /// <c>finally</c> in <see cref="SendMessageStreamingAsync"/> is guarded by its own bound
    /// conversation id and can't clobber the state this resets.
    /// </summary>
    public void StartNewChat()
    {
        CancelGeneration();
        chatState.StartNewConversation();
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

        // Shown before the request goes out, not after it returns: the reply can take tens of
        // seconds and the message needs to be visible for that whole time.
        chatState.AppendPendingUserMessage(message);

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
        catch (ApiException ex)
        {
            // ex.Content is the raw ProblemDetails body the API's GlobalExceptionHandler
            // returned; ex.Message is just Refit's generic "status code does not indicate
            // success" text, so the ProblemDetails Title (already safe to show verbatim) is
            // preferred whenever the body parses.
            var problem = ApiProblemDetailsReader.TryRead(ex.Content);

            Console.WriteLine($"Unable to send message: {(int)ex.StatusCode} {problem?.Title ?? ex.Message}");
            chatState.SetError(problem?.Title ?? "Unable to send that message. Please try again.");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to send message: {ex.Message}");
            chatState.SetError("Unable to send that message. Please try again.");
            throw;
        }
    }

    /// <summary>
    /// Streams the assistant's reply token-by-token instead of waiting for the full completion.
    /// </summary>
    /// <remarks>
    /// All outcomes — success, cancel, and mid-stream failure — flow through the same
    /// <c>finally</c> refetch, which is what recovers the server's persisted partial text after
    /// a cancel. Unlike <see cref="SendMessageAsync"/>, exceptions are never rethrown: every
    /// outcome here is expressed through <see cref="ChatState"/> instead.
    /// </remarks>
    public async Task SendMessageStreamingAsync(
        string model,
        string message)
    {
        if (chatState.SelectedConversationId is null)
            return;

        // A fast double Enter/double-click can queue a second call before the composer's
        // disabled attribute round-trips back to the client. IsGenerating is set synchronously
        // below, so this guard only ever catches that queued duplicate.
        if (chatState.IsGenerating)
            return;

        // Bound at entry, not read again below: a mid-stream New Chat swaps out
        // SelectedConversationId on ChatState, but this stream's own finally must only ever
        // act on the conversation it actually started for.
        var boundConversationId = chatState.SelectedConversationId.Value;

        chatState.AppendPendingUserMessage(message);
        chatState.SetError(null);
        chatState.BeginGeneration();

        // Captured into a local: cancelling and immediately starting a new stream would
        // otherwise race this call's own cleanup (below) against the new call's freshly
        // assigned streamCts field.
        var localCts = new CancellationTokenSource();
        streamCts = localCts;
        var cancellationToken = localCts.Token;

        // Deltas are buffered and only flushed to ChatState on a ~75ms cadence (or immediately
        // on the final chunk). Every flush re-renders all three ChatState subscribers, so
        // bounding notify frequency here is the whole point — flushing every token would mean
        // a render per token.
        var pending = new StringBuilder();
        var lastFlush = DateTime.UtcNow;

        // Set when the server reports a provider failure via an in-band SSE error chunk
        // (see ChatStreamChunk.IsError) rather than aborting the connection. Not an
        // exception, so it flows through the same success-path cleanup below instead of
        // the catch blocks.
        string? streamErrorMessage = null;

        try
        {
            await foreach (var chunk in chatStreamClient.StreamMessageAsync(
                new SendMessageRequest(
                    boundConversationId,
                    model,
                    message),
                cancellationToken))
            {
                if (chunk.IsError)
                {
                    streamErrorMessage = chunk.ErrorMessage;
                }

                if (!string.IsNullOrEmpty(chunk.Text))
                {
                    pending.Append(chunk.Text);
                }

                var dueForFlush = (DateTime.UtcNow - lastFlush).TotalMilliseconds >= 75;

                if (pending.Length > 0 && (chunk.IsCompleted || dueForFlush))
                {
                    chatState.AppendStreamingText(pending.ToString());
                    pending.Clear();
                    lastFlush = DateTime.UtcNow;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected: the user clicked Cancel. Not an error.
        }
        catch (HttpRequestException ex)
        {
            // Only ChatStreamClient's pre-stream status check (a ProblemDetails response
            // received before any SSE bytes went out) throws this, with ex.Message already set
            // to the ProblemDetails Title. A break after the 200 response has started surfaces
            // as a different exception from the SSE parser and falls through to the generic
            // catch below - there is no ProblemDetails body to read at that point.
            Console.WriteLine($"Unable to stream message: {(int?)ex.StatusCode} {ex.Message}");
            chatState.SetError(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to stream message: {ex.Message}");
            chatState.SetError("The response was interrupted. Please try again.");
        }
        finally
        {
            if (ReferenceEquals(streamCts, localCts))
            {
                streamCts = null;
            }

            localCts.Dispose();

            // A New Chat click during this stream already reset ChatState to a different (or no)
            // conversation — that reset must not be clobbered by this now-abandoned stream's
            // refetch, and the abandoned stream has no bearing on the newly selected one either.
            if (chatState.SelectedConversationId == boundConversationId)
            {
                if (pending.Length > 0)
                {
                    chatState.AppendStreamingText(pending.ToString());
                }

                try
                {
                    var conversation = await conversationApi.GetConversationAsync(boundConversationId);

                    chatState.CompleteGeneration(conversation);
                    chatState.UpsertConversationSummary(ToSummary(conversation));

                    if (streamErrorMessage is not null)
                    {
                        Console.WriteLine($"Streaming failed: {streamErrorMessage}");
                        chatState.SetError("The response was interrupted. Please try again.");
                    }

                    // Title generation is scheduled server-side but runs detached, well after
                    // this response — there's no push channel back, so if it hasn't landed yet
                    // this polls for it separately. Deliberately fire-and-forget: it must not
                    // delay this method's return, and it never touches IsGenerating.
                    if (string.Equals(conversation.Title, UntitledConversationTitle, StringComparison.Ordinal))
                    {
                        _ = PollForGeneratedTitleAsync(boundConversationId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to refresh conversation after streaming: {ex.Message}");
                    chatState.EndGeneration();
                    chatState.SetError("Unable to refresh the conversation. Please try again.");
                }
            }
        }
    }

    /// <summary>
    /// Resends the last user message as a new turn. There is no delete/replace-message endpoint,
    /// so this appends rather than replaces — the honest behavior given the current API surface.
    /// </summary>
    public async Task RegenerateLastResponseAsync(string model)
    {
        if (chatState.IsGenerating)
            return;

        var lastUserMessage = chatState.CurrentConversation?.Messages
            .LastOrDefault(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase));

        if (lastUserMessage is null)
            return;

        await SendMessageStreamingAsync(model, lastUserMessage.Content);
    }

    /// <summary>
    /// Stops a running generation. Only cancels the token — the running loop in
    /// <see cref="SendMessageStreamingAsync"/> remains the sole owner of state transitions, so
    /// there is no race between this and the loop's own cleanup.
    /// </summary>
    public void CancelGeneration()
    {
        try
        {
            streamCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The stream already finished and disposed the token source between our null-check
            // and the Cancel() call; nothing left to cancel.
        }
    }

    /// <summary>
    /// Cancels any in-flight stream. The service is scoped to the circuit, so an abandoned
    /// circuit (browser closed mid-stream) would otherwise leave the loop reading forever.
    /// </summary>
    public void Dispose()
    {
        CancelGeneration();
    }

    /// <summary>
    /// Polls periodically, for up to <see cref="TitlePollMaxAttempts"/> * <see cref="TitlePollInterval"/>,
    /// for <paramref name="conversationId"/>'s title to change away from the placeholder. Updates only the title on
    /// <see cref="ChatState"/> — never the preview, never any loading flag — and gives up
    /// silently once <see cref="TitlePollMaxAttempts"/> is reached, which is the expected
    /// outcome for a trivial first exchange that was never eligible for title generation.
    /// </summary>
    private async Task PollForGeneratedTitleAsync(Guid conversationId)
    {
        if (!titlePollsInFlight.Add(conversationId))
        {
            return;
        }

        try
        {
            for (var attempt = 0; attempt < TitlePollMaxAttempts; attempt++)
            {
                await Task.Delay(TitlePollInterval);

                GetConversationResponse conversation;

                try
                {
                    conversation = await conversationApi.GetConversationAsync(conversationId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to poll for generated title: {ex.Message}");
                    return;
                }

                if (!string.Equals(conversation.Title, UntitledConversationTitle, StringComparison.Ordinal))
                {
                    chatState.UpdateConversationTitle(conversationId, conversation.Title);
                    return;
                }
            }
        }
        finally
        {
            titlePollsInFlight.Remove(conversationId);
        }
    }

    private static ConversationSummaryResponse ToSummary(GetConversationResponse conversation) =>
        new(
            conversation.ConversationId,
            conversation.CreatedAt,
            conversation.LastUpdatedAt,
            conversation.Title,
            conversation.Preview);
}