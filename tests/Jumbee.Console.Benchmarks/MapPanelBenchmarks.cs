namespace Jumbee.Console.Benchmarks;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Space;

using Jumbee.Console;
using Jumbee.Console.Drawing;

using CCharacter = ConsoleGUI.Data.Character;
using CColor = ConsoleGUI.Data.Color;

/// <summary>How the map's refresh is written — the three configurations measured.</summary>
public enum MapMode
{
    /// <summary>What the dashboard did before: <c>Clear()</c> + re-add the coastline, whole panel reported dirty.</summary>
    RebuildNoTracking,

    /// <summary>Still rebuilds the layers, but reports only the changed cells (<see cref="Canvas.DamageTracking"/>).</summary>
    RebuildTracked,

    /// <summary>What it does now: coastline added once, <c>ClearLabels()</c> per refresh, plus damage tracking.</summary>
    KeepCoastlineTracked,
}

/// <summary>
/// The MonitorDashboard's live outage map: a full-width Canvas holding a braille world coastline plus a handful of
/// outage markers with rich markup city labels, refreshed as outages come and go. One op = one refresh frame
/// (rebuild + paint + composite) through the real compositor, headless.
/// <para>
/// <see cref="MapPanelDiagnostics.Diagnose"/> (<c>-- --diag</c>) measures the same three configurations but splits
/// each frame into render vs composite, which is what shows that damage tracking and <c>ClearLabels</c> fix
/// different halves. This class is the rigorous whole-frame number.
/// </para>
/// </summary>
[MemoryDiagnoser]
public class MapPanelBenchmarks
{
    [Params(MapMode.RebuildNoTracking, MapMode.RebuildTracked, MapMode.KeepCoastlineTracked)]
    public MapMode Mode;

    private Canvas _map = null!;
    private int _tick;

    [GlobalSetup]
    public void Setup() => _map = MapPanelDiagnostics.NewSession(Mode);

    [Benchmark]
    public void MapRefreshFrame() => MapPanelDiagnostics.RefreshFrame(_map, Mode, ++_tick);
}

/// <summary>The render-vs-composite split behind <see cref="MapPanelBenchmarks"/>, and the shared workload.</summary>
public static class MapPanelDiagnostics
{
    private const int W = 120, H = 20;

    /// <summary>
    /// Measures ONE mode and prints its render/composite split — the part BenchmarkDotNet can't show, since it times
    /// the whole op. Run via <c>-- --diag &lt;mode&gt;</c>, one mode per process.
    /// <para>
    /// One mode per process on purpose: measuring all three in a single run made whichever ran last look good, because
    /// the JIT was fully warmed by then. That bias inflated the first configuration and is what made an earlier version
    /// of this harness disagree with BDN by ~2x. Trust <see cref="MapPanelBenchmarks"/> for absolute times; this is
    /// for the split.
    /// </para>
    /// </summary>
    public static void Diagnose(string? mode)
    {
        if (!Enum.TryParse<MapMode>(mode, ignoreCase: true, out var m))
        {
            Console.WriteLine($"usage: -- --diag <{string.Join('|', Enum.GetNames<MapMode>())}>");
            return;
        }

        var r = Measure(m);
        Console.WriteLine($"{m,-22} render {r.Paint,7:F1}us   composite {r.Draw,7:F1}us   total {r.Total,7:F1}us   " +
                          $"cells {r.Cells,4}/{(long)W * H}");
    }

    // Rebuilds the canvas the way MonitorDashboardExample.RedrawMap used to: clear everything, re-add the static
    // coastline, then a Dot layer of outage markers with markup labels. Clearing the shapes forces the whole layer
    // raster — coastline included — to be rebuilt on the next render.
    private static void Redraw(Canvas map, int tick)
    {
        map.Clear();
        map.Add(new WorldMap(new CColor(60, 95, 85), MapResolution.High));
        map.Layer(CanvasMarker.Dot);
        for (int i = 0; i < 5; i++)
        {
            var (lon, lat, name) = Cities[(tick + i * 5) % Cities.Length];
            map.Add(new Points([(lon, lat)], new CColor(240, 80, 80)));
            map.PrintMarkup(lon + 3, lat, Label(name));
        }
    }

    // The same refresh leaving the coastline alone: only labels are replaced, so the cached layers survive. The
    // outage marker becomes a "dot" label rather than a Points shape, since changing any shape invalidates the raster.
    private static void RedrawLabelsOnly(Canvas map, int tick)
    {
        map.ClearLabels();
        for (int i = 0; i < 5; i++)
        {
            var (lon, lat, name) = Cities[(tick + i * 5) % Cities.Length];
            map.Print(lon, lat, "•", new CColor(240, 80, 80));
            map.PrintMarkup(lon + 3, lat, Label(name));
        }
    }

    private static string Label(string name) => $"[red]⚠[/] [white]{name}[/] [grey62]fiber cut[/]";

    private static readonly (double Lon, double Lat, string Name)[] Cities =
    [
        (-74.0, 40.7, "New York"), (-0.13, 51.5, "London"), (139.7, 35.7, "Tokyo"),
        (151.2, -33.9, "Sydney"), (2.35, 48.85, "Paris"), (37.6, 55.75, "Moscow"),
        (77.2, 28.6, "Delhi"), (-46.6, -23.5, "Sao Paulo"), (103.8, 1.35, "Singapore"),
        (-118.2, 34.05, "Los Angeles"), (55.3, 25.2, "Dubai"), (18.4, -33.9, "Cape Town"),
    ];

    private readonly record struct Result(double Paint, double Draw, long Cells)
    {
        public double Total => Paint + Draw;
    }

    /// <summary>Builds the canvas for a mode and starts a headless compositor session on it.</summary>
    public static Canvas NewSession(MapMode mode)
    {
        var map = new Canvas { Height = H, DamageTracking = mode != MapMode.RebuildNoTracking };
        map.WithMarker(CanvasMarker.Braille).WithXBounds(-180, 180).WithYBounds(-90, 90);
        if (mode == MapMode.KeepCoastlineTracked)
        {
            map.Add(new WorldMap(new CColor(60, 95, 85), MapResolution.High));   // added once, never cleared
            RedrawLabelsOnly(map, 0);
        }
        else Redraw(map, 0);
        StartSession(map);
        return map;
    }

    /// <summary>One refresh: update the canvas for this mode, then paint and composite.</summary>
    public static void RefreshFrame(Canvas map, MapMode mode, int tick)
    {
        if (mode == MapMode.KeepCoastlineTracked) RedrawLabelsOnly(map, tick); else Redraw(map, tick);
        UI.PaintFrame();
        ConsoleManager.Draw();
    }

    private static Result Measure(MapMode mode)
    {
        var map = NewSession(mode);

        const int warmup = 2000, n = 500;
        var paint = new List<double>(n);
        var draw = new List<double>(n);
        var cells = new List<long>(n);
        var sw = new Stopwatch();
        for (int i = 1; i <= warmup + n; i++)
        {
            if (mode == MapMode.KeepCoastlineTracked) RedrawLabelsOnly(map, i); else Redraw(map, i);
            sw.Restart(); UI.PaintFrame(); sw.Stop();
            var p = sw.Elapsed.TotalMicroseconds;
            sw.Restart(); ConsoleManager.Draw(); sw.Stop();
            var d = sw.Elapsed.TotalMicroseconds;
            if (i <= warmup) continue;
            paint.Add(p); draw.Add(d); cells.Add(ConsoleManager.LastFrameDirtyCells);
        }
        paint.Sort(); draw.Sort(); cells.Sort();
        return new Result(paint[n / 2], draw[n / 2], cells[n / 2]);
    }

    // The real compositor, headless: a no-op console + no-op ANSI sink, so we measure paint + composite without I/O.
    private static void StartSession(IControl content)
    {
        ConsoleManager.AnsiEnabled = true;
        ConsoleManager.AnsiOutput = _ => Task.CompletedTask;
        ConsoleManager.Console = new NullConsole { Size = new Size(W, H) };
        ConsoleManager.Setup();
        ConsoleManager.Content = content;
        UI.PaintFrame();
        ConsoleManager.Draw();
    }

    private sealed class NullConsole : IConsole
    {
        public Size Size { get; set; }
        public bool KeyAvailable => false;
        public void Initialize() { }
        public void OnRefresh() { }
        public void Write(Position position, in CCharacter character) { }
        public ConsoleKeyInfo ReadKey() => throw new NotSupportedException();
    }
}
