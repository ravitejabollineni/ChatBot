using ChatBot.Api.AI.Prompts.Models;

namespace ChatBot.Api.AI.Prompts.Contracts
{
    public interface IPromptRepository
    {
        Task<PromptDefinition> GetAsync(
            string promptName,
            CancellationToken cancellationToken = default);
    }
}
