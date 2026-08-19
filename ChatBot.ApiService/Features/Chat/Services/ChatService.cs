using ChatBot.Api.AI.Prompting.Contracts;
using ChatBot.Api.AI.Prompts;
using ChatBot.Api.AI.Prompts.Contracts;
using ChatBot.Api.Common.Errors;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Features.Chat.Contracts;
using ChatBot.Api.Features.Chat.Models;
using ChatBot.Api.Features.Conversations.Contracts;
using ChatBot.Api.Features.Conversations.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBot.Api.Features.Chat.Services
{
    public sealed class ChatService(
         IConversationService conversationService,
        IConversationBuilder conversationBuilder,
        IChatProviderFactory providerFactory,
        IConversationMetadataService metadataService,
        ITokenManager tokenManager,
        IChatContextManager contextManager,
        TimeProvider timeProvider)
        : IChatService
    {
        public async Task<ChatResponse> SendAsync(
            ChatRequest request,
            CancellationToken cancellationToken = default)
        {
            var conversation = await conversationService.GetRequiredAsync(
                request.ConversationId,
                cancellationToken);

            var now = timeProvider.GetUtcNow();

            var userMessage = new ConversationMessage(
        conversation.Id,
        ChatRole.User,
        request.UserMessage,
        now);

            var promptMessages = await conversationBuilder.BuildAsync(
         PromptNames.Chat,
         conversation.Id,
         new List<ConversationMessage>(conversation.Messages) { userMessage },
         cancellationToken);

            // May throw ConversationContextTooLargeException — nothing has been mutated or
            // persisted yet, so a rejection here leaves the conversation untouched.
            var context = contextManager.SelectContext(promptMessages, request.Model);

            conversation.AddMessage(
                userMessage,
                now);

            var provider = providerFactory.GetProvider(request.Model);

            var assistantResponse = await provider.SendAsync(
                request.Model,
                context,
                cancellationToken);

            var tokenUsage = await tokenManager.CalculateAsync(
                context,
                assistantResponse,
                request.Model,
                cancellationToken);

            var assistantMessage = new ConversationMessage(
                 conversation.Id,
                 ChatRole.Assistant,
                 assistantResponse,
                 timeProvider.GetUtcNow(),
                 tokenUsage);

            conversation.AddMessage(
                assistantMessage,
                assistantMessage.CreatedAt);

            metadataService.ApplyPreview(conversation, assistantMessage);

            await conversationService.SaveAsync(
                conversation,
                cancellationToken);

            await metadataService.ScheduleTitleGenerationAsync(
                conversation,
                request.Model,
                cancellationToken);

            return new ChatResponse(
                assistantMessage.Content,
                assistantMessage.CreatedAt,
                tokenUsage);
        }

        public async Task<ChatStreamContext> PrepareStreamAsync(
            ChatRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var conversation = await conversationService.GetRequiredAsync(
                request.ConversationId,
                cancellationToken);

            var now = timeProvider.GetUtcNow();

            var userMessage = new ConversationMessage(
        conversation.Id,
        ChatRole.User,
        request.UserMessage,
        now);

            var history = new List<ConversationMessage>(conversation.Messages)
            {
                userMessage
            };

            var promptHistory = await conversationBuilder.BuildAsync(
         PromptNames.Chat,
         conversation.Id,
         history,
         cancellationToken);

            // A plain async method, not an iterator: a rejection here (e.g.
            // ConversationContextTooLargeException) is eagerly observable by the caller —
            // in particular, by StreamMessageEndpoint awaiting this before it starts the SSE
            // response — so it can still become a normal 400 ProblemDetails.
            var context = contextManager.SelectContext(promptHistory, request.Model);

            return new ChatStreamContext(conversation, userMessage, request.Model, context);
        }

        public async IAsyncEnumerable<ChatStreamChunk> StreamAsync(ChatStreamContext preparedContext, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(preparedContext);

            var conversation = preparedContext.Conversation;
            var userMessage = preparedContext.UserMessage;
            var model = preparedContext.Model;
            var context = preparedContext.PromptContext;

            var provider = providerFactory.GetProvider(model);

            var responseBuilder = new StringBuilder(1024);
            ChatStreamChunk? completionChunk = null;
            string? providerFailureMessage = null;
            var persistAttempted = false;

            try
            {
                // yield return cannot appear inside a try block that has a catch, so
                // MoveNextAsync (where the provider call actually happens) is isolated in its
                // own try/catch with no yield, and the yield stays in the try/finally below
                // that only disposes the enumerator. Same split as ChatStreamMapper uses.
                var enumerator = provider.StreamAsync(
                    model,
                    context,
                    cancellationToken).GetAsyncEnumerator(cancellationToken);

                try
                {
                    while (true)
                    {
                        ChatStreamChunk chunk;

                        try
                        {
                            if (!await enumerator.MoveNextAsync())
                            {
                                break;
                            }

                            chunk = enumerator.Current;
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (ChatProviderException ex)
                        {
                            // A failed generation must not look like a normal completion:
                            // stop pulling from the provider and fall through to persist +
                            // report the error below, instead of yielding what came before
                            // as if it were the whole answer.
                            providerFailureMessage = ex.Message;
                            break;
                        }

                        if (!string.IsNullOrEmpty(chunk.Text))
                        {
                            responseBuilder.Append(chunk.Text);
                        }

                        if (chunk.IsCompleted)
                        {
                            // Hold the completion signal back: the client treats it as
                            // "this turn is persisted and safe to re-fetch," so it must
                            // not reach the wire before SaveChangesAsync below returns.
                            completionChunk = chunk;
                            continue;
                        }

                        yield return chunk;
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }

                persistAttempted = true;
                await PersistConversationTurnAsync(
                    conversation,
                    userMessage,
                    model,
                    responseBuilder.ToString(),
                    context,
                    isPartial: providerFailureMessage is not null);

                if (providerFailureMessage is not null)
                {
                    // Once the SSE response has started, a normal ProblemDetails response
                    // can no longer replace it — this is the only way left to tell the
                    // client the generation failed.
                    yield return ChatStreamChunk.Error(providerFailureMessage);
                }
                else if (completionChunk is not null)
                {
                    yield return completionChunk;
                }
            }
            finally
            {
                // Reached on cancellation, or on an exception before the normal
                // persistence point above. Persist whatever was generated so far,
                // exactly once.
                if (!persistAttempted)
                {
                    // Reached via cancellation or an exception before the normal completion
                    // path above ran — this turn never reached a genuine completion, so
                    // whatever text was generated is always partial.
                    await PersistConversationTurnAsync(
                        conversation,
                        userMessage,
                        model,
                        responseBuilder.ToString(),
                        context,
                        isPartial: true);
                }
            }
        }
        private async Task PersistConversationTurnAsync(
        Conversation conversation,
        ConversationMessage userMessage,
        string model,
        string assistantResponse,
        IReadOnlyCollection<ConversationMessage> context,
        bool isPartial = false)
        {
            // Persist even if the HTTP request has already been cancelled.
            var persistenceCancellation = CancellationToken.None;

            // The user's message is added unconditionally, even when the client cancelled before
            // a single token arrived: the UI shows the user's message optimistically the moment it
            // is sent, and an immediate cancel must not make it vanish on the post-stream refetch.
            conversation.AddMessage(
                userMessage,
                userMessage.CreatedAt);

            if (!string.IsNullOrWhiteSpace(assistantResponse))
            {
                var tokenUsage = await tokenManager.CalculateAsync(
                    context,
                    assistantResponse,
                    model,
                    persistenceCancellation);

                var assistantMessage = new ConversationMessage(
        conversation.Id,
        ChatRole.Assistant,
        assistantResponse,
        timeProvider.GetUtcNow(),
        tokenUsage,
        isPartial);

                conversation.AddMessage(
                    assistantMessage,
                    assistantMessage.CreatedAt);

                metadataService.ApplyPreview(conversation, assistantMessage);
            }

            await conversationService.SaveAsync(
                conversation,
                persistenceCancellation);

            if (!string.IsNullOrWhiteSpace(assistantResponse))
            {
                await metadataService.ScheduleTitleGenerationAsync(
                    conversation,
                    model,
                    persistenceCancellation);
            }
        }
    }
}