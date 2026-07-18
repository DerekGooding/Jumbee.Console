namespace Jumbee.Console;

using System;
using System.Diagnostics;

using Spectre.Console.Rendering;

using S = Spectre.Console;

/// <summary>
/// A translucent "glass" HUD showing live UI telemetry — frame draw/paint times (µs), CPU, working set, allocation
/// rate and, the headline for a no-lock design, monitor lock contentions — floating over the app.
/// </summary>
/// <remarks>
/// <para>The panel is frosted glass (the app shows through as soft tinted smudges, not raw glyphs); the readout is
/// drawn crisply on top; it refreshes itself a few times a second while shown.</para>
/// <para>Timing comes from <see cref="UI.AverageDrawTime"/>/<see cref="UI.AveragePaintTime"/>; process metrics
/// are read directly from <see cref="Process"/>/<see cref="GC"/>/<see cref="Monitor"/> and differenced across
/// refreshes, so no external sampling has to be running. Show it with <see cref="GlassPanel.Show"/> /
/// <see cref="ShowTopRight"/>, toggle with <see cref="GlassPanel.Toggle"/> or <see cref="RegisterToggle"/>.</para>
/// </remarks>
public sealed class PerfHud : GlassPanel
{
    #region Constructors
    /// <param name="tint">Glass colour the app beneath is tinted toward.</param>
    /// <param name="factor">Blend strength (0 = clear, 1 = opaque tint).</param>
    /// <param name="frosted">Frost the app beneath to a colour blur (clean readout, content shows as soft smudges)
    /// rather than letting its raw glyphs bleed through and clutter the readout.</param>
    public PerfHud(Color? tint = null, float factor = 0.6f, bool frosted = true)
        : base(HudWidth, HudHeight, tint ?? new Color(44, 54, 82), factor, frosted)
    {
        Refresh();
        UI.Paint += OnHudPaint;
    }
    #endregion

    #region Methods
    /// <summary>Floats the HUD in the top-right corner of the current UI, <paramref name="margin"/> cells in from
    /// the edges.</summary>
    public void ShowTopRight(int margin = 1, Overlay? overlay = null)
    {
        var ov = overlay ?? UI.Overlay
            ?? throw new InvalidOperationException("No overlay is available to host the HUD; start the UI first.");
        var w = ov.Bottom.CControl.Size.Width;
        Show(Math.Max(margin, w - HudWidth - margin), margin, ov);
    }

    /// <summary>Registers a global hotkey (default <c>Ctrl+G</c>) that toggles the HUD in the top-right corner over
    /// the ambient <see cref="UI.Overlay"/>. Call once after <see cref="UI.Start"/>.</summary>
    public void RegisterToggle(ConsoleKeyInfo? key = null, int margin = 1)
        => UI.RegisterHotKey(key ?? UI.HotKeys.Ctrl(ConsoleKey.G), () => { if (IsShown) Hide(); else ShowTopRight(margin); });

    /// <summary>Rebuilds the telemetry readout from the current metrics. Called automatically while shown.</summary>
    public void Refresh() => Content = Build();

    private static IRenderable Build()
    {
        var m = UI.ProcessMetrics;
        // "frame"/"busy" are the high-resolution per-frame RENDER cost (peak-over-window) — near-0 for retained
        // rendering, which is the point. "cpu" is whole-process (matches Task Manager); it captures work outside the
        // render cycle (input, dispatcher, other threads) that the per-frame numbers don't.
        // frame/busy show the AVERAGE (the typical frame — low for retained rendering) with the PEAK as a separate
        // row (the worst frame in the window — a resize/paste burst).
        double renderUs = m.RenderTimeMsAvg * 1000.0;
        double renderPeakUs = m.RenderTimeMsPeak * 1000.0;
        double busy = m.BusyPercentAvg;
        double busyPeak = m.BusyPercentPeak;
        // Fraction of frames that took the full draw path (vs idled) — a retained UI keeps this low.
        double redraw = m.RedrawPercent;
        // Fraction of the SCREEN re-composited per drawn frame — dirty-rect rendering keeps this tiny (a status-bar
        // tick redraws only its own rows), spiking to 100 only on resize/theme switch.
        double dirty = m.DirtyAreaPercentAvg;
        double dirtyPeak = m.DirtyAreaPercentPeak;
        double cpu = m.CpuUsagePercent;
        // mem is a sticky gauge: the average tracks the current footprint and the peak is the window high-water mark.
        double memMb = m.WorkingSetBytesAvg / 1048576.0;
        double memPeakMb = m.WorkingSetBytesPeak / 1048576.0;
        // Average = the steady per-frame allocation (near-zero for retained rendering, even at fullscreen); peak =
        // the worst single frame in the window (a resize/paste burst). Showing both makes "is it flat" obvious.
        double allocKb = m.AllocatedBytesPerFrame / 1024.0;
        double allocPeakKb = m.PeakAllocatedBytesPerFrame / 1024.0;
        double exc = m.ExceptionsPerSecond;
        long locks = m.LockContentions;

        var g = new S.Grid();       
        g.AddColumn(new S.GridColumn { Padding = new S.Padding(0, 0, 2, 0) });
        g.AddColumn();
        // Each metric on one row: the AVERAGE (the typical/steady value) in bright ink, then the PEAK — the worst
        // frame in the window (a resize/paste burst) — dimmed after a slash. redraw/cpu are single gauges.
        g.AddRow(new S.Markup("[grey62]frame[/]"), new S.Markup($"[#e8f0ff]{renderUs,5:F0} µs[/] [grey50]/ {renderPeakUs:F0}[/]"));
        g.AddRow(new S.Markup("[grey62]busy[/]"), new S.Markup($"[#e8f0ff]{busy,5:F0} %[/] [grey50]/ {busyPeak:F0}[/]"));
        g.AddRow(new S.Markup("[grey62]redraw[/]"), new S.Markup($"[#e8f0ff]{redraw,5:F0} %[/]"));
        g.AddRow(new S.Markup("[grey62]dirty[/]"), new S.Markup($"[#e8f0ff]{dirty,5:F1} %[/] [grey50]/ {dirtyPeak:F0}[/]"));
        g.AddRow(new S.Markup("[grey62]cpu[/]"), new S.Markup($"[#e8f0ff]{cpu,5:F1} %[/]"));
        g.AddRow(new S.Markup("[grey62]mem[/]"), new S.Markup($"[#e8f0ff]{memMb,5:F1} MB[/] [grey50]/ {memPeakMb:F0}[/]"));
        g.AddRow(new S.Markup("[grey62]alloc[/]"), new S.Markup($"[#e8f0ff]{allocKb,5:F1} KB/f[/] [grey50]/ {allocPeakKb:F0}[/]"));
        g.AddRow(new S.Markup("[grey62]exc/s[/]"), new S.Markup(exc > 0 ? $"[bold #ff6b6b]{exc,5:F0}[/]" : "[#e8f0ff]    0[/]"));
        // The dagger: a no-lock UI design holds contention at zero. Green 0 when true, red count otherwise.
        g.AddRow(new S.Markup("[grey62]locks[/]"), new S.Markup(locks == 0 ? "[bold #7CFC00]0 ✓[/]" : $"[bold #ff6b6b]{locks}[/]"));

        return new S.Panel(g)
        {
            Border = S.BoxBorder.Rounded,
            Padding = new S.Padding(1, 0, 1, 0),
            Expand = true,
            Header = new S.PanelHeader("[#8fd0ff] ◈ perf · glass [/]"),
            BorderStyle = new S.Style(foreground: S.Color.SkyBlue1),
        };
    }

    private void OnHudPaint(object? sender, UI.PaintEventArgs e)
    {
        // Only sample metrics / rebuild the readout while the HUD is actually on screen. OnHudPaint stays subscribed
        // to UI.Paint for the control's whole lifetime (ctor→Dispose), so without this guard Build() — a Process/GC/
        // Monitor sample plus a fresh Grid/Panel/Markup graph — and its subsequent Spectre render (GetSegments) run
        // ~4×/s from launch even while hidden. _refresh keeps ticking while hidden, so the first paint after Show()
        // is already past RefreshMs and refreshes immediately (no staleness on show).
        if (!IsShown) return;
        if (_refresh.ElapsedMilliseconds >= RefreshMs)
        {
            _refresh.Restart();
            Refresh();
        }
    }

    public override void Dispose()
    {
        UI.Paint -= OnHudPaint;
        base.Dispose();
    }
    #endregion

    #region Fields
    private const int HudWidth = 34;
    private const int HudHeight = 11;
    private const long RefreshMs = 250;
    private readonly Stopwatch _refresh = Stopwatch.StartNew();
    #endregion
}
