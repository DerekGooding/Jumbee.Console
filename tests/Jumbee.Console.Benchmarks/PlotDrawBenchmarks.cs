namespace Jumbee.Console.Benchmarks;

using BenchmarkDotNet.Attributes;

using ConsolePlot;
using ConsolePlot.Drawing.Tools;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// Measures the per-frame allocation of <see cref="ConsolePlot.Plot.Draw"/> — the ConsolePlot render hot path the
/// Jumbee <c>Plot</c> control runs every frame for a live chart. Representative dashboard workload: a fixed-axis line
/// series plus bars, with grid, axis, ticks and labels all on (the tick/label + bounds + converter machinery). Use
/// <c>--filter *PlotDrawBenchmarks*</c> and watch the <c>Allocated</c> column.
/// </summary>
[MemoryDiagnoser]
public class PlotDrawBenchmarks
{
    private Plot _line = null!;
    private Plot _bars = null!;
    private List<double> _xs = null!;
    private List<double> _ys = null!;
    private PointPen _pen;

    [Params(60)]
    public int Width;

    [Params(20)]
    public int Height;

    [GlobalSetup]
    public void Setup()
    {
        _line = BuildLine();
        _bars = BuildBars();

        _xs = new List<double>();
        _ys = new List<double>();
        for (int i = 0; i < 64; i++) { _xs.Add(i); _ys.Add(50 + 40 * System.Math.Sin(i * 0.2)); }
        _pen = new PointPen(SystemPointBrushes.Braille, new CColor(90, 160, 240));
    }

    // A streaming line chart with a pinned axis, grid and tick labels — the common live-monitor configuration.
    private Plot BuildLine()
    {
        var xs = new double[64];
        var ys = new double[64];
        for (int i = 0; i < xs.Length; i++) { xs[i] = i; ys[i] = 50 + 40 * System.Math.Sin(i * 0.2); }

        var plot = new Plot(Width, Height);
        plot.FixedXRange = (0, 63);
        plot.FixedYRange = (0, 100);
        plot.AddSeries(xs, ys, new PointPen(SystemPointBrushes.Braille, new CColor(90, 160, 240)));
        return plot;
    }

    private Plot BuildBars()
    {
        var xs = new double[12];
        var ys = new double[12];
        for (int i = 0; i < xs.Length; i++) { xs[i] = i + 1; ys[i] = 3 + (i % 7); }

        var plot = new Plot(Width, Height);
        plot.FixedYRange = (0, 11);
        plot.AddBars(xs, ys, new CColor(90, 160, 240));
        return plot;
    }

    [Benchmark(Baseline = true)]
    public void DrawLine() => _line.Draw();

    [Benchmark]
    public void DrawBars() => _bars.Draw();

    // The old per-frame cost a live Jumbee plot paid: rebuild the whole plot (new image + re-add series) then Draw.
    // The gap between this and DrawLine (a plain redraw of the reused plot) is what the no-rebuild change saves per
    // frame — the underlying series now alias the live buffers, so a data update only redraws.
    [Benchmark]
    public void RebuildAndDraw()
    {
        var plot = new Plot(Width, Height);
        plot.FixedXRange = (0, 63);
        plot.FixedYRange = (0, 100);
        plot.AddSeries(_xs, _ys, _pen);
        plot.Draw();
    }
}
