namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

using CColor = ConsoleGUI.Data.Color;

public class RunChartTests
{
    public RunChartTests() => UiTestHarness.EnsureStopped();

    [Fact]
    public void RunChart_Legend_ShowsNameAndStats()
    {
        var chart = new RunChart();
        var s = chart.AddSeries("BING", new CColor(235, 90, 90));
        s.Push(0.20);
        s.Push(0.25);   // cur 0.25, dlt +0.05, max 0.25, min 0.20

        var text = ConsoleSnapshot.ToText(chart, 60, 12);
        Assert.Contains("BING", text);
        Assert.Contains("cur", text);
        Assert.Contains("0.25", text);   // current / max
        Assert.Contains("0.05", text);   // delta
        Assert.Contains("0.2", text);    // min
    }

    [Fact]
    public void RunChart_TracksNegativeDelta()
    {
        var chart = new RunChart();
        var s = chart.AddSeries("LAT", new CColor(230, 200, 90));
        s.Push(10);
        s.Push(4);   // dlt = -6, min = 4, max = 10

        var text = ConsoleSnapshot.ToText(chart, 60, 12);
        Assert.Contains("-6", text);   // negative delta
        Assert.Contains("10", text);   // max
    }

    [Fact]
    public void RunChart_MultipleSeries_EachGetALegendEntry()
    {
        var chart = new RunChart();
        var a = chart.AddSeries("AAA", new CColor(235, 90, 90));
        var b = chart.AddSeries("BBB", new CColor(90, 160, 240));
        a.Push(1); a.Push(2);
        b.Push(5); b.Push(3);

        var text = ConsoleSnapshot.ToText(chart, 60, 14);
        Assert.Contains("AAA", text);
        Assert.Contains("BBB", text);
    }

    [Fact]
    public void RunChart_StreamsManyValues_StaysStable()
    {
        var chart = new RunChart().SetYRange(0, 100).SetXWindow(40);
        var s = chart.AddSeries("TX", new CColor(120, 200, 120));
        for (var i = 0; i < 200; i++) s.Push(i % 100);   // more than the window

        // Doesn't throw, still renders, and the legend reflects the latest value.
        var text = ConsoleSnapshot.ToText(chart, 60, 12);
        Assert.Contains("TX", text);
        Assert.Contains("cur", text);
    }
}
