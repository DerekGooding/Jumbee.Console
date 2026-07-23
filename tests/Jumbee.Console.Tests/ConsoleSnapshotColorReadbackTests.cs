namespace Jumbee.Console.Tests;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary><see cref="ConsoleSnapshot.ForegroundAt"/>/<see cref="ConsoleSnapshot.BackgroundAt"/> let a test assert a
/// rendered cell's colour as a <see cref="Color"/> — which text snapshots drop — without reaching into the buffer's
/// internal ConsoleGUI cell type.</summary>
public class ConsoleSnapshotColorReadbackTests
{
    [Fact]
    public void ForegroundAt_ReadsRenderedCellColour_AsJumbeeColor()
    {
        var red = new Color(0xC0, 0x30, 0x30);
        var label = new TextLabel(TextLabelOrientation.Horizontal, "R", fgcolor: red);
        var buffer = ConsoleSnapshot.Render(label, 4, 1);

        Assert.Equal(red, ConsoleSnapshot.ForegroundAt(buffer, 0, 0));
    }

    [Fact]
    public void ForegroundAt_IsNull_ForTheTerminalDefault()
    {
        var plain = new TextLabel(TextLabelOrientation.Horizontal, "R");   // no explicit foreground
        var buffer = ConsoleSnapshot.Render(plain, 4, 1);

        Assert.Null(ConsoleSnapshot.ForegroundAt(buffer, 0, 0));
    }

    [Fact]
    public void GlyphAt_AndToLines_ReadCellsWithoutRowArithmetic()
    {
        var red = new Color(0xC0, 0x30, 0x30);
        var label = new TextLabel(TextLabelOrientation.Horizontal, "Hi", fgcolor: red);
        var buffer = ConsoleSnapshot.Render(label, 6, 1);

        // GlyphAt + ForegroundAt read the same cell — glyph and colour together, no text-index mapping.
        Assert.Equal('H', ConsoleSnapshot.GlyphAt(buffer, 0, 0));
        Assert.Equal('i', ConsoleSnapshot.GlyphAt(buffer, 1, 0));
        Assert.Equal(' ', ConsoleSnapshot.GlyphAt(buffer, 4, 0));   // empty cell → space
        Assert.Equal(red, ConsoleSnapshot.ForegroundAt(buffer, 0, 0));

        // ToLines gives right-trimmed rows; ToText is exactly those joined + terminated with '\n'.
        var lines = ConsoleSnapshot.ToLines(buffer);
        Assert.Single(lines);
        Assert.Equal("Hi", lines[0]);   // right-trimmed, no trailing padding
        Assert.Equal(string.Concat(System.Linq.Enumerable.Select(lines, l => l + '\n')), ConsoleSnapshot.ToText(buffer));
    }
}
