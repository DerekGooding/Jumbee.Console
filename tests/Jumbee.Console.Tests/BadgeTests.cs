namespace Jumbee.Console.Tests;

using ConsoleGUI;
using ConsoleGUI.Common;
using ConsoleGUI.Space;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Headless tests for <see cref="Badge"/>: text + padding rendering, a filled themed background, and sizing.</summary>
public class BadgeTests
{
    private sealed class NoopListener : IDrawingContextListener
    {
        public void OnRedraw(DrawingContext d) { }
        public void OnUpdate(DrawingContext d, Rect r) { }
    }

    // Size under loose limits (min 0, max given) so a fixed-width control sizes to its content, as it would inside
    // a real layout — ConsoleSnapshot.Render pins min==max, which forces a root control to fill the width.
    private static void SizeLoose(RenderableControl c, int maxW, int maxH)
    {
        var ctx = new DrawingContext(new NoopListener(), (IControl)c.FocusableControl);
        ctx.SetLimits(new Size(0, 0), new Size(maxW, maxH));
        UI.PaintFrame();
    }

    [Fact]
    public void Renders_TextWithPadding()
    {
        var b = new Badge("OK", BadgeVariant.Success);
        Assert.Contains("OK", ConsoleSnapshot.ToText(b, 20, 1));
    }

    [Fact]
    public void SizesTo_TextPlusPadding()
    {
        var b = new Badge("OK");   // 2 chars + 1 padding each side
        SizeLoose(b, 40, 1);
        Assert.Equal(4, b.ActualWidth);
        Assert.Equal(1, b.ActualHeight);
    }

    [Fact]
    public void FilledVariant_HasBackground()
    {
        var b = new Badge("OK", BadgeVariant.Success);
        var buf = ConsoleSnapshot.Render(b, 20, 1);
        Assert.NotNull(buf[1, 0].Background);   // the 'O' cell is filled
    }

    [Fact]
    public void Padding_Zero_IsTight()
    {
        var b = new Badge("OK") { Padding = 0 };
        SizeLoose(b, 40, 1);
        Assert.Equal(2, b.ActualWidth);
    }

    [Fact]
    public void ExplicitStyle_Renders()
        => Assert.Contains("X", ConsoleSnapshot.ToText(new Badge("X", Style.Bold), 10, 1));
}
