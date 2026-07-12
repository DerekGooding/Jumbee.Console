namespace Jumbee.Console.Tests;

using System.Linq;

using Jumbee.Console;
using Jumbee.Console.DocumentViewers;
using Jumbee.Console.Snapshot;

using RazorConsole.Core.Rendering.Syntax;

using Spectre.Console.Rendering;

using Xunit;

using Color = Spectre.Console.Color;
using Decoration = Spectre.Console.Decoration;

// Verifies the AsciiDoc ColorCode grammar assigns the expected scopes by checking the colours/decorations the default
// syntax theme resolves them to (the same formatter/theme the CodeEditor uses).
public class AsciiDocLanguageTests
{
    private static readonly Color Heading = Color.FromHex("#569CD6");       // blue (also Keyword)
    private static readonly Color ControlKeyword = Color.FromHex("#C586C0"); // purple (admonition)
    private static readonly Color Code = Color.FromHex("#CE9178");           // orange (monospace / link)
    private static readonly Color Comment = Color.FromHex("#6A9955");        // green
    private static readonly Color Attribute = Color.FromHex("#D7BA7D");      // gold (block attr / title)
    private static readonly Color ListItem = Color.FromHex("#6796E6");       // blue (list marker)

    private static IReadOnlyList<Segment> Highlight(string src) =>
        new SpectreSegmentFormatter()
            .Format(src, AsciiDocLanguage.Instance, SyntaxTheme.CreateDefault(), new SyntaxOptions { TabWidth = 0 })
            .ToList();

    private static Color? ColorOf(IReadOnlyList<Segment> segs, string token) =>
        segs.FirstOrDefault(s => s.Text == token)?.Style.Foreground;

    private static Decoration? DecorationOf(IReadOnlyList<Segment> segs, string token) =>
        segs.FirstOrDefault(s => s.Text == token)?.Style.Decoration;

    [Fact]
    public void Highlights_Heading_Comment_Admonition_And_Monospace()
    {
        var segs = Highlight("= Doc Title\n// a comment\nNOTE: heads up\ninline `code` here\n");

        Assert.Equal(Heading, ColorOf(segs, "= Doc Title"));
        Assert.Equal(Comment, ColorOf(segs, "// a comment"));
        Assert.Equal(ControlKeyword, ColorOf(segs, "NOTE:"));
        Assert.Equal(Code, ColorOf(segs, "`code`"));
    }

    [Fact]
    public void Highlights_BoldAndItalic_ByDecoration()
    {
        var segs = Highlight("A *bold* and _italic_ run.\n");

        Assert.True(DecorationOf(segs, "*bold*")?.HasFlag(Decoration.Bold));
        Assert.True(DecorationOf(segs, "_italic_")?.HasFlag(Decoration.Italic));
    }

    [Fact]
    public void Highlights_Attributes_Lists_And_Links()
    {
        var segs = Highlight(":toc: left\n[source,ruby]\n* an item\nSee https://example.com now\n");

        Assert.Equal(Heading, ColorOf(segs, ":toc:"));          // attribute entry -> Keyword (blue)
        Assert.Equal(Attribute, ColorOf(segs, "[source,ruby]"));
        Assert.Equal(Code, ColorOf(segs, "https://example.com"));
        Assert.Equal(ListItem, ColorOf(segs, "* "));            // the "* " marker
    }

    [Fact]
    public void CodeEditor_AcceptsTheAsciiDocGrammar_AndRenders()
    {
        var editor = new CodeEditor(AsciiDocLanguage.Instance) { Text = "= Title\n\nSome *bold* text.\n" };

        var text = ConsoleSnapshot.ToText(editor, 40, 10);

        Assert.Contains("Title", text);
        Assert.Contains("bold", text);
    }
}
