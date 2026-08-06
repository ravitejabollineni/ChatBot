using ChatBot.Api.AI.Prompts.Models;

namespace ChatBot.Api.AI.Prompts.Contracts;

public interface IPromptParser
{
    PromptDefinition Parse(string rawPrompt);
}