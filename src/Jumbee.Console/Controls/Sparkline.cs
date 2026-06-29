namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using Spectre.Console.Rendering;

/// <summary>
/// A compact, single-row chart that draws a series of numeric values as block bars (one cell per value),
/// scaling each value's height against the series maximum. Self-rendering; one row tall, as wide as the
/// number of values (unless an explicit <see cref="Control.Width"/> is set).
/// </summary>
public class Sparkline : RenderableControl
{
    #region Constructors
    public Sparkline(params double[] values)
    {
        _values = values ?? [];
        Height = 1;
        Width = _values.Length;
        ApplyTheme();
    }
    #endregion

    #region Properties
    /// <summary>The series values. Setting it re-sizes the control to one cell per value.</summary>
    public double[] Values
    {
        get => _values;
        set => SetAtomicProperty(ref _values, value ?? [], updatesLayout: true, watch: (_, v) => Width = v.Length);
    }

    /// <summary>The value mapped to a full-height bar. When <see langword="null"/> (default) the series maximum is used.</summary>
    public double? Max
    {
        get => _max;
        set => SetAtomicProperty(ref _max, value);
    }

    /// <summary>The bar style. Defaults to <see cref="IStyleTheme.TextAccent"/>.</summary>
    public Style BarStyle { get => _barStyle; set => SetAtomicProperty(ref _barStyle, value, themeOverride: true); }

    /// <summary>
    /// The glyph ramp used for bar heights, ordered shortest → tallest. Defaults to <see cref="BlockBars"/>
    /// (the eighth-block elements <c>▁▂▃▄▅▆▇█</c>). Those need a font with block-element coverage (e.g. Windows
    /// Terminal / Cascadia Mono); on a legacy console (cmd.exe with a raster font) set this to <see cref="AsciiBars"/>.
    /// </summary>
    public string Bars
    {
        get => _bars;
        set => SetAtomicProperty(ref _bars, string.IsNullOrEmpty(value) ? BlockBars : value);
    }
    #endregion

    #region Methods
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(BarStyle))) _barStyle = UI.StyleTheme.TextAccent;
    }

    // Content-only render (never reads focus/hover): reuse the cached buffer on interactive-state changes.
    protected override bool RendersInteractiveState => false;

    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        var width = Math.Min(_values.Length, maxWidth);
        return new Measurement(width, width);
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (_values.Length == 0) yield break;

        var max = _max ?? _values.DefaultIfEmpty(0).Max();
        var width = Math.Min(_values.Length, maxWidth);
        var sb = new System.Text.StringBuilder(width);
        for (var i = 0; i < width; i++)
        {
            sb.Append(BarFor(_values[i], max));
        }
        yield return new Segment(sb.ToString(), _barStyle.SpectreConsoleStyle);
    }

    // Maps a value to a glyph in the ramp (or a space when there is nothing to draw).
    private char BarFor(double value, double max)
    {
        if (max <= 0 || value <= 0) return value > 0 ? _bars[0] : ' ';
        var level = (int)Math.Ceiling(value / max * _bars.Length);
        level = Math.Clamp(level, 1, _bars.Length);
        return _bars[level - 1];
    }
    #endregion

    #region Fields
    /// <summary>The default eighth-block ramp <c>▁▂▃▄▅▆▇█</c> (needs block-element font coverage).</summary>
    public const string BlockBars = "▁▂▃▄▅▆▇█";

    /// <summary>An ASCII-only ramp for legacy consoles that lack the block elements.</summary>
    public const string AsciiBars = ".:-=+*#@";

    private double[] _values;
    private double? _max;
    private Style _barStyle;
    private string _bars = BlockBars;
    #endregion
}
