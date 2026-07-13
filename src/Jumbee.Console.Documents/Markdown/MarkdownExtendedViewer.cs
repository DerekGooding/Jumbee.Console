namespace Jumbee.Console.DocumentViewers;

using System;
using System.Collections.Generic;
using System.Text;

using ConsoleGUI.Space;

using NTokenizers.Extensions.Spectre.Console.Styles;

/// <summary>
/// A <see cref="MarkdownViewer"/> that renders fenced <c>```mermaid</c> code blocks as diagrams (flowchart, sequence,
/// class, ER, state) instead of showing their source. Markdown between diagrams is rendered by the base viewer
/// unchanged; each diagram is rasterized to box-drawing cells and stacked inline. A document with no mermaid blocks
/// renders identically to the base — the extra work happens only when a diagram is present.
/// <para>
/// A diagram that fails to parse falls back to a normal fenced code block, so a bad diagram never blanks the document.
/// Diagrams wider than the control clip on the right (this viewer, like the base, scrolls only vertically).
/// </para>
/// </summary>
public class MarkdownExtendedViewer : MarkdownViewer
{
    #region Constructors
    public MarkdownExtendedViewer(string markdown = "") : base(markdown) { }
    #endregion

    #region Properties
    /// <summary>Colours / scale for embedded mermaid diagrams. Defaults to <see cref="MermaidStyles.Default"/>.</summary>
    public MermaidStyles DiagramStyles
    {
        get => _mermaid;
        set => UI.Invoke(() => { _mermaid = value ?? MermaidStyles.Default; InvalidateContent(); });
    }
    #endregion

    #region Methods
    // Splits the document at fenced ```mermaid blocks, renders markdown spans via the base viewer and diagram spans via
    // the mermaid rasterizer, then stacks the pieces into one content buffer. Falls straight through to the base single
    // pass when there are no diagrams, so plain markdown pays only one O(lines) scan.
    protected override (ConsoleBuffer buffer, int height) RenderMarkdown(string text, MarkdownStyles styles, int width)
    {
        var segments = Split(text);
        if (segments.Count == 1 && !segments[0].IsMermaid)
            return base.RenderMarkdown(text, styles, width);

        var parts = new List<(ConsoleBuffer buf, int height)>();
        foreach (var (isMermaid, content) in segments)
        {
            if (string.IsNullOrWhiteSpace(content)) continue;
            parts.Add(isMermaid ? RenderDiagram(content, styles, width) : base.RenderMarkdown(content, styles, width));
        }
        return Stack(parts, width);
    }

    // Rasterizes one mermaid block into a width-clipped buffer; on any parse/render failure shows the block as a plain
    // fenced code block (via the base) rather than losing it.
    private (ConsoleBuffer buffer, int height) RenderDiagram(string code, MarkdownStyles styles, int width)
    {
        try
        {
            var canvas = MermaidCanvas.Build(code, _mermaid);
            var h = Math.Clamp(canvas.Height, 1, MaxRows);
            var buffer = new ConsoleBuffer { Size = new Size(Math.Max(1, width), h) };
            buffer.Initialize();
            canvas.Blit(buffer);   // clips horizontally to the control width if the diagram is wider
            return (buffer, h);
        }
        catch
        {
            return base.RenderMarkdown($"```\n{code}```", styles, width);
        }
    }

    // Vertically concatenates the rendered pieces (a blank spacer row between them) into a single buffer.
    private static (ConsoleBuffer buffer, int height) Stack(List<(ConsoleBuffer buf, int height)> parts, int width)
    {
        var total = 0;
        for (var i = 0; i < parts.Count; i++) total += parts[i].height + (i > 0 ? 1 : 0);
        total = Math.Clamp(total, 1, MaxRows);

        var combined = new ConsoleBuffer { Size = new Size(Math.Max(1, width), total) };
        combined.Initialize();

        var y = 0;
        foreach (var (buf, height) in parts)
        {
            var rows = Math.Min(height, combined.Size.Height - y);
            for (var yy = 0; yy < rows; yy++)
                for (var x = 0; x < width && x < buf.Size.Width; x++)
                    combined.Write(x, y + yy, buf[x, yy]);
            y += height + 1;   // blank spacer row before the next piece
            if (y >= combined.Size.Height) break;
        }
        return (combined, Math.Min(y, combined.Size.Height));
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
        if (t.StartsWith("```", StringComparison.Ordinal)) return t[3..].Trim();
        if (t.StartsWith("~~~", StringComparison.Ordinal)) return t[3..].Trim();
        return null;
    }

    private static bool IsFenceClose(string line)
    {
        var t = line.Trim();
        return (t.StartsWith("```", StringComparison.Ordinal) || t.StartsWith("~~~", StringComparison.Ordinal))
            && t.TrimEnd('`', '~').Length == 0;
    }

    private static bool IsMermaidLang(string lang) =>
        lang.Trim().ToLowerInvariant() is "mermaid" or "mmd";
    #endregion

    #region Fields
    private MermaidStyles _mermaid = MermaidStyles.Default;
    #endregion
}
