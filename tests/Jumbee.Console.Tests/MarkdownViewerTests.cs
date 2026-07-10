namespace Jumbee.Console.Tests;

using ConsoleGUI.Input;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class MarkdownViewerTests
{
    private static ConsoleKeyInfo K(ConsoleKey k) => new('\0', k, false, false, false);

    [Fact]
    public void RendersHeadingAndInlineText()
    {
        var view = new MarkdownViewer("# Title\n\nSome **bold** and *italic* text.");

        var text = ConsoleSnapshot.ToText(view, 40, 12);

        Assert.Contains("Title", text);
        Assert.Contains("bold", text);
        Assert.Contains("italic", text);
    }

    [Fact]
    public void RendersTableWithBoxDrawingGlyphs()
    {
        var md = "| Name | Score |\n|------|-------|\n| Alice | 10 |\n| Bob | 7 |\n";
        var view = new MarkdownViewer(md);

        var text = ConsoleSnapshot.ToText(view, 40, 12);

        Assert.Contains("Alice", text);
        Assert.Contains("Bob", text);
        // The table is drawn with box-drawing characters (not raw pipes/dashes) — the point of the viewer.
        Assert.True(text.IndexOfAny(['│', '─', '┌', '┐', '└', '┘', '├', '┤', '┬', '┴', '┼']) >= 0,
            "the markdown table should render with box-drawing glyphs");
    }

    [Fact]
    public void RendersUnorderedList()
    {
        var view = new MarkdownViewer("- alpha\n- beta\n- gamma\n");

        var text = ConsoleSnapshot.ToText(view, 40, 12);

        Assert.Contains("alpha", text);
        Assert.Contains("beta", text);
        Assert.Contains("gamma", text);
    }

    [Fact]
    public void RendersHorizontalRule_UsingConsoleWidth_NotSystemConsole()
    {
        // A horizontal rule spans the console's own (Profile) width. Before the writer used Profile.Width it read
        // System.Console.WindowWidth, which throws headless — so this both proves the fix and that a rule renders.
        var view = new MarkdownViewer("Above\n\n---\n\nBelow");

        var text = ConsoleSnapshot.ToText(view, 24, 12);

        Assert.Contains("Above", text);
        Assert.Contains("Below", text);
        Assert.Contains('─', text);   // the rule line renders (and doesn't throw)
    }

    [Fact]
    public void Framed_MeasuresContentTallerThanViewport_SoItScrolls()
    {
        // Many lines -> the rendered content is taller than the small viewport, so the frame can scroll it.
        var lines = new string[40];
        for (var i = 0; i < lines.Length; i++) lines[i] = $"Line number {i + 1}";
        var view = new MarkdownViewer(string.Join("\n\n", lines));
        view.WithRoundedBorder();

        ConsoleSnapshot.Render(view, 30, 8);

        Assert.True(view.Frame!.ViewportSize.Height > 0);
        // Content sized taller than the ~6-row viewport -> the frame has somewhere to scroll.
        Assert.True(view.Size.Height > view.Frame!.ViewportSize.Height,
            $"content ({view.Size.Height}) should exceed the viewport ({view.Frame!.ViewportSize.Height})");
    }

    [Fact]
    public void ArrowAndPageKeys_ScrollTheFrame()
    {
        var lines = new string[60];
        for (var i = 0; i < lines.Length; i++) lines[i] = $"Paragraph {i + 1}";
        var view = new MarkdownViewer(string.Join("\n\n", lines));
        view.WithRoundedBorder();
        ConsoleSnapshot.Render(view, 30, 8);
        Assert.Equal(0, view.Frame!.Top);

        UI.SendInput(view, K(ConsoleKey.DownArrow));
        Assert.True(view.Frame!.Top > 0);          // a line down

        var afterDown = view.Frame!.Top;
        UI.SendInput(view, K(ConsoleKey.PageDown));
        Assert.True(view.Frame!.Top > afterDown);  // a page further down

        UI.SendInput(view, K(ConsoleKey.Home));
        Assert.Equal(0, view.Frame!.Top);          // back to the top
    }
}
