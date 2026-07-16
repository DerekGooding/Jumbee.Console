namespace Jumbee.Console.Tests;

using System.Threading.Tasks;

using ConsoleGUI;

using Jumbee.Console;
using Jumbee.Console.Drawing;
using Jumbee.Console.Snapshot;

using Xunit;
using Xunit.Abstractions;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// <see cref="Canvas.DamageTracking"/>: a canvas whose content is mostly static (a world map) but whose few markers
/// and labels change should re-composite only the cells that moved. The dashboard's live outage map is exactly this
/// shape, so these use it as the workload.
/// </summary>
public class CanvasDamageTests
{
    private readonly ITestOutputHelper _out;
    public CanvasDamageTests(ITestOutputHelper output)
    {
        _out = output;
        UiTestHarness.EnsureStopped();
    }

    private static readonly (double Lon, double Lat, string Name)[] Cities =
        [(-74.0, 40.7, "New York"), (139.7, 35.7, "Tokyo"), (2.35, 48.85, "Paris"), (151.2, -33.9, "Sydney")];

    // Rebuilds the canvas the way the dashboard does: a static coastline, then outage dots + markup labels that move.
    private static Canvas Map(bool track)
    {
        var map = new Canvas { DamageTracking = track };
        map.WithMarker(CanvasMarker.Braille).WithXBounds(-180, 180).WithYBounds(-90, 90);
        return map;
    }

    private static void Redraw(Canvas map, int tick)
    {
        map.Clear();
        map.Add(new WorldMap(new CColor(60, 95, 85), MapResolution.High));
        map.Layer(CanvasMarker.Dot);
        var (lon, lat, name) = Cities[tick % Cities.Length];
        map.Add(new Points([(lon, lat)], new CColor(240, 80, 80)));
        map.PrintMarkup(lon + 3, lat, $"[red]⚠[/] [white]{name}[/] [grey62]fiber cut[/]");
    }

    [Fact]
    public async Task MovingLabels_RecompositeOnlyTheChangedCells()
    {
        const int w = 120, h = 20;
        var map = Map(track: true);
        Redraw(map, 0);
        using var session = await AnsiConsoleSession.StartAsync(map, w, h);

        Redraw(map, 1);   // the label + dot move to another city; the coastline is unchanged
        await session.FrameAsync();

        var dirty = ConsoleManager.LastFrameDirtyCells;
        _out.WriteLine($"canvas opt-in dirty cells: {dirty} of {(long)w * h}");
        Assert.True(dirty > 0, "the moved label must report something");
        Assert.True(dirty < (long)w * h / 4, $"expected a small partial redraw, got {dirty} of {(long)w * h}");
    }

    [Fact]
    public async Task Default_Off_RecompositesTheWholeCanvas()
    {
        const int w = 120, h = 20;
        var map = Map(track: false);
        Redraw(map, 0);
        using var session = await AnsiConsoleSession.StartAsync(map, w, h);

        Redraw(map, 1);
        await session.FrameAsync();

        Assert.Equal((long)w * h, ConsoleManager.LastFrameDirtyCells);
    }

    // The one failure mode that matters: under-reporting silently drops updates. Tracking must not change a pixel.
    [Fact]
    public async Task Tracking_RendersIdenticallyToFullRedraw()
    {
        const int w = 120, h = 20;

        string tracked, full;
        var a = Map(track: true);
        Redraw(a, 0);
        using (var session = await AnsiConsoleSession.StartAsync(a, w, h))
        {
            for (var tick = 1; tick <= 3; tick++) { Redraw(a, tick); await session.FrameAsync(); }
            tracked = ConsoleSnapshot.ToText(session.Screen.Buffer);
        }

        var b = Map(track: false);
        Redraw(b, 0);
        using (var session = await AnsiConsoleSession.StartAsync(b, w, h))
        {
            for (var tick = 1; tick <= 3; tick++) { Redraw(b, tick); await session.FrameAsync(); }
            full = ConsoleSnapshot.ToText(session.Screen.Buffer);
        }

        Assert.Equal(full, tracked);
    }
}
