namespace Jumbee.Console.Tests;

using System.Threading.Tasks;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>
/// Wide (2-cell) glyphs — CJK, fullwidth forms — must survive the frame's content measurement. A scrolling
/// <see cref="ControlFrame"/> leaves the height unbounded, so it calls <c>MeasureHeight</c>, which word-wraps the
/// content; on the first layout pass the available width is 0, so the wrap runs at maxWidth 1. A glyph wider than
/// that can't be split, and the fold used to emit an empty slice and re-queue the glyph forever (an infinite loop
/// that hung the whole app on any framed CJK text). Each test is time-boxed so a regression FAILS instead of hanging
/// the suite.
/// </summary>
public class WideGlyphLayoutTests
{
    private const int TimeoutMs = 10_000;

    private static void Within(string because, Func<object?> act)
        => Assert.True(Task.Run(act).Wait(TimeoutMs), because);

    [Theory]
    [InlineData("中文")]              // CJK — 2 cells each
    [InlineData("＋ New")]            // U+FF0B fullwidth plus
    [InlineData("日本語のテキスト")]   // longer CJK run
    [InlineData("mixed 中 text")]     // wide glyph among ASCII
    public void FramingControlWithWideGlyphs_DoesNotHang(string markup)
        => Within($"framing a control whose text contains wide glyphs hung: {markup}",
            () => new TextPanel(markup).WithSize(10, 1).WithFrame(borderStyle: BorderStyle.Rounded));

    [Fact]
    public void MeasuringWideGlyphs_AtOneCellWidth_Terminates()
    {
        // The exact degenerate case: a 2-cell glyph can never fit a 1-cell line. It must be emitted anyway (on its
        // own line) rather than split into nothing and retried.
        var panel = new TextPanel("中文");
        Within("rendering wide glyphs into a 1-column frame hung", () => ConsoleSnapshot.ToText(panel, 3, 4));
    }

    [Fact]
    public void FramedWideGlyphs_StillRender()
    {
        var panel = new TextPanel("中文").WithFrame(borderStyle: BorderStyle.Rounded);
        string text = null!;
        Within("rendering framed wide glyphs hung", () => text = ConsoleSnapshot.ToText(panel, 12, 4));

        Assert.Contains("中", text);
        Assert.Contains("文", text);
    }

    [Fact]
    public void AsciiFraming_StillWorks()   // guards the fix didn't change the normal path
    {
        var panel = new TextPanel("hello").WithFrame(borderStyle: BorderStyle.Rounded);
        string text = null!;
        Within("framing ASCII text hung", () => text = ConsoleSnapshot.ToText(panel, 12, 4));

        Assert.Contains("hello", text);
    }

    // The same hang reached every framed control carrying arbitrary (user/document) text, not just TextPanel — these
    // are the ones that would have broken on any non-Latin content.
    //
    // NOTE on the assertions: a 2-cell glyph occupies cell N and leaves cell N+1 as a spacer, so the snapshot's
    // cell grid reads "日 本 語", not "日本語" — assert per glyph. (Whether the renderer should SKIP that spacer so
    // wide text stays aligned on a real terminal is a separate, pre-existing gap; these tests only pin the hang.)
    [Fact]
    public void FramedDataTable_WithCjk_Renders()
    {
        var table = new DataTable("名前", "値");
        table.AddRow("日本語", "テスト");
        table.WithFrame(borderStyle: BorderStyle.Rounded);
        string text = null!;
        Within("framed DataTable with CJK hung", () => text = ConsoleSnapshot.ToText(table, 30, 8));

        Assert.Contains("日", text);
    }

    [Fact]
    public void FramedMarkdownViewer_WithCjk_Renders()
    {
        var viewer = new MarkdownViewer("# 見出し\n\n本文のテキストです。").WithFrame(borderStyle: BorderStyle.Rounded);
        Within("framed MarkdownViewer with CJK hung", () => ConsoleSnapshot.ToText(viewer, 30, 10));
    }

    [Fact]
    public void FramedListBox_WithCjk_Renders()
    {
        var list = new ListBox("日本語", "中文", "한국어").WithFrame(borderStyle: BorderStyle.Rounded);
        string text = null!;
        Within("framed ListBox with CJK hung", () => text = ConsoleSnapshot.ToText(list, 20, 8));

        Assert.Contains("中", text);
    }
}
