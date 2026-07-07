namespace Jumbee.Console.Examples;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Jumbee.Console;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// A live "ops" dashboard: a 3×3 grid of framed panels — streaming line charts, a live bar chart, a process table,
/// a scrolling log, throughput sparklines and a progress gauge — all updated a few times a second from a simulated
/// data feed. Demonstrates <b>subplots</b> (compose <see cref="Plot"/> and other controls in a <see cref="Grid"/>)
/// and <b>live data</b> (the <see cref="PlotSeries"/> handles from <c>AddLiveSeries</c>/<c>AddLiveBars</c>). The feed
/// runs on the UI thread off the <see cref="UI.Paint"/> tick, so no marshalling is needed here.
/// </summary>
public sealed class LiveDashboardExample : CompositeControl, IExample, IActivatable
{
    public LiveDashboardExample()
    {
        // Both axes are pinned so the panels are a stationary frame the data scrolls through (a real-time monitor):
        // Y to the value range, X to the strip width. The series feed with Scroll(value, window) — fixed x positions
        // 0..window-1 — so the x axis never grows/shifts (unlike an ever-increasing time counter).
        var txPlot = new Plot();
        _txUsa = txPlot.AddLiveSeries(new CColor(235, 90, 90));
        _txEu = txPlot.AddLiveSeries(new CColor(230, 200, 90));
        txPlot.ConfigureGrid(g => g.IsVisible = false);
        txPlot.SetYRange(0, 100);
        txPlot.SetXRange(0, Window - 1);

        var errPlot = new Plot();
        _errors = errPlot.AddLiveSeries(new CColor(235, 90, 90));
        errPlot.ConfigureGrid(g => g.IsVisible = false);
        errPlot.SetYRange(0, 55);
        errPlot.SetXRange(0, Window - 1);

        var utilPlot = new Plot();
        _util = utilPlot.AddLiveBars(new CColor(90, 160, 240));
        utilPlot.ConfigureGrid(g => g.IsVisible = false);
        utilPlot.ConfigureAxis(a => a.IsVisible = false);   // bars carry their own baseline; keep the panel clean
        utilPlot.SetYRange(0, 11);
        utilPlot.SetXTicks([(1, "US1"), (2, "US2"), (3, "EU1"), (4, "AU1"), (5, "AS1"), (6, "JP1")]);
        utilPlot.ConfigureTicks(t => t.Labels.AttachToAxis = false);

        var latPlot = new Plot();
        _latency = latPlot.AddLiveSeries(new CColor(230, 200, 90));
        latPlot.ConfigureGrid(g => g.IsVisible = false);
        latPlot.SetYRange(0, 14);
        latPlot.SetXRange(0, LatWindow - 1);

        _procs = new DataTable("Process", "Cpu%", "Mem");
        _log = new Log();
        _tp1 = new Sparkline(_tp1Data);
        _tp2 = new Sparkline(_tp2Data);
        _deploy = new TextLabel(TextLabelOrientation.Horizontal, "", new CColor(200, 130, 230));

        Seed();

        var grid = new Grid([15, 12, 6], [40, 32, 32],
        [
            [txPlot.WithFrame(BorderStyle.Rounded, title: "Total Transactions"),
             errPlot.WithFrame(BorderStyle.Rounded, title: "Errors Rate"),
             utilPlot.WithFrame(BorderStyle.Rounded, title: "Server Utilization")],
            [_procs.WithFrame(BorderStyle.Rounded, title: "Active Processes"),
             _log.WithFrame(BorderStyle.Rounded, title: "Server Log"),
             latPlot.WithFrame(BorderStyle.Rounded, title: "Network Latency")],
            [_deploy.WithFrame(BorderStyle.Rounded, title: "Deployment Progress"),
             _tp1.WithFrame(BorderStyle.Rounded, title: "Throughput S1"),
             _tp2.WithFrame(BorderStyle.Rounded, title: "Throughput S2")],
        ]);

        SetContent(grid);
    }

    #region Live feed
    // Start/stop the feed as the example is shown/hidden (called by ExampleHost). The feed is a wall-clock timer that
    // POSTS each update onto the UI thread: a posted update runs at frame start, so its redraw is requested before
    // the frame's paint — which the frame loop composites even though the deeply-nested plot panels don't localize
    // their own damage. (Driving this from the per-frame Paint event instead leaves the redraw request "mid-paint",
    // which the loop defers and never composites until some input forces a full redraw.)
    public void OnActivated()
    {
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(50, ct); }
                catch (TaskCanceledException) { break; }
                UI.Post(Advance);
            }
        });
    }

    public void OnDeactivated()
    {
        _cts?.Cancel();
        _cts = null;
    }

    // Advance the simulation and push it into the panels. Runs on the UI thread (posted), so control updates are direct.
    private void Advance()
    {
        if (Size.Width <= 0 || Size.Height <= 0) return;
        int t = _tick++;   // a running tick, used only for log timestamps

        _txUsaV = Walk(_txUsaV, 30, 90, 12);
        _txEuV = Walk(_txEuV, 10, 45, 8);
        _errV = Walk(_errV, 2, 50, 10);
        _latV = Walk(_latV, 1, 12, 3);

        // Scroll into the fixed-width strip charts — the x axis stays put, the data flows through it.
        _txUsa.Scroll(_txUsaV, Window);
        _txEu.Scroll(_txEuV, Window);
        _errors.Scroll(_errV, Window);
        _latency.Scroll(_latV, LatWindow);

        _util.SetValues([.. Enumerable.Range(0, 6).Select(_ => (double)_rng.Next(1, 11))]);

        UpdateProcesses();

        Shift(_tp1Data, _rng.Next(2, 9));
        _tp1.Values = (double[])_tp1Data.Clone();
        Shift(_tp2Data, _rng.Next(1, 8));
        _tp2.Values = (double[])_tp2Data.Clone();

        _deployPct = (_deployPct + _rng.Next(0, 4)) % 101;
        _deploy.Text = Progress(_deployPct);

        if (t % 4 == 0) _log.Write($"[grey]t{t}[/] avg wait {_rng.NextDouble():0.00}s");
    }

    private void UpdateProcesses()
    {
        _procs.Clear();
        foreach (var name in _procNames)
            _procs.AddRow(name, _rng.Next(0, 6).ToString(), _rng.Next(15, 96).ToString());
    }

    // Populate the strip charts with an initial history so the dashboard looks live the instant it opens.
    private void Seed()
    {
        for (int i = 0; i < Window; i++)
        {
            _txUsaV = Walk(_txUsaV, 30, 90, 12);
            _txEuV = Walk(_txEuV, 10, 45, 8);
            _errV = Walk(_errV, 2, 50, 10);
            _txUsa.Scroll(_txUsaV, Window);
            _txEu.Scroll(_txEuV, Window);
            _errors.Scroll(_errV, Window);
            _latency.Scroll(_latV = Walk(_latV, 1, 12, 3), LatWindow);
        }
        _util.SetValues([5, 5, 1, 9, 7, 10]);
        for (int i = 0; i < _tp1Data.Length; i++) { _tp1Data[i] = _rng.Next(2, 9); _tp2Data[i] = _rng.Next(1, 8); }
        _tp1.Values = (double[])_tp1Data.Clone();
        _tp2.Values = (double[])_tp2Data.Clone();
        _deploy.Text = Progress(_deployPct = 14);
        UpdateProcesses();
        _log.Write("[green]OK[/] dashboard started");
    }

    private double Walk(double v, double lo, double hi, double step) =>
        Math.Clamp(v + (_rng.NextDouble() - 0.5) * step, lo, hi);

    private static void Shift(double[] a, double v)
    {
        Array.Copy(a, 1, a, 0, a.Length - 1);
        a[^1] = v;
    }

    private static string Progress(int pct)
    {
        const int n = 14;
        int f = pct * n / 100;
        return new string('█', f) + new string('░', n - f) + $" {pct}%";
    }
    #endregion

    #region IExample
    public bool FillsPane => true;
    public string Category => "Flexibility";
    public string Title => "Live Dashboard";
    public string Description =>
        "A grid of live panels — streaming charts, bars, a process table, a log and sparklines — fed a few times a second.";
    #endregion

    #region Fields
    private const int Window = 60;
    private const int LatWindow = 40;

    private readonly PlotSeries _txUsa, _txEu, _errors, _latency, _util;
    private readonly DataTable _procs;
    private readonly Log _log;
    private readonly Sparkline _tp1, _tp2;
    private readonly TextLabel _deploy;

    private readonly Random _rng = new(7);
    private readonly string[] _procNames = ["node", "java", "gulp", "timer", "python", "awk", "redis", "nginx"];
    private readonly double[] _tp1Data = new double[16];
    private readonly double[] _tp2Data = new double[16];
    private double _txUsaV = 60, _txEuV = 25, _errV = 30, _latV = 5;
    private int _tick, _deployPct;
    private CancellationTokenSource? _cts;
    #endregion
}
