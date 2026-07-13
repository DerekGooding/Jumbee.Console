namespace Jumbee.Console.Tests;

using System.Linq;

using Jumbee.Console;
using Jumbee.Console.Documents;
using Jumbee.Console.Snapshot;

using RazorConsole.Core.Rendering.Syntax;

using Spectre.Console.Rendering;

using Xunit;

using Color = Spectre.Console.Color;
using Decoration = Spectre.Console.Decoration;

// Verifies the composite Markdown+Mermaid grammar: Markdown is highlighted normally, and the contents of a ```mermaid
// fence are highlighted by the nested Mermaid grammar (ColorCode's "&mermaid" language embedding). The clearest proof
// is ControlKeyword (purple) on a mermaid arrow — a scope the Markdown grammar never emits.
public class MarkdownWithMermaidLanguageTests
{
    private static readonly Color Keyword = Color.FromHex("#569CD6");        // blue (mermaid diagram keyword)
    private static readonly Color ControlKeyword = Color.FromHex("#C586C0"); // purple (mermaid arrow)
    private static readonly Color Comment = Color.FromHex("#6A9955");        // green (mermaid %% comment)

    private static IReadOnlyList<Segment> Highlight(string src) =>
        new SpectreSegmentFormatter()
            .Format(src, MarkdownWithMermaidLanguage.Instance, SyntaxTheme.CreateDefault(), new SyntaxOptions { TabWidth = 0 })
            .ToList();

    private static Color? ColorOf(IReadOnlyList<Segment> segs, string token) =>
        segs.FirstOrDefault(s => s.Text == token)?.Style.Foreground;

    private const string Doc =
        "# Title\n\nSome **bold** text.\n\n```mermaid\ngraph TD\n    A --> B\n    %% a note\n```\n\nAfter.\n";

    [Fact]
    public void MermaidTokens_InsideAFence_GetTheMermaidGrammarColours()
    {
        var segs = Highlight(Doc);

        Assert.Equal(Keyword, ColorOf(segs, "graph"));       // mermaid diagram keyword
        Assert.Equal(ControlKeyword, ColorOf(segs, "-->"));  // mermaid arrow (Markdown has no such scope)
        Assert.Equal(Comment, ColorOf(segs, "%% a note"));   // mermaid comment
    }

    [Fact]
    public void MarkdownOutsideTheFence_IsStillHighlighted()
    {
        var segs = Highlight(Doc);

        Assert.Equal(Keyword, ColorOf(segs, "# Title"));   // MarkdownHeader (same blue as Keyword) — heading coloured
        Assert.True(segs.First(s => s.Text == "**bold**").Style.Decoration.HasFlag(Decoration.Bold));   // markdown bold
    }

    [Fact]
    public void AMermaidArrowInProse_IsNotColoured()
    {
        // Outside a ```mermaid fence the embedding must not apply: "-->" in plain prose gets no mermaid colour.
        var segs = Highlight("just a --> b arrow in prose\n");

        Assert.NotEqual(ControlKeyword, ColorOf(segs, "-->"));
    }

    [Fact]
    public void CodeEditor_AcceptsTheCompositeGrammar_AndRenders()
    {
        var editor = new CodeEditor(MarkdownWithMermaidLanguage.Instance)
        {
            Text = "# Hi\n\n```mermaid\ngraph TD\n  A --> B\n```\n",
        };

        var text = ConsoleSnapshot.ToText(editor, 40, 12);

        Assert.Contains("graph", text);
        Assert.Contains("-->", text);
    }
}
