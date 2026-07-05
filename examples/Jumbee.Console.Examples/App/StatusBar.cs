namespace Jumbee.Console.Examples;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Spectre.Console.Rendering;

/// <summary>
/// The always-on footer: the current example on the left, live process metrics in the middle, and key hints on the
/// right — so the "lightweight" claim is visible at all times, not just when a HUD is toggled. Refreshes a few times
/// a second from <see cref="UI.ProcessMetrics"/> using the same throttled <see cref="UI.Paint"/> tick the perf HUD uses.
/// </summary>
public sealed class StatusBar : RenderableControl
{
    public StatusBar()
    {
        Focusable = false;
        ApplyTheme();
        UI.Paint += OnUiPaint;
    }

    /// <summary>The current example name shown on the left.</summary>
    public string Current { get => _current; set { _current = value; Invalidate(); } }

    protected override int IntrinsicHeight() => 1;

    protected override void ApplyTheme() => _style = UI.StyleTheme.TextMuted | UI.StyleTheme.Surface;

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var width = Math.Max(1, maxWidth);
        var m = UI.ProcessMetrics;

        // Both mem and alloc show the windowed AVERAGE with the rolling PEAK in parens. For alloc the average is the
        // "is it flat" number (near-zero even at fullscreen once a resize burst ages out); for mem — a sticky gauge —
        // the average tracks the current footprint and the peak is the high-water mark within the window.
        var perf = $"frame {m.RenderTimeMsAvg * 1000:F0}µs (peak {m.RenderTimeMsPeak * 1000:F0}) · " +
                   $"busy {m.BusyPercentAvg:F0}% (peak {m.BusyPercentPeak:F0}) · cpu {m.CpuUsagePercent:F1}% · " +
                   $"mem {m.WorkingSetBytesAvg / 1048576.0:F0}MB (peak {m.WorkingSetBytesPeak / 1048576.0:F0}) · " +
                   $"alloc {m.AllocatedBytesPerFrame / 1024.0:F1}KB/f (peak {m.PeakAllocatedBytesPerFrame / 1024.0:F0})";
        var left = string.IsNullOrEmpty(_current) ? " Jumbee.Console Examples" : $" {_current}";
        var hints = "Ctrl+← → pane · Ctrl+G perf · Ctrl+B/E collapse · Ctrl+Q quit ";

        var body = $"{left}    {perf}";
        // Right-align the hints when there's room; otherwise just concatenate and clip.
        var line = body.Length + hints.Length + 2 <= width ? body.PadRight(width - hints.Length) + hints : body + "  " + hints;
        line = line.Length < width ? line.PadRight(width) : line[..width];

        yield return new Segment(line, _style.SpectreConsoleStyle);
    }

    private void OnUiPaint(object? sender, UI.PaintEventArgs e)
    {
        if (_refresh.ElapsedMilliseconds < RefreshMs) return;
        _refresh.Restart();
        Invalidate();
    }

    public override void Dispose()
    {
        UI.Paint -= OnUiPaint;
        base.Dispose();
    }

    private const long RefreshMs = 250;
    private string _current = "";
    private Style _style;
    private readonly Stopwatch _refresh = Stopwatch.StartNew();
}
