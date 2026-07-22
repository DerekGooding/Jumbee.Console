namespace Jumbee.Console.Tests;

using System;
using System.Linq;

using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Screen-anchored axis captions (<c>ConfigureAxis</c> XTitle/YTitle): a Y caption pins to the top-left and an
/// X caption to the bottom-right, and — unlike a data-anchored label — they do NOT move when the axes rescale. Added
/// for the scope-tui port, whose "time"/"amplitude" captions must stay put while the user zooms the view.</summary>
public class PlotAxisTitleTests
{
    private static (int row, int col) Locate(string[] lines, string needle)
    {
        var row = Array.FindIndex(lines, l => l.Contains(needle));
        return (row, row < 0 ? -1 : lines[row].IndexOf(needle, StringComparison.Ordinal));
    }

    private static string[] Draw(Plot p) => ConsoleSnapshot.ToText(p, 40, 12).Split('\n');

    [Fact]
    public void AxisTitles_AreScreenAnchored_AndSurviveClear()
    {
        var p = new Plot();
        p.ConfigureGrid(g => g.IsVisible = false)
         .ConfigureTicks(t => { t.IsVisible = false; t.Labels.IsVisible = false; })
         .ConfigureAxis(a => { a.XTitle = "time"; a.YTitle = "amp"; });
        p.AddSeries([0.0, 1, 2, 3], [0.0, 1, 0, -1], PlotBrush.Braille);

        var lines = Draw(p);
        var amp = Locate(lines, "amp");
        var time = Locate(lines, "time");
        Assert.True(amp.row >= 0 && time.row >= 0, "both captions render");
        Assert.True(amp.row < time.row, "Y caption sits above the X caption");
        Assert.Equal(0, amp.col);                       // Y caption pinned to the left edge
        Assert.True(time.col > amp.col, "X caption is right-of the Y caption (bottom-right)");

        // Screen-anchored: a huge X rescale must NOT move either caption's row or column.
        p.SetXRange(0, 10000);
        var rescaled = Draw(p);
        Assert.Equal(amp, Locate(rescaled, "amp"));
        Assert.Equal(time, Locate(rescaled, "time"));

        // Chrome persistence: captions survive the per-frame Clear() + re-add loop.
        p.Clear();
        p.AddSeries([0.0, 1, 2, 3], [0.0, 1, 0, -1], PlotBrush.Braille);
        var afterClear = Draw(p);
        Assert.True(Locate(afterClear, "amp").row >= 0 && Locate(afterClear, "time").row >= 0,
            "captions persist across Clear()");
    }
}
