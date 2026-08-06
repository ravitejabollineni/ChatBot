using Markdig;
using Markdig.Renderers;

namespace ChatBot.Web.Features.Chat.Rendering;

/// <summary>
/// Registers <see cref="HighlightedCodeBlockRenderer"/> in place of Markdig's default
/// <see cref="Markdig.Renderers.Html.CodeBlockRenderer"/>.
/// </summary>
public sealed class HighlightedCodeBlockExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        // No parser/block changes needed — only the HTML renderer is customized, below.
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is not HtmlRenderer htmlRenderer)
        {
            return;
        }

        htmlRenderer.ObjectRenderers.Replace<Markdig.Renderers.Html.CodeBlockRenderer>(
            new HighlightedCodeBlockRenderer());
    }
}

public static class MarkdownPipelineBuilderExtensions
{
    public static MarkdownPipelineBuilder UseHighlightedCodeBlocks(this MarkdownPipelineBuilder pipeline)
    {
        pipeline.Extensions.AddIfNotAlready<HighlightedCodeBlockExtension>();
        return pipeline;
    }
}
