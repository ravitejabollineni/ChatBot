using System.Text;
using System.Text.RegularExpressions;

namespace ChatBot.Api.Common.Text;

/// <summary>
/// Converts Markdown text into a single-line plain-text snippet, for use as a sidebar preview.
/// Dependency-free by design — the API project has no Markdown rendering needs beyond this.
/// </summary>
public static partial class MarkdownPlainTextConverter
{
    public static string ToPreview(string markdown, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var text = StripFencedCodeBlocks(markdown);
        text = StripLineLevelMarkup(text);
        text = StripInlineMarkup(text);
        text = CollapseTableRows(text);

        return CollapseAndTruncate(text, maxLength);
    }

    private static string StripFencedCodeBlocks(string text) =>
        FencedCodeBlockRegex().Replace(text, " ");

    private static string StripLineLevelMarkup(string text)
    {
        var lines = text.Split('\n');
        var builder = new StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = HeadingPrefixRegex().Replace(rawLine, string.Empty);
            line = BlockquotePrefixRegex().Replace(line, string.Empty);
            line = BulletPrefixRegex().Replace(line, string.Empty);
            line = NumberedBulletPrefixRegex().Replace(line, string.Empty);

            if (HorizontalRuleRegex().IsMatch(line.Trim()))
            {
                continue;
            }

            builder.Append(line).Append('\n');
        }

        return builder.ToString();
    }

    private static string StripInlineMarkup(string text)
    {
        text = ImageRegex().Replace(text, string.Empty);
        text = LinkRegex().Replace(text, "$1");

        // Looped rather than a single pass: nested emphasis (e.g. "**bold *italic* text**")
        // only has its outer markers consumed by one pass, leaving the inner markers behind.
        string beforePass;
        do
        {
            beforePass = text;
            text = BoldItalicRegex().Replace(text, "$2");
        }
        while (text != beforePass);

        text = InlineCodeRegex().Replace(text, "$1");
        text = HtmlTagRegex().Replace(text, string.Empty);

        return text;
    }

    private static string CollapseTableRows(string text)
    {
        var lines = text.Split('\n');
        var builder = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith('|') && trimmed.EndsWith('|'))
            {
                if (TableSeparatorRowRegex().IsMatch(trimmed))
                {
                    continue;
                }

                var cells = trimmed
                    .Trim('|')
                    .Split('|')
                    .Select(cell => cell.Trim())
                    .Where(cell => cell.Length > 0);

                builder.Append(string.Join(' ', cells)).Append('\n');
            }
            else
            {
                builder.Append(line).Append('\n');
            }
        }

        return builder.ToString();
    }

    private static string CollapseAndTruncate(string text, int maxLength)
    {
        var collapsed = string.Join(
            ' ',
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (collapsed.Length <= maxLength)
        {
            return collapsed;
        }

        var truncated = collapsed[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');

        if (lastSpace > 0)
        {
            truncated = truncated[..lastSpace];
        }

        return $"{truncated}…";
    }

    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Multiline)]
    private static partial Regex FencedCodeBlockRegex();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+")]
    private static partial Regex HeadingPrefixRegex();

    [GeneratedRegex(@"^\s{0,3}>\s?")]
    private static partial Regex BlockquotePrefixRegex();

    [GeneratedRegex(@"^\s{0,3}[-*+]\s+")]
    private static partial Regex BulletPrefixRegex();

    [GeneratedRegex(@"^\s{0,3}\d+[.)]\s+")]
    private static partial Regex NumberedBulletPrefixRegex();

    [GeneratedRegex(@"^(?:-{3,}|\*{3,}|_{3,})$")]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]*\)")]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"(\*\*\*|___|\*\*|__|\*|_)(.+?)\1")]
    private static partial Regex BoldItalicRegex();

    [GeneratedRegex(@"`([^`]*)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"^\|?\s*:?-+:?\s*(\|\s*:?-+:?\s*)*\|?$")]
    private static partial Regex TableSeparatorRowRegex();
}
