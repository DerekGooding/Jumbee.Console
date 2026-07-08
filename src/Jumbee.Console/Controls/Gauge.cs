namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using Spectre.Console.Rendering;

using SColor = Spectre.Console.Color;
using SStyle = Spectre.Console.Style;

/// <summary>
/// A single-row horizontal progress bar: the track is filled proportional to <see cref="Value"/> / <see cref="Max"/>,
/// optionally followed by the percentage and the raw value — e.g. <c>████████░░░░  34.5% (126)</c>. For dashboards
/// (year/day progress, a deployment %, a capacity meter). The bar is drawn as a solid colour band (a background-filled
/// space, seam-free on any font) with an eighth-block sub-cell edge so it animates smoothly.
/// </summary>
public class Gauge : RenderableControl
{
    #region Constructors
    public Gauge(double value = 0, double max = 100)
    {
        _value = value;
        _max = max <= 0 ? 1 : max;
        Height = 1;
        ApplyTheme();
    }
    #endregion

    #region Properties
    /// <summary>The current value. The filled fraction is <see cref="Value"/> / <see cref="Max"/> (clamped to 0..1).</summary>
    public double Value { get => _value; set => SetAtomicProperty(ref _value, value); }

    /// <summary>The value mapped to a full bar. Coerced to at least a tiny positive number so the fraction is defined.</summary>
    public double Max { get => _max; set => SetAtomicProperty(ref _max, value, validate: v => v <= 0 ? 1 : v); }

    /// <summary>An optional label drawn before the bar (e.g. a metric name). Null/empty draws none.</summary>
    public string? Label { get => _label; set => SetAtomicProperty(ref _label, value); }

    /// <summary>Whether to draw the percentage (<c>34.5%</c>) after the bar. Default <see langword="true"/>.</summary>
    public bool ShowPercent { get => _showPercent; set => SetAtomicProperty(ref _showPercent, value); }

    /// <summary>Whether to draw the raw value in parentheses (<c>(126)</c>) after the bar. Default <see langword="false"/>.</summary>
    public bool ShowValue { get => _showValue; set => SetAtomicProperty(ref _showValue, value); }

    /// <summary>The fill/track/text colours. Defaults to <see cref="IStyleTheme.Gauge"/>.</summary>
    public GaugeStyle Style { get => _style; set => SetAtomicProperty(ref _style, value, themeOverride: true); }
    #endregion

    #region Methods
    /// <summary>Recolours the fill (a fluent shorthand for <c>Style = Style.WithFill(color)</c>); marks it an override.</summary>
    public Gauge WithFill(Color color) { Style = _style.WithFill(color); return this; }

    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(Style))) _style = UI.StyleTheme.Gauge;
    }

    // Content-only render (never reads focus/hover): reuse the cached buffer on interactive-state changes.
    protected override bool RendersInteractiveState => false;

    // Fixed one row tall; fills the width its parent offers.
    protected override int IntrinsicHeight() => 1;

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        int width = Math.Max(1, maxWidth);
        double fraction = Math.Clamp(_max <= 0 ? 0 : _value / _max, 0, 1);

        var left = string.IsNullOrEmpty(_label) ? "" : _label + " ";
        var right = "";
        if (_showPercent) right += $" {fraction * 100:0.0}%";
        if (_showValue) right += $" ({FormatValue(_value)})";

        int barWidth = Math.Max(1, width - left.Length - right.Length);

        var textStyle = _style.Text.SpectreConsoleStyle;
        var fillColor = _style.Fill.SpectreConsoleStyle?.Foreground ?? SColor.Blue;
        var trackColor = _style.Track.SpectreConsoleStyle?.Foreground ?? SColor.Grey;
        var fillBand = new SStyle(background: fillColor);
        var trackBand = new SStyle(background: trackColor);

        // Split the bar into full fill cells, a fractional edge cell (an eighth-block: fill on the left, track on the
        // right), and the remaining track cells.
        double exact = fraction * barWidth;
        int full = (int)Math.Floor(exact);
        int eighths = (int)Math.Round((exact - full) * 8);
        if (eighths >= 8) { full++; eighths = 0; }
        full = Math.Min(full, barWidth);
        bool hasEdge = eighths > 0 && full < barWidth;
        int trackCells = barWidth - full - (hasEdge ? 1 : 0);

        if (left.Length > 0) yield return new Segment(left, textStyle);
        if (full > 0) yield return new Segment(new string(' ', full), fillBand);
        if (hasEdge) yield return new Segment(LeftBlocks[eighths].ToString(), new SStyle(foreground: fillColor, background: trackColor));
        if (trackCells > 0) yield return new Segment(new string(' ', trackCells), trackBand);
        if (right.Length > 0) yield return new Segment(right, textStyle);
    }

    // Whole numbers render without a decimal point; fractional values keep up to two places.
    private static string FormatValue(double v) => v == Math.Floor(v) ? ((long)v).ToString() : v.ToString("0.##");
    #endregion

    #region Fields
    private double _value;
    private double _max;
    private string? _label;
    private bool _showPercent = true;
    private bool _showValue;
    private GaugeStyle _style;

    // Left-anchored eighth blocks for the fractional fill edge (index = eighths filled, 0 = empty .. 8 = full).
    private static readonly char[] LeftBlocks = [' ', '▏', '▎', '▍', '▌', '▋', '▊', '▉', '█'];
    #endregion
}
