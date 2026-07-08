namespace Jumbee.Console.Tests;

using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class GaugeTests
{
    public GaugeTests() => UiTestHarness.EnsureStopped();

    // The default fill colour (GaugeStyle.Default) — a cell belongs to the filled band when its background is this.
    private const byte FillRed = 90;

    private static int FillCells(Gauge g, int width)
    {
        var n = 0;
        for (var x = 0; x < width; x++)
            if (g[new Position(x, 0)].Character.Background?.Red == FillRed) n++;
        return n;
    }

    [Fact]
    public void Gauge_ShowsPercent_AndOptionalValue()
    {
        var g = new Gauge(126, 365) { ShowValue = true };
        var text = ConsoleSnapshot.ToText(g, 40, 1);

        Assert.Contains("34.5%", text);   // 126 / 365
        Assert.Contains("(126)", text);
    }

    [Fact]
    public void Gauge_FillsProportionalToValue()
    {
        var g = new Gauge(50, 100);   // half full
        ConsoleSnapshot.Render(g, 30, 1);

        // Bar width = 30 − " 50.0%" (6 text cells) = 24; half ≈ 12 filled cells.
        var fill = FillCells(g, 30);
        Assert.InRange(fill, 10, 14);
    }

    [Fact]
    public void Gauge_Empty_HasNoFill()
    {
        var g = new Gauge(0, 100) { ShowPercent = false };
        ConsoleSnapshot.Render(g, 20, 1);
        Assert.Equal(0, FillCells(g, 20));
    }

    [Fact]
    public void Gauge_Full_FillsEntireBar()
    {
        var g = new Gauge(100, 100) { ShowPercent = false };
        ConsoleSnapshot.Render(g, 20, 1);
        Assert.Equal(20, FillCells(g, 20));   // no text reserved -> the whole width is the bar
    }

    [Fact]
    public void Gauge_OverAndUnderRange_Clamp()
    {
        var over = new Gauge(150, 100) { ShowPercent = false };
        ConsoleSnapshot.Render(over, 20, 1);
        Assert.Equal(20, FillCells(over, 20));   // clamps to full

        var under = new Gauge(-5, 100) { ShowPercent = false };
        ConsoleSnapshot.Render(under, 20, 1);
        Assert.Equal(0, FillCells(under, 20));    // clamps to empty
    }

    [Fact]
    public void Gauge_Label_IsDrawnBeforeTheBar()
    {
        var g = new Gauge(50, 100) { Label = "CPU" };
        var text = ConsoleSnapshot.ToText(g, 30, 1);
        Assert.StartsWith("CPU", text.TrimEnd());
    }

    [Fact]
    public void Gauge_WithFill_RecoloursTheBand()
    {
        var g = new Gauge(100, 100) { ShowPercent = false }.WithFill(new Color(200, 40, 40));
        ConsoleSnapshot.Render(g, 10, 1);
        Assert.Equal((byte?)200, g[new Position(0, 0)].Character.Background?.Red);
    }
}
