using ConsoleGUI.Space;
using NTokenizers.Extensions.Spectre.Console.Styles;
using System.Text;

namespace Jumbee.Console.Documents;

/// <summary>
/// A <see cref="MarkdownViewer"/> that renders fenced <c>```mermaid</c> code blocks as diagrams (flowchart, sequence,
/// class, ER, state) instead of showing their source.
/// </summary>
/// <remarks>
/// Markdown between diagrams is rendered by the base viewer
/// unchanged; each diagram is rasterized to box-drawing cells and stacked inline. A document with no mermaid blocks
/// renders identically to the base — the extra work happens only when a diagram is present.
/// <para>
/// A diagram that fails to parse falls back to a normal fenced code block, so a bad diagram never blanks the document.
/// Diagrams wider than the control clip on the right (this viewer, like the base, scrolls only vertically).
/// </para>
/// </remarks>
/// <remarks>Initializes a new <see cref="MarkdownExtendedViewer"/> showing <paramref name="markdown"/>.</remarks>
public class MarkdownExtendedViewer(string markdown = "") : MarkdownViewer(markdown)
{
    #region Properties

    /// <summary>Colours / scale for embedded mermaid diagrams. Defaults to <see cref="MermaidStyles.Default"/>.</summary>
    public MermaidStyles DiagramStyles
    {
        get;
        set => UI.Invoke(() => { field = value ?? MermaidStyles.Default; InvalidateContent(); });
    } = MermaidStyles.Default;

    #endregion Properties

    #region Methods

    /// <inheritdoc/>
    // Splits the document at fenced ```mermaid blocks, renders markdown spans via the base viewer and diagram spans via
    // the mermaid rasterizer, then stacks the pieces into the reusable `target` content buffer. Falls straight through
    // to the base single pass when there are no diagrams, so plain markdown pays only one O(lines) scan.
    // Only `target` (the caller's reused ping-pong buffer) is reused; the per-segment part buffers are still transient
    // (a future pass can pool them).
    protected override int RenderMarkdown(string text, MarkdownStyles styles, int width, ConsoleBuffer target)
    {
        var segments = Split(text);
        if (segments.Count == 1 && !segments[0].IsMermaid)
            return base.RenderMarkdown(text, styles, width, target);

        var parts = new List<(ConsoleBuffer buf, int height)>();
        foreach (var (isMermaid, content) in segments)
        {
            if (string.IsNullOrWhiteSpace(content)) continue;
            if (isMermaid)
            {
                parts.Add(RenderDiagram(content, styles, width));
            }
            else
            {
                var part = new ConsoleBuffer();
                var h = base.RenderMarkdown(content, styles, width, part);
                parts.Add((part, h));
            }
        }
        return Stack(parts, width, target);
    }

    // Rasterizes one mermaid block into a width-clipped transient buffer; on any parse/render failure shows the block
    // as a plain fenced code block (via the base) rather than losing it.
    private (ConsoleBuffer buffer, int height) RenderDiagram(string code, MarkdownStyles styles, int width)
    {
        try
        {
            var canvas = MermaidCanvas.Build(code, DiagramStyles);
            var h = Math.Clamp(canvas.Height, 1, MaxRows);
            var buffer = new ConsoleBuffer { Size = new Size(Math.Max(1, width), h) };
            buffer.Initialize();
            canvas.Blit(buffer);   // clips horizontally to the control width if the diagram is wider
            return (buffer, h);
        }
        catch
        {
            var buffer = new ConsoleBuffer();
            var h = base.RenderMarkdown($"```\n{code}```", styles, width, buffer);
            return (buffer, h);
        }
    }

    // Vertically concatenates the rendered pieces (a blank spacer row between them) into the reusable `target` buffer.
    private static int Stack(List<(ConsoleBuffer buf, int height)> parts, int width, ConsoleBuffer target)
    {
        var total = 0;
        for (var i = 0; i < parts.Count; i++) total += parts[i].height + (i > 0 ? 1 : 0);
        total = Math.Clamp(total, 1, MaxRows);

        target.Size = new Size(Math.Max(1, width), total);   // capacity-retentive reuse of the caller's buffer
        target.Initialize();

        var y = 0;
        foreach (var (buf, height) in parts)
        {
            var rows = Math.Min(height, target.Size.Height - y);
            for (var yy = 0; yy < rows; yy++)
                for (var x = 0; x < width && x < buf.Size.Width; x++)
                    target.Write(x, y + yy, buf[x, yy]);
            y += height + 1;   // blank spacer row before the next piece
            if (y >= target.Size.Height) break;
        }
        return Math.Min(y, target.Size.Height);
    }

    // Line-scans for top-level ``` / ~~~ mermaid fences (the common case). Returns alternating spans: mermaid spans
    // carry the inner diagram source, markdown spans the surrounding text. Non-mermaid fences pass through untouched
    // inside the markdown spans, so the base writer still highlights them.
    private static List<(bool IsMermaid, string Content)> Split(string text)
    {
        var result = new List<(bool, string)>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var md = new StringBuilder();

        for (var i = 0; i < lines.Length; i++)
        {
            if (FenceInfo(lines[i]) is { } lang && IsMermaidLang(lang))
            {
                if (md.Length > 0) { result.Add((false, md.ToString())); md.Clear(); }
                var code = new StringBuilder();
                for (i++; i < lines.Length && !IsFenceClose(lines[i]); i++)
                    code.Append(lines[i]).Append('\n');
                result.Add((true, code.ToString()));   // i is left on the closing fence; the for-loop skips it
            }
            else
            {
                md.Append(lines[i]).Append('\n');
            }
        }
        if (md.Length > 0) result.Add((false, md.ToString()));
        if (result.Count == 0) result.Add((false, text));
        return result;
    }

    // The info string (language) of an opening ``` / ~~~ fence, or null when the line isn't a fence opener.
    private static string? FenceInfo(string line)
    {
        var t = line.TrimStart();
        return t.StartsWith("```", StringComparison.Ordinal)
            ? t[3..].Trim()
            : t.StartsWith("~~~", StringComparison.Ordinal) ? t[3..].Trim() : null;
    }

    private static bool IsFenceClose(string line)
    {
        var t = line.Trim();
        return (t.StartsWith("```", StringComparison.Ordinal) || t.StartsWith("~~~", StringComparison.Ordinal))
            && t.TrimEnd('`', '~').Length == 0;
    }

    private static bool IsMermaidLang(string lang) =>
        lang.Trim().ToLowerInvariant() is "mermaid" or "mmd";

    #endregion Methods
}