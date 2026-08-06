using ChatBot.Api.AI.Prompts.Contracts;
using ChatBot.Api.AI.Prompts.Internal;
using ChatBot.Api.AI.Prompts.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ChatBot.Api.AI.Prompts.Parsing;

public sealed class MarkdownPromptParser(
    IDeserializer yamlDeserializer)
    : IPromptParser
{
    public PromptDefinition Parse(string rawPrompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawPrompt);

        var (yaml, markdown) = SplitFrontMatter(rawPrompt);

        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new InvalidOperationException("Prompt must begin with YAML front matter delimited by '---'.");
        }

        var frontMatter = yamlDeserializer.Deserialize<PromptFrontMatter>(yaml);
        if (!Version.TryParse(frontMatter.Version, out var version))
        {
            throw new InvalidOperationException(
                $"Invalid version '{frontMatter.Version}'.");
        }
        var metadata = new PromptMetadata(
            Name: frontMatter.Name,
            Version: version,
            Description: frontMatter.Description,
            Author: frontMatter.Author,
            Created: frontMatter.Created,
            Tags: frontMatter.Tags,
            Temperature: frontMatter.Temperature,
            MaxTokens: frontMatter.MaxTokens);

        return new PromptDefinition(
            Metadata: metadata,
            Content: markdown);
    }

    private static (string Yaml, string Markdown) SplitFrontMatter(string rawPrompt)
    {
        using var reader = new StringReader(rawPrompt);

        var firstLine = reader.ReadLine();

        if (!string.Equals(firstLine, "---", StringComparison.Ordinal))
        {
            return (string.Empty, rawPrompt);
        }

        var yamlBuilder = new System.Text.StringBuilder();

        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (line == "---")
            {
                break;
            }

            yamlBuilder.AppendLine(line);
        }

        var body = reader.ReadToEnd().TrimStart();

        return (yamlBuilder.ToString(), body);
    }
}