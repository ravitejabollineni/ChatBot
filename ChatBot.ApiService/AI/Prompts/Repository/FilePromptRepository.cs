using ChatBot.Api.AI.Prompts.Contracts;
using ChatBot.Api.AI.Prompts.Models;
using Microsoft.Extensions.Hosting;

namespace ChatBot.Api.AI.Prompts.Repository;

public sealed class FilePromptRepository(
    IHostEnvironment environment,
    IPromptParser parser)
    : IPromptRepository
{
    private const string PromptFolder = "AI/Prompts/Templates";

    public async Task<PromptDefinition> GetAsync(
        string promptName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptName);
        string path = GetPromptPath(promptName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Prompt '{promptName}' was not found.",
                path);
        }

        var rawPrompt = await File.ReadAllTextAsync(
            path,
            cancellationToken);

        return parser.Parse(rawPrompt);
    }

    private string GetPromptPath(string promptName)
    {
        return Path.Combine(
            environment.ContentRootPath,
            PromptFolder,
            $"{promptName}.md");
    }
}