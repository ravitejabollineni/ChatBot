using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatBot.Web.Features.Chat.Rendering;

public static partial class ChatMarkdownRenderer
{
    [GeneratedRegex(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\s*[-*]\s+(.+)$", RegexOptions.Compiled)]
    private static partial Regex ListItemRegex();

    [GeneratedRegex(@"`([^`\n]+)`", RegexOptions.Compiled)]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"\[(.+?)\]\((.+?)\)", RegexOptions.Compiled)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\*\*(.+?)\*\*", RegexOptions.Compiled)]
    private static partial Regex BoldAsteriskRegex();

    [GeneratedRegex(@"__(.+?)__", RegexOptions.Compiled)]
    private static partial Regex BoldUnderscoreRegex();

    [GeneratedRegex(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", RegexOptions.Compiled)]
    private static partial Regex ItalicAsteriskRegex();

    [GeneratedRegex(@"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)", RegexOptions.Compiled)]
    private static partial Regex ItalicUnderscoreRegex();

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var html = new StringBuilder();
        var paragraphLines = new List<string>();
        var listItems = new List<string>();
        var codeLines = new List<string>();

        var inCodeBlock = false;
        string? codeLanguage = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd();

            if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
            {
                if (inCodeBlock)
                {
                    AppendCodeBlock(html, codeLines, codeLanguage);
                    codeLines.Clear();
                    codeLanguage = null;
                    inCodeBlock = false;
                }
                else
                {
                    FlushParagraph(html, paragraphLines);
                    FlushList(html, listItems);
                    inCodeBlock = true;
                    codeLanguage = trimmedLine["```".Length..].Trim();
                }

                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(line);
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                FlushParagraph(html, paragraphLines);
                FlushList(html, listItems);
                continue;
            }

            var headingMatch = HeadingRegex().Match(trimmedLine);
            if (headingMatch.Success)
            {
                FlushParagraph(html, paragraphLines);
                FlushList(html, listItems);

                var level = headingMatch.Groups[1].Value.Length;
                var content = ProcessInline(headingMatch.Groups[2].Value.Trim());
                html.Append($"<h{level}>{content}</h{level}>");
                continue;
            }

            var listMatch = ListItemRegex().Match(trimmedLine);
            if (listMatch.Success)
            {
                FlushParagraph(html, paragraphLines);
                listItems.Add(listMatch.Groups[1].Value.Trim());
                continue;
            }

            if (listItems.Count > 0)
            {
                FlushList(html, listItems);
            }

            paragraphLines.Add(trimmedLine);
        }

        if (inCodeBlock)
        {
            AppendCodeBlock(html, codeLines, codeLanguage);
        }

        FlushParagraph(html, paragraphLines);
        FlushList(html, listItems);

        return html.ToString();
    }

    private static void FlushParagraph(StringBuilder html, List<string> paragraphLines)
    {
        if (paragraphLines.Count == 0)
            return;

        html.Append("<p>");
        html.Append(string.Join("<br />", paragraphLines.Select(ProcessInline)));
        html.Append("</p>");
        paragraphLines.Clear();
    }

    private static void FlushList(StringBuilder html, List<string> listItems)
    {
        if (listItems.Count == 0)
            return;

        html.Append("<ul>");

        foreach (var item in listItems)
        {
            html.Append("<li>");
            html.Append(ProcessInline(item));
            html.Append("</li>");
        }

        html.Append("</ul>");
        listItems.Clear();
    }

    private static void AppendCodeBlock(
        StringBuilder html,
        List<string> codeLines,
        string? codeLanguage)
    {
        var encodedCode = WebUtility.HtmlEncode(string.Join('\n', codeLines));
        var languageClass = string.IsNullOrWhiteSpace(codeLanguage)
            ? string.Empty
            : $" class=\"language-{WebUtility.HtmlEncode(codeLanguage.Trim())}\"";

        html.Append($"<pre><code{languageClass}>{encodedCode}</code></pre>");
    }

    private static string ProcessInline(string text)
    {
        var encoded = WebUtility.HtmlEncode(text);
        var placeholders = new Dictionary<string, string>();
        var placeholderIndex = 0;

        encoded = InlineCodeRegex().Replace(encoded, match =>
        {
            var token = CreatePlaceholderToken(ref placeholderIndex);
            placeholders[token] = $"<code>{match.Groups[1].Value}</code>";
            return token;
        });

        encoded = LinkRegex().Replace(encoded, match =>
        {
            var href = WebUtility.HtmlDecode(match.Groups[2].Value.Trim());
            if (!TryGetSafeHref(href, out var safeHref))
            {
                return match.Value;
            }

            var linkText = match.Groups[1].Value;
            return $"<a href=\"{WebUtility.HtmlEncode(safeHref)}\" target=\"_blank\" rel=\"noopener noreferrer\">{linkText}</a>";
        });

        encoded = BoldAsteriskRegex().Replace(encoded, "<strong>$1</strong>");
        encoded = BoldUnderscoreRegex().Replace(encoded, "<strong>$1</strong>");
        encoded = ItalicAsteriskRegex().Replace(encoded, "<em>$1</em>");
        encoded = ItalicUnderscoreRegex().Replace(encoded, "<em>$1</em>");

        foreach (var placeholder in placeholders)
        {
            encoded = encoded.Replace(placeholder.Key, placeholder.Value, StringComparison.Ordinal);
        }

        return encoded;
    }

    private static string CreatePlaceholderToken(ref int placeholderIndex)
    {
        return $"\u0000md{placeholderIndex++}\u0000";
    }

    private static bool TryGetSafeHref(string href, out string safeHref)
    {
        safeHref = string.Empty;

        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https" or "mailto"))
        {
            return false;
        }

        safeHref = uri.ToString();
        return true;
    }
}
