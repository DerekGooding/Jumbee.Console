
using AdocNet.Ast;
using ColorCode;
using RazorConsole.Core.Rendering.Syntax;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

namespace Jumbee.Console.Documents;
/// <summary>
/// Traverses an AsciiDoc <see cref="DocumentNode"/> and composes a tree of Spectre.Console
/// <see cref="IRenderable"/>s (headings as rules, code/example/admonition blocks as panels, AsciiDoc tables as
/// Spectre tables, inline markup as styled <see cref="Markup"/>). The whole document is written to an
/// <see cref="IAnsiConsole"/> in a single <c>AnsiConsoleExtensions.Write(IAnsiConsole, IRenderable)</c> call.
/// </summary>
internal sealed class AsciiDocRenderer
{
    #region Constructors

    public AsciiDocRenderer(AsciiDocStyles styles) => _s = styles;

    #endregion Constructors

    #region Methods

    /// <summary>Builds a single renderable for the whole document (title rule + stacked, blank-separated blocks).</summary>
    public IRenderable Render(DocumentNode document)
    {
        var blocks = new List<IRenderable>();
        if (!string.IsNullOrEmpty(document.Title))
            blocks.Add(new Rule(Wrap(_s.Title, Esc(document.Title))) { Justification = Spectre.Console.Justify.Left });
        blocks.AddRange(RenderBlocks(document.Children.OfType<BlockNode>()));
        return Stack(blocks, spaced: true);
    }

    // ── Block dispatch ───────────────────────────────────────────────────
    private List<IRenderable> RenderBlocks(IEnumerable<BlockNode> nodes)
    {
        var list = new List<IRenderable>();
        foreach (var node in nodes)
        {
            IRenderable? r;
            try { r = RenderBlock(node); }
            catch { r = new Markup(Wrap("red", Esc($"[unrenderable {node.Kind}]"))); }
            if (r is not null) list.Add(r);
        }
        return list;
    }

    private IRenderable? RenderBlock(BlockNode node) => node switch
    {
        SectionNode n => RenderSection(n),
        ParagraphNode n => new Markup(InlineMarkup(n.Inlines)),
        ListNode n => RenderList(n),
        TableNode n => RenderTable(n),
        DelimitedBlockNode n => RenderDelimitedBlock(n),
        BlockImageNode n => RenderBlockImage(n),
        AdmonitionNode n => RenderAdmonition(n),
        DescriptionListNode n => RenderDescriptionList(n),
        TocNode n => RenderToc(n),
        ThematicBreakNode => new Rule { Style = Spectre.Console.Style.Parse(_s.PanelBorder) },
        PageBreakNode => new Rule { Style = Spectre.Console.Style.Parse(_s.PanelBorder) },
        _ => null,   // video, audio, index, bibliography — skipped in v1
    };

    private IRenderable RenderSection(SectionNode section)
    {
        var title = section.TitleInlines.Count > 0 ? InlineMarkup(section.TitleInlines) : Esc(section.Title);
        var level = System.Math.Clamp(section.Level, 1, _s.Headings.Length - 1);
        var style = _s.Headings[level];
        IRenderable heading = section.Level <= 1
            ? new Rule(Wrap(style, title)) { Justification = Spectre.Console.Justify.Left }
            : new Markup(Wrap(style, title));

        var items = new List<IRenderable> { heading };
        items.AddRange(RenderBlocks(section.Children.OfType<BlockNode>()));
        return Stack(items, spaced: true);
    }

    private IRenderable RenderList(ListNode list)
    {
        var items = new List<IRenderable>();
        var index = list.Start ?? 1;
        foreach (var item in list.Children.OfType<ListItemNode>())
        {
            string marker =
                item.Checked is bool chk ? (chk ? "☑" : "☐")          // ☑ / ☐
                : list.ListKind == ListKind.Ordered ? $"{index++}."
                : "•";                                                      // •
            var line = new Markup($"{Wrap(_s.ListMarker, Esc(marker))} {InlineMarkup(item.Inlines)}");

            var nested = RenderBlocks(item.Children.OfType<BlockNode>());
            if (nested.Count == 0)
            {
                items.Add(line);
            }
            else
            {
                items.Add(new Rows(line, new Padder(new Rows(nested), new Padding(2, 0, 0, 0))));
            }
        }
        return new Rows(items);
    }

    private IRenderable RenderTable(TableNode node)
    {
        var rows = node.Children.OfType<TableRowNode>().ToList();
        if (rows.Count == 0) return Blank;

        var table = new Table { Border = TableBorder.Rounded, Expand = false };
        if (node.Title is not null) table.Title = new TableTitle(Esc(node.Title));

        var columnCount = rows.Max(r => r.Children.OfType<TableCellNode>().Count());
        var firstIsHeader = node.HasHeader;

        if (firstIsHeader)
        {
            var headers = Cells(rows[0]);
            for (var i = 0; i < columnCount; i++)
                table.AddColumn(new TableColumn(i < headers.Count ? headers[i] : string.Empty));
        }
        else
        {
            for (var i = 0; i < columnCount; i++)
                table.AddColumn(new TableColumn(string.Empty));
            table.ShowHeaders = false;
        }

        for (var r = firstIsHeader ? 1 : 0; r < rows.Count; r++)
        {
            var cells = Cells(rows[r]);
            var rendered = new IRenderable[columnCount];
            for (var c = 0; c < columnCount; c++)
                rendered[c] = new Markup(c < cells.Count ? cells[c] : string.Empty);
            table.AddRow(rendered);
        }
        return table;

        List<string> Cells(TableRowNode row) =>
            row.Children.OfType<TableCellNode>().Select(c => InlineMarkup(c.Inlines, c.Text)).ToList();
    }

    private IRenderable RenderDelimitedBlock(DelimitedBlockNode block)
    {
        switch (block.BlockKind)
        {
            case DelimitedBlockKind.Source:
            case DelimitedBlockKind.Listing:
            case DelimitedBlockKind.Literal:
                {
                    // A `[source,mermaid]` block renders the diagram itself, not its text — parse+rasterize it and drop
                    // the cell grid into the panel via the CellCanvas→IRenderable adapter.
                    if (IsMermaid(block.Language) && RenderMermaid(block.Content ?? string.Empty) is { } diagram)
                    {
                        return new Panel(diagram)
                        {
                            Border = BoxBorder.Rounded,
                            BorderStyle = Spectre.Console.Style.Parse(_s.PanelBorder),
                            Header = new PanelHeader(" mermaid "),
                            Expand = false,
                        };
                    }

                    var panel = new Panel(CodeContent(block.Content ?? string.Empty, block.Language))
                    {
                        Border = BoxBorder.Rounded,
                        BorderStyle = Spectre.Console.Style.Parse(_s.PanelBorder),
                        Expand = true,
                    };
                    var header = block.Language ?? block.Title
                        ?? (block.BlockKind == DelimitedBlockKind.Literal ? null : "source");
                    if (header is not null) panel.Header = new PanelHeader($" {Esc(header)} ");
                    return panel;
                }

            case DelimitedBlockKind.Quote:
            case DelimitedBlockKind.Verse:
                {
                    IRenderable body = block.Content is not null
                        ? new Markup(Wrap(_s.Quote, Esc(block.Content)))   // Esc preserves embedded newlines
                        : new Rows(RenderBlocks(block.Children.OfType<BlockNode>()));
                    var lines = new List<IRenderable> { body };
                    if (block.Attribution is not null)
                    {
                        var attr = block.CitationSource is not null
                            ? $"— {block.Attribution}, {block.CitationSource}"
                            : $"— {block.Attribution}";
                        lines.Add(new Markup(Wrap(_s.Attribution, Esc(attr))));
                    }
                    return new Padder(new Rows(lines), new Padding(2, 0, 0, 0));
                }

            case DelimitedBlockKind.Example:
            case DelimitedBlockKind.Sidebar:
                {
                    var panel = new Panel(new Rows(RenderBlocks(block.Children.OfType<BlockNode>())))
                    {
                        Border = BoxBorder.Rounded,
                        BorderStyle = Spectre.Console.Style.Parse(_s.PanelBorder),
                        Expand = true,
                    };
                    var header = block.Title ?? (block.BlockKind == DelimitedBlockKind.Sidebar ? "SIDEBAR" : null);
                    if (header is not null) panel.Header = new PanelHeader($" {Esc(header)} ");
                    return panel;
                }

            default:   // Open, Passthrough
                return block.Content is not null
                    ? new Text(block.Content)
                    : new Rows(RenderBlocks(block.Children.OfType<BlockNode>()));
        }
    }

    private IRenderable RenderBlockImage(BlockImageNode image)
    {
        var label = string.IsNullOrEmpty(image.Alt) ? image.Target : image.Alt;
        return new Markup(Wrap(_s.CrossReference, Esc($"[image: {label}]")));
    }

    private IRenderable RenderAdmonition(AdmonitionNode node)
    {
        var (color, glyph) = AdmonitionAccent(node.AdmonitionType);
        IRenderable body = node.Inlines.Count > 0
            ? new Markup(InlineMarkup(node.Inlines))
            : node.Text is not null
                ? new Markup(Esc(node.Text))
                : new Rows(RenderBlocks(node.Children.OfType<BlockNode>()));

        var header = node.Title ?? node.AdmonitionType.ToUpperInvariant();
        return new Panel(body)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Spectre.Console.Style.Parse(color),
            Header = new PanelHeader($" {glyph} {Esc(header)} "),
            Expand = true,
        };
    }

    private IRenderable RenderDescriptionList(DescriptionListNode list)
    {
        var items = new List<IRenderable>();
        foreach (var item in list.Children.OfType<DescriptionItemNode>())
        {
            var term = new Markup(Wrap(_s.Strong,
                item.TermInlines.Count > 0 ? InlineMarkup(item.TermInlines) : Esc(string.Join(", ", item.Terms))));
            var desc = new Padder(new Markup(
                item.DescriptionInlines.Count > 0 ? InlineMarkup(item.DescriptionInlines) : Esc(item.Description)),
                new Padding(2, 0, 0, 0));
            items.Add(new Rows(term, desc));
        }
        return Stack(items, spaced: true);
    }

    private IRenderable RenderToc(TocNode toc)
    {
        if (toc.Entries.Count == 0) return Blank;
        var tree = new Spectre.Console.Tree(Wrap(_s.Headings[1], Esc("Table of Contents"))) { Guide = Spectre.Console.TreeGuide.Line };
        foreach (var entry in toc.Entries) AddTocEntry(tree, entry);
        return tree;
    }

    private void AddTocEntry(IHasTreeNodes parent, TocEntry entry)
    {
        var node = parent.AddNode(new Markup(Wrap(_s.CrossReference, Esc(entry.Title))));
        foreach (var child in entry.Children) AddTocEntry(node, child);
    }

    private static bool IsMermaid(string? language) =>
        language?.Trim().ToLowerInvariant() is "mermaid" or "mmd";

    // Rasterizes a Mermaid block to a renderable, or null on a parse/render failure (the caller then falls back to
    // showing the block as plain source, so a bad diagram never blanks the surrounding document).
    private IRenderable? RenderMermaid(string source)
    {
        try { return MermaidCanvas.Build(source, _s.Mermaid).ToRenderable(); }
        catch { return null; }
    }

    // Verbatim block body: syntax-highlighted Markup when the language is recognised, else plain styled text.
    private IRenderable CodeContent(string code, string? language)
    {
        if (ColorCodeLanguage(language) is not { } ccLang)
            return new Text(code, Spectre.Console.Style.Parse(_s.Code));
        try
        {
            _highlighter ??= new SpectreMarkupFormatter();
            _syntaxTheme ??= SyntaxTheme.CreateDefault();
            return new Markup(_highlighter.Format(code, ccLang, _syntaxTheme, _syntaxOptions));
        }
        catch
        {
            return new Text(code, Spectre.Console.Style.Parse(_s.Code));
        }
    }

    // Maps an AsciiDoc source language token to a ColorCode grammar, or null to render the block unhighlighted.
    private static ILanguage? ColorCodeLanguage(string? language) => language?.Trim().ToLowerInvariant() switch
    {
        "csharp" or "c#" or "cs" or "dotnet" => Languages.CSharp,
        "java" => Languages.Java,
        "python" or "py" => Languages.Python,
        "javascript" or "js" or "typescript" or "ts" => Languages.Typescript,
        "xml" => Languages.Xml,
        "html" or "xhtml" => Languages.Html,
        "css" => Languages.Css,
        "sql" => Languages.Sql,
        "cpp" or "c++" or "cxx" => Languages.Cpp,
        "markdown" or "md" => Languages.Markdown,
        _ => null,
    };

    // ── Inline markup ────────────────────────────────────────────────────
    private string InlineMarkup(IReadOnlyList<InlineNode> inlines, string? fallback = null)
    {
        if (inlines.Count == 0) return fallback is not null ? Esc(fallback) : string.Empty;
        var sb = new StringBuilder();
        AppendInlines(sb, inlines);
        return sb.ToString();
    }

    private void AppendInlines(StringBuilder sb, IEnumerable<InlineNode> nodes)
    {
        foreach (var node in nodes) AppendInline(sb, node);
    }

    private void AppendInline(StringBuilder sb, InlineNode node)
    {
        switch (node)
        {
            case TextInlineNode n: sb.Append(Esc(n.Value)); break;
            case StrongInlineNode n: AppendStyled(sb, _s.Strong, n.Children); break;
            case EmphasisInlineNode n: AppendStyled(sb, _s.Emphasis, n.Children); break;
            case MonospaceInlineNode n: AppendStyled(sb, _s.Monospace, n.Children); break;
            case HighlightInlineNode n: AppendStyled(sb, _s.Highlight, n.Children); break;
            case SuperscriptInlineNode n: sb.Append(Esc("^" + n.Content)); break;
            case SubscriptInlineNode n: sb.Append(Esc("~" + n.Content)); break;
            case PassthroughInlineNode n: sb.Append(Esc(n.Content)); break;
            case LinkInlineNode n: sb.Append(Wrap(_s.Link, Esc(n.Url))); break;
            case InlineLinkMacroNode n: sb.Append(Wrap(_s.Link, Esc(string.IsNullOrEmpty(n.Label) ? n.Url : n.Label))); break;
            case CrossReferenceInlineNode n: sb.Append(Wrap(_s.CrossReference, Esc(n.Label ?? n.Target))); break;
            case InlineImageNode n: sb.Append(Wrap(_s.CrossReference, Esc($"[image: {(string.IsNullOrEmpty(n.Alt) ? n.Target : n.Alt)}]"))); break;
            case InlineMacroNode n: sb.Append(Esc(n.Content)); break;
            case FootnoteInlineNode: sb.Append(Wrap(_s.CrossReference, Esc("[*]"))); break;
            case InlineAnchorNode: break;   // invisible target
            default: break;   // unknown inline — emit nothing
        }
    }

    private void AppendStyled(StringBuilder sb, string style, IReadOnlyList<InlineNode> children)
    {
        var styled = !string.IsNullOrEmpty(style);
        if (styled) sb.Append('[').Append(style).Append(']');
        AppendInlines(sb, children);
        if (styled) sb.Append("[/]");
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    private (string color, string glyph) AdmonitionAccent(string type) => type.ToUpperInvariant() switch
    {
        "TIP" => (_s.AdmonitionTip, "✓"),        // ✓
        "IMPORTANT" => (_s.AdmonitionImportant, "❕"),  // ❕
        "WARNING" => (_s.AdmonitionWarning, "⚠"),    // ⚠
        "CAUTION" => (_s.AdmonitionCaution, "⚡"),    // ⚡
        _ => (_s.AdmonitionNote, "ℹ"),       // ℹ
    };

    private static string Esc(string? s) => Markup.Escape(s ?? string.Empty);

    private static string Wrap(string style, string content) =>
        string.IsNullOrEmpty(style) ? content : $"[{style}]{content}[/]";

    // Stacks block renderables, optionally with a single blank line between them.
    private static IRenderable Stack(IReadOnlyList<IRenderable> blocks, bool spaced)
    {
        if (blocks.Count == 0) return Blank;
        if (!spaced || blocks.Count == 1) return new Rows(blocks);
        var joined = new List<IRenderable>(blocks.Count * 2 - 1);
        for (var i = 0; i < blocks.Count; i++)
        {
            if (i > 0) joined.Add(Blank);
            joined.Add(blocks[i]);
        }
        return new Rows(joined);
    }

    #endregion Methods

    #region Fields

    // A one-cell renderable that occupies a single (visually blank) row — Rows drops zero-segment children.
    private static readonly IRenderable Blank = new Text(" ");

    private readonly AsciiDocStyles _s;

    // Syntax highlighter for source blocks, created lazily (only if the document has a recognised code block). Each
    // AsciiDocRenderer is single-use on one thread, so the non-thread-safe formatter/theme are safe as instance state.
    private SpectreMarkupFormatter? _highlighter;

    private SyntaxTheme? _syntaxTheme;
    private readonly SyntaxOptions _syntaxOptions = new() { TabWidth = 4 };

    #endregion Fields
}