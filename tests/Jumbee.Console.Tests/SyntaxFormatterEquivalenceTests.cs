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
    };

    private static ILanguage Language(string id) => id switch
    {
        "csharp" => Languages.CSharp,
        "html" => Languages.Html,
        "sql" => Languages.Sql,
        _ => throw new System.ArgumentOutOfRangeException(nameof(id)),
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void SegmentFormatter_RendersSameBuffer_AsMarkupFormatter(string languageId, string source)
    {
        var language = Language(languageId);
        var theme = SyntaxTheme.CreateDefault();
        var options = new SyntaxOptions { TabWidth = 0 };

        var markupBuffer = Render(console =>
            console.Markup(new SpectreMarkupFormatter().Format(source, language, theme, options)), source);
        var segmentBuffer = Render(console =>
            console.Write(new SpectreSegmentFormatter().Format(source, language, theme, options)), source);

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
    private static ConsoleBuffer Render(System.Action<AnsiConsoleBuffer> write, string source)
    {
        var buffer = new ConsoleBuffer { Size = new ConsoleGUI.Space.Size(100, 40) };
        buffer.Initialize();
        var console = new AnsiConsoleBuffer(buffer) { wrap = true };
        console.Profile.Width = LongestLineWidth(source) + 1;
        write(console);
        return buffer;
    }

    private static int LongestLineWidth(string text)
    {
        int max = 1, col = 0;
        foreach (var c in text)
        {
            if (c == '\n') { if (col > max) max = col; col = 0; }
            else if (c != '\r') col++;
        }
        return System.Math.Max(max, col);
    }
}
