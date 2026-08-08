using ChatBot.Api.AI.Prompting.Contracts;
using ChatBot.Api.AI.Prompts;
using ChatBot.Api.AI.Prompts.Contracts;
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

            conversation.AddMessage(
                userMessage,
                now);

            var history = await conversationBuilder.BuildAsync(
         PromptNames.Chat,
         conversation.Id,
         conversation.Messages,
         cancellationToken);

            var provider = providerFactory.GetProvider(request.Model);

            var assistantResponse = await provider.SendAsync(
                request.Model,
                history,
                cancellationToken);

            var tokenUsage = await tokenManager.CalculateAsync(
                conversation.Messages,
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

        public async IAsyncEnumerable<ChatStreamChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

            var provider = providerFactory.GetProvider(request.Model);

            var responseBuilder = new StringBuilder(1024);

            try
            {
                await foreach (var chunk in provider.StreamAsync(
                    request.Model,
                    promptHistory,
                    cancellationToken))
                {
                    if (!string.IsNullOrEmpty(chunk.Text))
                    {
                        responseBuilder.Append(chunk.Text);
                    }

                    yield return chunk;
                }
            }
            finally
            {
                await PersistConversationTurnAsync(
                    conversation,
                    userMessage,
                    request.Model,
                    responseBuilder.ToString());
            }
        }
        private async Task PersistConversationTurnAsync(
        Conversation conversation,
        ConversationMessage userMessage,
        string model,
        string assistantResponse)
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
                    conversation.Messages,
                    assistantResponse,
                    model,
                    persistenceCancellation);

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