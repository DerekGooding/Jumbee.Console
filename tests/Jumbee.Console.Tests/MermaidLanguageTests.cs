namespace Jumbee.Console.Tests;

using System.Linq;

using Jumbee.Console;
using Jumbee.Console.Documents;
using Jumbee.Console.Snapshot;

using RazorConsole.Core.Rendering.Syntax;

using Spectre.Console.Rendering;

using Xunit;

using Color = Spectre.Console.Color;

// Verifies the Mermaid ColorCode grammar assigns the expected scopes by checking the colours the default syntax theme
// resolves them to (the same formatter/theme the CodeEditor uses). Colours are the VS Code Dark+ palette from
// SyntaxTheme.CreateDefault().
public class MermaidLanguageTests
{
    private static readonly Color Keyword = Color.FromHex("#569CD6");       // blue
    private static readonly Color ControlKeyword = Color.FromHex("#C586C0"); // purple (arrows)
    private static readonly Color String_ = Color.FromHex("#CE9178");        // orange (labels)
    private static readonly Color Comment = Color.FromHex("#6A9955");        // green
    private static readonly Color ClassName = Color.FromHex("#4EC9B0");      // teal (directions)

    private static IReadOnlyList<Segment> Highlight(string src) =>
        new SpectreSegmentFormatter()
            .Format(src, MermaidLanguage.Instance, SyntaxTheme.CreateDefault(), new SyntaxOptions { TabWidth = 0 })
            .ToList();

    // The foreground colour of the first segment whose text equals `token`, or null if the token wasn't a segment.
    private static Color? ColorOf(IReadOnlyList<Segment> segs, string token) =>
        segs.FirstOrDefault(s => s.Text == token)?.Style.Foreground;

    [Fact]
    public void Highlights_DiagramKeyword_Direction_Arrow_Label_And_Comment()
    {
        var segs = Highlight("graph TD\n    A[Start] --> B{Decision}\n    %% a comment\n");

        Assert.Equal(Keyword, ColorOf(segs, "graph"));
        Assert.Equal(ClassName, ColorOf(segs, "TD"));
        Assert.Equal(ControlKeyword, ColorOf(segs, "-->"));
        Assert.Equal(String_, ColorOf(segs, "[Start]"));
        Assert.Equal(Comment, ColorOf(segs, "%% a comment"));
    }

    [Fact]
    public void Highlights_QuotedStrings_And_StructuralKeywords()
    {
        var segs = Highlight("sequenceDiagram\n    participant A\n    A->>B: \"hi\"\n");

        Assert.Equal(Keyword, ColorOf(segs, "sequenceDiagram"));
        Assert.Equal(Keyword, ColorOf(segs, "participant"));
        Assert.Equal(ControlKeyword, ColorOf(segs, "->>"));
        Assert.Equal(String_, ColorOf(segs, "\"hi\""));
    }

    [Fact]
    public void Highlights_ClassRelationships_And_ErCardinality()
    {
        var cls = Highlight("classDiagram\n    Animal <|-- Dog\n");
        Assert.Equal(Keyword, ColorOf(cls, "classDiagram"));
        Assert.Equal(ControlKeyword, ColorOf(cls, "<|--"));

        var er = Highlight("erDiagram\n    CUSTOMER ||--o{ ORDER : places\n");
        Assert.Equal(Keyword, ColorOf(er, "erDiagram"));
        Assert.Equal(ControlKeyword, ColorOf(er, "||--o{"));
    }

    [Fact]
    public void CodeEditor_AcceptsTheMermaidGrammar_AndRenders()
    {
        // End-to-end: the custom-ILanguage CodeEditor/TextEditor constructor path drives the Mermaid grammar.
        var editor = new CodeEditor(MermaidLanguage.Instance) { Text = "graph TD\n    A[Start] --> B\n" };

        var text = ConsoleSnapshot.ToText(editor, 40, 10);

        Assert.Contains("graph", text);
        Assert.Contains("-->", text);
        Assert.Contains("Start", text);
    }

    [Fact]
    public void PlainText_IsNotColoured()
    {
        // A bare node id gets no rule, so it stays the default (plain) foreground (it's emitted in a default-styled
        // run with the surrounding whitespace rather than as its own token segment).
        var segs = Highlight("graph LR\n    Node1\n");
        var run = segs.First(s => s.Text.Contains("Node1"));
        Assert.Equal(Color.Default, run.Style.Foreground);
    }
}
