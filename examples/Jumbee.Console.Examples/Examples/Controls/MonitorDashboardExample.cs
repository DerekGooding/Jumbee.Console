namespace Jumbee.Console.Examples;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Jumbee.Console;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// A "sampler"-style system monitor: two streaming run charts (with per-series cur/dlt/max/min legends), a rolling
/// CPU meter, a server-utilization bar chart, a scrolling server log, a UTC clock, a weather box, a container table
/// and a live <b>Task Manager</b> panel showing this machine's top processes by CPU (real
/// <see cref="System.Diagnostics.Process"/> data). Most panels run off a fast simulated feed; the process list is
/// sampled on a slower background cadence. Shows off <see cref="RunChart"/>, <see cref="Gauge"/>,
/// <see cref="TextPanel"/>, <see cref="Digits"/>, <see cref="DataTable"/>, <see cref="Log"/> and <see cref="Plot"/>.
/// </summary>
public sealed class MonitorDashboardExample : CompositeControl, IExample, IActivatable
{
    public MonitorDashboardExample()
    {
        // --- streaming run charts (Plot + cur/dlt/max/min legend) ----------------------------------------------
        var search = new RunChart().SetYRange(0, 1.4).SetXWindow(Window);
        _bing = search.AddSeries("bing", new CColor(120, 200, 120));
        _google = search.AddSeries("googl", new CColor(230, 200, 90));
        _yahoo = search.AddSeries("yahoo", new CColor(190, 130, 230));
        search.ValueFormat = "0.000";

        var kafka = new RunChart().SetYRange(0, 9000).SetXWindow(Window);
        _inbound = kafka.AddSeries("in", new CColor(120, 200, 120));
        _processing = kafka.AddSeries("proc", new CColor(90, 160, 240));
        _dlq = kafka.AddSeries("dlq", new CColor(235, 90, 90));
        kafka.ValueFormat = "0";

        // --- CPU meter: a current-value gauge over a rolling history bar chart ---------------------------------
        _cpuGauge = new Gauge(0, 100) { ShowValue = false }.WithFill(new CColor(120, 200, 120));
        var cpuPlot = CleanBars(out _cpuBars, new CColor(120, 200, 120), 100);

        // --- server utilization: live categorical bars (US1..JP1) ----------------------------------------------
        var utilPlot = new Plot();
        _util = utilPlot.AddLiveBars(new CColor(90, 160, 240));
        utilPlot
            .SetYRange(0, 11)
            .ConfigureAxis(a => a.IsVisible = false)
            .SetXTicks([.. _sites.Select((s, i) => ((double)(i + 1), s))])
            .ConfigureTicks(t => t.Labels.AttachToAxis = false);

        _clock = new Digits("00:00:00");
        _weather = new TextPanel();
        _log = new Log();
        _procs = new DataTable("Container", "Cpu%", "Mem");
        _tasks = new DataTable("Process", "Cpu%", "Mem");

        Seed();

        // Narrow last column (27, floored by the UTC clock's full width) leaves the source pane more room beside the
        // grid; the clock and Top-Processes table are swapped so the wide table sits in a wide column.
        var grid = new Grid([13, 12, 10], [48, 44, 27],
        [
            [search.WithFrame(BorderStyle.Rounded, title: "Search Response (s)"),
             kafka.WithFrame(BorderStyle.Rounded, title: "Kafka In-Flight / Topic"),
             _clock.WithFrame(BorderStyle.Rounded, title: "UTC Time")],
            [Meter(_cpuGauge, cpuPlot).WithFrame(BorderStyle.Rounded, title: "CPU Usage %"),
             utilPlot.WithFrame(BorderStyle.Rounded, title: "Server Utilization"),
             _log.WithFrame(BorderStyle.Rounded, title: "Server Log")],
            [_procs.WithFrame(BorderStyle.Rounded, title: "Containers"),
             _tasks.WithFrame(BorderStyle.Rounded, title: "Top Processes (live)"),
             _weather.WithFrame(BorderStyle.Rounded, title: "Weather")],
        ]);

        SetContent(grid);
    }

    // A meter panel: the current-value gauge on top, its rolling history bar chart filling below.
    private static Group Meter(Gauge gauge, Control history) =>
        new(new DockPanel(DockedControlPlacement.Top, gauge, history));

    // A chrome-free rolling bar chart pinned to [0, max], plus its live-bars handle.
    private static Plot CleanBars(out PlotSeries bars, CColor color, double max)
    {
        var plot = new Plot();
        bars = plot.AddLiveBars(color, width: 0.9);
        plot.SetYRange(0, max).ConfigureAxis(a => a.IsVisible = false).ConfigureGrid(g => g.IsVisible = false);
        return plot;
    }

    #region Live feed
    public void OnActivated()
    {
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        // Fast simulated feed (charts, meter, log, containers, clock, weather).
        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(50, ct); }
                catch (TaskCanceledException) { break; }
                UI.Post(Advance);
            }
        });

        // Slow REAL process sampling on a background thread (enumerating the OS process list every tick is expensive,
        // so this runs at its own ~1.5s cadence and only the finished top-N is posted to the UI thread).
        _ = Task.Run(async () =>
        {
            _sampler.Sample(TopProcesses);   // prime the CPU-time deltas; first display shows real % next round
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(1500, ct); }
                catch (TaskCanceledException) { break; }
                var top = _sampler.Sample(TopProcesses);
                UI.Post(() => ShowProcesses(top));
            }
        });
    }

    public void OnDeactivated()
    {
        _cts?.Cancel();
        _cts = null;
    }

    // Runs on the UI thread (posted), so control updates are direct.
    private void Advance()
    {
        if (Size.Width <= 0 || Size.Height <= 0) return;
        int t = _tick++;

        _bing.Push(_bV = Walk(_bV, 0.10, 0.45, 0.08));
        _google.Push(_gV = Walk(_gV, 0.15, 0.70, 0.10));
        _yahoo.Push(_yV = Walk(_yV, 0.55, 1.30, 0.18));

        _inbound.Push(_inV = Walk(_inV, 3000, 8500, 900));
        _processing.Push(_prV = Walk(_prV, 1500, 6000, 700));
        _dlq.Push(_dqV = Walk(_dqV, 0, 900, 200));

        _cpuV = Walk(_cpuV, 5, 95, 18);
        _cpuGauge.Value = _cpuV;
        Shift(_cpuData, _cpuV);
        _cpuBars.SetValues(_cpuData);   // SetValues copies into the series' own buffer, so no defensive clone needed

        for (int i = 0; i < _utilData.Length; i++) _utilData[i] = _rng.Next(1, 11);
        _util.SetValues(_utilData);

        _clock.Text = DateTime.UtcNow.ToString("HH:mm:ss");

        // The simulated feed is fast (50ms); throttle the log and container/weather churn to a human rate.
        if (t % 20 == 0) _log.Write($"[grey]{DateTime.UtcNow:HH:mm:ss}[/] [green]OK[/] req [white]{_rng.Next(100, 999)}[/] {_rng.NextDouble():0.00}s");
        if (t % 6 == 0) UpdateContainers();
        if (t % 160 == 0) _weather.Markup = Weather();
    }

    private void UpdateContainers()
    {
        _procs.Clear();
        foreach (var name in _containers)
            _procs.AddRow(name, _rng.Next(0, 40).ToString(), $"{_rng.Next(40, 400)}M");
    }

    // Posts the sampled top processes into the Task Manager table (runs on the UI thread).
    private void ShowProcesses(List<(string name, double cpu, double memMb)> rows)
    {
        _tasks.Clear();
        foreach (var r in rows)
            _tasks.AddRow(r.name.Length > 16 ? r.name[..16] : r.name, $"{r.cpu:0.0}", $"{r.memMb:0}M");
    }

    private void Seed()
    {
        for (int i = 0; i < Window; i++)
        {
            _bing.Push(_bV = Walk(_bV, 0.10, 0.45, 0.08));
            _google.Push(_gV = Walk(_gV, 0.15, 0.70, 0.10));
            _yahoo.Push(_yV = Walk(_yV, 0.55, 1.30, 0.18));
            _inbound.Push(_inV = Walk(_inV, 3000, 8500, 900));
            _processing.Push(_prV = Walk(_prV, 1500, 6000, 700));
            _dlq.Push(_dqV = Walk(_dqV, 0, 900, 200));
        }
        for (int i = 0; i < _cpuData.Length; i++) _cpuData[i] = _rng.Next(10, 90);
        _cpuBars.SetValues((double[])_cpuData.Clone());
        _util.SetValues([5, 5, 1, 9, 7, 10]);
        _weather.Markup = Weather();
        _log.Write("[green]OK[/] monitor started");
        _tasks.AddRow("sampling…", "", "");   // placeholder until the first background process sample lands (~1.5s)
        UpdateContainers();
    }

    private string Weather()
    {
        string Row(string city, string sky, int lo, int hi) =>
            $"[grey70]{city,-9}[/] {sky} [white]{_rng.Next(lo, hi)}°F[/] [grey54]{(_rng.Next(2) == 0 ? "↑" : "↓")} {_rng.Next(3, 14)} mph[/]";
        return string.Join('\n',
            Row("Local", "[yellow]☀[/]", 58, 66),
            Row("New York", "[grey62]☁[/]", 52, 61),
            Row("San Fran", "[yellow]☀[/]", 57, 64));
    }

    private double Walk(double v, double lo, double hi, double step) =>
        Math.Clamp(v + (_rng.NextDouble() - 0.5) * step, lo, hi);

    private static void Shift(double[] a, double v) { Array.Copy(a, 1, a, 0, a.Length - 1); a[^1] = v; }
    #endregion

    #region IExample
    public bool FillsPane => true;
    public string Category => "Flexibility";
    public string Title => "System Monitor";
    public string Description =>
        "A sampler-style monitor: streaming run charts, a CPU meter, a live Task Manager (real process CPU/memory), a server log, a UTC clock and more.";
    #endregion

    // A framable group: wraps any layout so a set of controls can share one titled frame.
    private sealed class Group : CompositeControl
    {
        public Group(ILayout content) => SetContent(content);
        protected override bool FillsFrameViewport => true;
    }

    // Samples the OS process list off the UI thread: per-process CPU% (from TotalProcessorTime deltas between
    // samples, normalised to total system capacity) and working-set memory, returning the top N by CPU.
    private sealed class ProcessSampler
    {
        private readonly Dictionary<int, (TimeSpan cpu, DateTime at)> _last = new();
        private readonly int _cores = Math.Max(1, Environment.ProcessorCount);

        public List<(string name, double cpu, double memMb)> Sample(int top)
        {
            var now = DateTime.UtcNow;
            var rows = new List<(string name, double cpu, double memMb)>();
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    var cpu = p.TotalProcessorTime;
                    double pct = 0;
                    if (_last.TryGetValue(p.Id, out var prev))
                    {
                        var wallMs = (now - prev.at).TotalMilliseconds;
                        if (wallMs > 0) pct = (cpu - prev.cpu).TotalMilliseconds / (wallMs * _cores) * 100.0;
                    }
                    _last[p.Id] = (cpu, now);
                    rows.Add((p.ProcessName, Math.Clamp(pct, 0, 100), p.WorkingSet64 / 1048576.0));
                }
                catch { /* protected or already-exited process — skip */ }
                finally { p.Dispose(); }
            }
            return rows.OrderByDescending(r => r.cpu).ThenByDescending(r => r.memMb).Take(top).ToList();
        }
    }

    #region Fields
    private const int Window = 60;
    private const int TopProcesses = 8;

    private readonly RunSeries _bing, _google, _yahoo, _inbound, _processing, _dlq;
    private readonly Gauge _cpuGauge;
    private readonly PlotSeries _cpuBars, _util;
    private readonly Digits _clock;
    private readonly TextPanel _weather;
    private readonly Log _log;
    private readonly DataTable _procs, _tasks;
    private readonly ProcessSampler _sampler = new();

    private readonly Random _rng = new(11);
    private readonly string[] _containers = ["gateway", "mongodb", "rabbitmq", "turbine", "clickhouse", "dispatcher", "monitoring"];
    private readonly string[] _sites = ["US1", "US2", "EU1", "AU1", "AS1", "JP1"];
    private readonly double[] _cpuData = new double[30];
    private readonly double[] _utilData = new double[6];   // reused per-tick scratch for the utilization bars (matches _sites)
    private double _bV = 0.2, _gV = 0.3, _yV = 0.8, _inV = 5000, _prV = 3000, _dqV = 200, _cpuV = 40;
    private int _tick;
    private CancellationTokenSource? _cts;
    #endregion
}
