using System.Globalization;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace ChatBot.Web.Features.Chat.Rendering;

/// <summary>
/// Replaces Markdig's default fenced-code-block HTML with a VS Code/GitHub-style wrapper:
/// a header row (language badge + copy button) around a highlight.js-ready
/// &lt;pre&gt;&lt;code class="language-xxx hljs"&gt;.
///
/// Indented code blocks (obj is CodeBlock but not FencedCodeBlock) fall back to Markdig's
/// stock rendering — LLM chat output is fenced almost universally, so a plain, unheadered
/// &lt;pre&gt;&lt;code&gt; for the rare indented block is an acceptable gap.
/// </summary>
public sealed class HighlightedCodeBlockRenderer : CodeBlockRenderer
{
    private static readonly Dictionary<string, (string Css, string Display)> KnownLanguages =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["csharp"] = ("csharp", "C#"),
            ["cs"] = ("csharp", "C#"),
            ["c#"] = ("csharp", "C#"),
            ["js"] = ("javascript", "JavaScript"),
            ["javascript"] = ("javascript", "JavaScript"),
            ["ts"] = ("typescript", "TypeScript"),
            ["typescript"] = ("typescript", "TypeScript"),
            ["py"] = ("python", "Python"),
            ["python"] = ("python", "Python"),
            ["sh"] = ("bash", "Shell"),
            ["bash"] = ("bash", "Shell"),
            ["shell"] = ("bash", "Shell"),
            ["json"] = ("json", "JSON"),
            ["html"] = ("html", "HTML"),
            ["css"] = ("css", "CSS"),
            ["sql"] = ("sql", "SQL"),
            ["yaml"] = ("yaml", "YAML"),
            ["yml"] = ("yaml", "YAML"),
            ["xml"] = ("xml", "XML"),
            ["plaintext"] = ("plaintext", "Text"),
            ["text"] = ("plaintext", "Text"),
        };

    protected override void Write(HtmlRenderer renderer, CodeBlock obj)
    {
        if (obj is not FencedCodeBlock fencedCodeBlock)
        {
            base.Write(renderer, obj);
            return;
        }

        renderer.EnsureLine();

        var (cssLanguage, displayName) = ResolveLanguage(fencedCodeBlock.Info);

        renderer.Write("<div class=\"code-block\" data-lang=\"");
        renderer.WriteEscape(cssLanguage);
        renderer.WriteLine("\">");

        renderer.Write("<div class=\"code-block__header\"><span class=\"code-block__lang\">");
        renderer.WriteEscape(displayName);
        renderer.Write("</span><button type=\"button\" class=\"code-block__copy\" aria-label=\"Copy code\">");
        renderer.Write("<i class=\"bi bi-clipboard\"></i><span>Copy</span></button></div>");
        renderer.WriteLine();

        renderer.Write("<pre><code class=\"language-");
        renderer.WriteEscape(cssLanguage);
        renderer.Write(" hljs\">");
        // writeEndOfLines: true, escape: true, softEscape: false — matches Markdig's own
        // default CodeBlockRenderer behavior for escaping fenced code content.
        renderer.WriteLeafRawLines(obj, writeEndOfLines: true, escape: true, softEscape: false);
        renderer.WriteLine("</code></pre>");

        renderer.WriteLine("</div>");
        renderer.EnsureLine();
    }

    private static (string Css, string Display) ResolveLanguage(string? info)
    {
        // Fenced info strings can carry trailing metadata after the language token
        // (e.g. "csharp {highlight-lines}"); only the first word identifies the language.
        var token = info?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        if (token.Length == 0)
        {
            return KnownLanguages["plaintext"];
        }

        if (KnownLanguages.TryGetValue(token, out var known))
        {
            return known;
        }

        // Unknown language: pass a sanitized version of the raw token through as the
        // hljs/CSS class (best-effort client-side auto-detection) and title-case it for
        // the badge. WriteEscape() above still HTML-escapes displayName regardless —
        // fenced info strings are attacker-controlled (arrive verbatim in message content).
        var css = SanitizeCssToken(token);
        var display = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(token.ToLowerInvariant());
        return (css, display);
    }

    private static string SanitizeCssToken(string token)
    {
        var buffer = new char[token.Length];
        var written = 0;

        foreach (var c in token)
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_')
            {
                buffer[written++] = char.ToLowerInvariant(c);
            }
        }

        return written == 0 ? "plaintext" : new string(buffer, 0, written);
    }
}
