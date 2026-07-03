namespace Jumbee.Console;

using System;
using System.Diagnostics;

using Spectre.Console.Rendering;

using S = Spectre.Console;

/// <summary>
/// A translucent "glass" HUD showing live UI telemetry — frame draw/paint times (µs), CPU, working set, allocation
/// rate and, the headline for a no-lock design, monitor lock contentions — floating over the app. The panel is
/// frosted glass (the app shows through as soft tinted smudges, not raw glyphs); the readout is drawn crisply on
/// top; it refreshes itself a few times a second while shown.
/// </summary>
/// <remarks>Timing comes from <see cref="UI.AverageDrawTime"/>/<see cref="UI.AveragePaintTime"/>; process metrics
/// are read directly from <see cref="Process"/>/<see cref="GC"/>/<see cref="Monitor"/> and differenced across
/// refreshes, so no external sampling has to be running. Show it with <see cref="GlassPanel.Show"/> /
/// <see cref="ShowTopRight"/>, toggle with <see cref="GlassPanel.Toggle"/> or <see cref="RegisterToggle"/>.</remarks>
public sealed class PerfHud : GlassPanel
{
    #region Constructors
    /// <param name="tint">Glass colour the app beneath is tinted toward.</param>
    /// <param name="factor">Blend strength (0 = clear, 1 = opaque tint).</param>
    /// <param name="frosted">Frost the app beneath to a colour blur (clean readout, content shows as soft smudges)
    /// rather than letting its raw glyphs bleed through and clutter the readout.</param>
    public PerfHud(CColor? tint = null, float factor = 0.6f, bool frosted = true)
        : base(HudWidth, HudHeight, tint ?? new CColor(44, 54, 82), factor, frosted)
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
        double drawUs = UI.AverageDrawTime * 1000.0;
        double paintUs = UI.AveragePaintTime * 1000.0;
        double memMb = m.WorkingSetBytes / 1048576.0;
        double allocKb = m.AllocatedBytesPerFrame / 1024.0;
        double gcPause = m.GcPausePercent;
        long locks = m.LockContentions;

        var g = new S.Grid();
        g.AddColumn(new S.GridColumn { Padding = new S.Padding(0, 0, 2, 0) });
        g.AddColumn();
        g.AddRow(new S.Markup("[grey62]draw[/]"), new S.Markup($"[#e8f0ff]{drawUs,6:F0} µs[/]"));
        g.AddRow(new S.Markup("[grey62]paint[/]"), new S.Markup($"[#e8f0ff]{paintUs,6:F0} µs[/]"));
        g.AddRow(new S.Markup("[grey62]cpu[/]"), new S.Markup($"[#e8f0ff]{m.CpuUsagePercent,6:F1} %[/]"));
        g.AddRow(new S.Markup("[grey62]mem[/]"), new S.Markup($"[#e8f0ff]{memMb,6:F1} MB[/]"));
        g.AddRow(new S.Markup("[grey62]alloc[/]"), new S.Markup($"[#e8f0ff]{allocKb,5:F1} KB/f[/]"));
        g.AddRow(new S.Markup("[grey62]gc[/]"), new S.Markup($"[#e8f0ff]{gcPause,6:F1} %[/]"));
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
    private const int HudHeight = 9;
    private const long RefreshMs = 250;
    private readonly Stopwatch _refresh = Stopwatch.StartNew();
    #endregion
}
