namespace Jumbee.Console.Tests;

using ColorCode;

using RazorConsole.Core.Rendering.Syntax;

using Spectre.Console;

using Xunit;

/// <summary>
/// <see cref="SpectreSegmentFormatter"/> (direct segments — the fast path) must render byte-for-byte the same
/// buffer as the original <see cref="SpectreMarkupFormatter"/> (markup string → Spectre re-parse). This locks
/// the optimization in as behaviour-preserving across every ColorCode language the editor uses.
/// </summary>
public class SyntaxFormatterEquivalenceTests
{
    // Representative source per language, no trailing whitespace (the markup Paragraph path can trim trailing
    // spaces; the editor's source is trimmed clean, so this compares like for like).
    public static TheoryData<string, string> Cases() => new()
    {
        {
            "csharp",
            """
            namespace Acme;

            using System;

            // A tiny widget.
            public sealed class Widget
            {
                private readonly string _name = "hi";
                public int Count { get; set; } = 0;
                public string Describe() => $"{_name}: {Count}";
            }
            """
        },
        {
            "html",
            """
            <div class="card">
              <h1>Title</h1>
              <!-- a comment -->
              <p>Some <b>bold</b> text</p>
            </div>
            """
        },
        {
            "sql",
            """
            SELECT id, name
            FROM widgets
            WHERE count > 0
            ORDER BY name;
            """
        },
        {
            "cpp",
            """
            #include <string>

            // A tiny widget.
            class Widget {
              std::string name_;
             public:
              int count = 0;
              explicit Widget(std::string name) : name_(name) {}
            };
            """
        },
        {
            "java",
            """
            package acme;

            // A tiny widget.
            public final class Widget {
                private final String name;
                private int count = 0;

                public Widget(String name) { this.name = name; }
            }
            """
        },
        {
            "python",
            """
            # A tiny widget.
            class Widget:
                def __init__(self, name: str) -> None:
                    self.name = name
                    self.count = 0

                def describe(self) -> str:
                    return f"{self.name}: {self.count}"
            """
        },
    };

    private static ILanguage Language(string id) => id switch
    {
        "csharp" => Languages.CSharp,
        "html" => Languages.Html,
        "sql" => Languages.Sql,
        "cpp" => Languages.Cpp,
        "java" => Languages.Java,
        "python" => Languages.Python,
        _ => throw new System.ArgumentOutOfRangeException(nameof(id)),
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void SegmentFormatter_RendersSameBuffer_AsMarkupFormatter(string languageId, string source) =>
        AssertSameBuffer(languageId, source, tabWidth: 0);

    // Covers ExpandTabs (only reached when TabWidth > 0): leading-tab indentation, TabWidth = 4.
    [Fact]
    public void SegmentFormatter_ExpandsTabs_SameAsMarkupFormatter() =>
        AssertSameBuffer("csharp", "class C\n{\n\tint x = 0;\n\tint Y() => x;\n}", tabWidth: 4);

    private static void AssertSameBuffer(string languageId, string source, int tabWidth)
    {
        var language = Language(languageId);
        var theme = SyntaxTheme.CreateDefault();
        var options = new SyntaxOptions { TabWidth = tabWidth };

        // Inflate the profile width past the longest *expanded* line so neither path word-wraps inside Spectre
        // (both then char-wrap at the buffer width, as the editor does). Tabs count as tabWidth columns here.
        var profileWidth = LongestLineWidth(source, tabWidth) + 1;
        var markupBuffer = Render(console =>
            console.Markup(new SpectreMarkupFormatter().Format(source, language, theme, options)), profileWidth);
        var segmentBuffer = Render(console =>
            console.Write(new SpectreSegmentFormatter().Format(source, language, theme, options)), profileWidth);

        for (var y = 0; y < markupBuffer.Size.Height; y++)
        {
            for (var x = 0; x < markupBuffer.Size.Width; x++)
            {
                var a = markupBuffer[x, y].Character;
                var b = segmentBuffer[x, y].Character;
                Assert.True(
                    a.Content == b.Content && a.Foreground == b.Foreground &&
                    a.Background == b.Background && a.Decoration == b.Decoration,
                    $"[{languageId}] cell ({x},{y}) differs: markup='{a.Content}'/{a.Foreground} vs segment='{b.Content}'/{b.Foreground}");
            }
        }
    }

    // Mirror TextEditor.WriteText: inflate the profile width so Spectre never word-wraps; the buffer applies its
    // own character wrap (wrap = true).
    private static ConsoleBuffer Render(System.Action<AnsiConsoleBuffer> write, int profileWidth)
    {
        var buffer = new ConsoleBuffer { Size = new ConsoleGUI.Space.Size(100, 40) };
        buffer.Initialize();
        var console = new AnsiConsoleBuffer(buffer) { wrap = true };
        console.Profile.Width = profileWidth;
        write(console);
        return buffer;
    }

    private static int LongestLineWidth(string text, int tabWidth)
    {
        int max = 1, col = 0;
        foreach (var c in text)
        {
            if (c == '\n') { if (col > max) max = col; col = 0; }
            else if (c == '\t') col += tabWidth > 0 ? tabWidth : 1;
            else if (c != '\r') col++;
        }
        return System.Math.Max(max, col);
    }
}
