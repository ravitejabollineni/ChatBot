using ChatBot.Api.AI.Configuration;
using ChatBot.Api.Common.Errors;
using ChatBot.Api.Domain.Entities;
using ChatBot.Api.Domain.Enums;
using ChatBot.Api.Features.Chat.Contracts;
using Microsoft.Extensions.Options;

namespace ChatBot.Api.AI.ContextManagement;

/// <summary>
/// Stateless: every dependency (<see cref="ITokenManager"/>, <see cref="IOptions{TOptions}"/>)
/// is itself thread-safe, and no field state is held across calls, so this is safe to
/// register as a singleton and share across concurrent requests.
/// </summary>
public sealed class ChatContextManager(
    ITokenManager tokenManager,
    IOptions<AiOptions> aiOptions)
    : IChatContextManager
{
    public IReadOnlyList<ConversationMessage> SelectContext(
        IReadOnlyCollection<ConversationMessage> messages,
        string model)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        if (messages.Count == 0)
        {
            throw new ArgumentException(
                "messages must contain at least the current user message.",
                nameof(messages));
        }

        var ordered = messages as IReadOnlyList<ConversationMessage> ?? messages.ToList();

        var hasSystemMessage = ordered[0].Role == ChatRole.System;
        var systemMessage = hasSystemMessage ? ordered[0] : null;
        var currentUserMessage = ordered[^1];

        var contextLimit = tokenManager.GetContextLimit(model);
        var outputReserve = aiOptions.Value.ContextManagement.OutputReserveTokens;
        var inputBudget = Math.Max(0, contextLimit - outputReserve);

        var protectedMessages = systemMessage is not null
            ? new[] { systemMessage, currentUserMessage }
            : new[] { currentUserMessage };

        var protectedTokens = tokenManager.EstimateTokenCount(protectedMessages);

        if (protectedTokens > inputBudget)
        {
            throw new ConversationContextTooLargeException(protectedTokens, inputBudget);
        }

        // Trimmable history: everything strictly between the system message and the
        // current user message.
        var middleStart = hasSystemMessage ? 1 : 0;
        var middleEnd = ordered.Count - 1; // exclusive of currentUserMessage

        if (middleEnd <= middleStart)
        {
            return systemMessage is not null
                ? [systemMessage, currentUserMessage]
                : [currentUserMessage];
        }

        // Group into whole turns. A turn starts at every User message; the first middle
        // message always starts a turn too, even if (defensively — shouldn't happen via
        // ConversationBuilder) it isn't a User message, so nothing is ever silently folded
        // into a turn that doesn't exist. A turn with no matching Assistant reply (e.g. a
        // prior request was cancelled before any text was produced, leaving a bare User
        // message in history) is still a valid, atomic 1-message turn under this rule.
        var turns = new List<List<ConversationMessage>>();

        for (var i = middleStart; i < middleEnd; i++)
        {
            var message = ordered[i];

            if (turns.Count == 0 || message.Role == ChatRole.User)
            {
                turns.Add([message]);
            }
            else
            {
                turns[^1].Add(message);
            }
        }

        var turnTokens = turns
            .Select(turn => tokenManager.EstimateTokenCount(turn))
            .ToList();

        var historyTokens = turnTokens.Sum();
        var firstKeptTurn = 0;

        // Terminates at-or-before firstKeptTurn == turns.Count: protectedTokens alone was
        // already proven <= inputBudget above, and historyTokens shrinks to 0 by then.
        while (firstKeptTurn < turns.Count
            && protectedTokens + historyTokens > inputBudget)
        {
            historyTokens -= turnTokens[firstKeptTurn];
            firstKeptTurn++;
        }

        var result = new List<ConversationMessage>();

        if (systemMessage is not null)
        {
            result.Add(systemMessage);
        }

        for (var i = firstKeptTurn; i < turns.Count; i++)
        {
            result.AddRange(turns[i]);
        }

        result.Add(currentUserMessage);

        return result;
    }
}
