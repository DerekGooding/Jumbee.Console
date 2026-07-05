namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using ConsolePlot.Drawing.Tools;
using ConsolePlot.Plotting;

using CPlot = ConsolePlot.Plot;

/// <summary>
/// A line/scatter chart backed by the ConsolePlot library, rendered into the control's buffer. Add data with
/// <see cref="AddSeries"/> and tune the axes/grid/ticks with the <c>Configure*</c> methods. The plot fills its
/// container and re-draws to fit whenever the control is resized; all configuration is replayed on each rebuild,
/// so settings survive resizing.
/// </summary>
public class Plot : Control
{
    #region Constructors
    public Plot() => Focusable = false;   // display-only
    #endregion

    #region Properties
    /// <summary>Background colour painted behind the plot, or <see langword="null"/> (the default) for transparent.</summary>
    public CColor? Background
    {
        get => _background;
        set => SetAtomicProperty(ref _background, value, watch: (_, _) => _dirty = true);
    }
    #endregion

    #region Methods
    /// <summary>
    /// Adds a data series. <paramref name="xs"/> and <paramref name="ys"/> must be the same length. When
    /// <paramref name="pen"/> is left at its default a colour is taken from the control's palette (cycling by series
    /// index) and drawn with the Braille brush.
    /// </summary>
    public Plot AddSeries(IReadOnlyCollection<double> xs, IReadOnlyCollection<double> ys, PointPen pen = default)
    {
        UI.Invoke(() =>
        {
            var p = pen.Equals(default(PointPen))
                ? new PointPen(SystemPointBrushes.Braille, Palette[_seriesCount % Palette.Length])
                : pen;
            AddElement(plot => plot.AddSeries(xs, ys, p));
        });
        return this;
    }

    /// <summary>
    /// Adds a data series drawn with the given <paramref name="brush"/> (its sub-cell resolution — Braille 2×4,
    /// Quadrant 2×2, the rest 1×1 — sets how smooth the line looks). When <paramref name="color"/> is
    /// <see langword="null"/> a colour is taken from the control's palette, cycling by series index.
    /// </summary>
    public Plot AddSeries(IReadOnlyCollection<double> xs, IReadOnlyCollection<double> ys, PlotBrush brush, CColor? color = null)
    {
        UI.Invoke(() =>
        {
            var pen = new PointPen(BrushFor(brush), color ?? Palette[_seriesCount % Palette.Length]);
            AddElement(plot => plot.AddSeries(xs, ys, pen));
        });
        return this;
    }

    /// <summary>
    /// Adds a scatter series — the points drawn as markers, without connecting lines. The <paramref name="brush"/>
    /// sets the marker (and its sub-cell resolution); <paramref name="color"/> defaults to the palette.
    /// </summary>
    public Plot AddScatter(IReadOnlyCollection<double> xs, IReadOnlyCollection<double> ys, PlotBrush brush = PlotBrush.Braille, CColor? color = null)
    {
        UI.Invoke(() =>
        {
            var pen = new PointPen(BrushFor(brush), color ?? Palette[_seriesCount % Palette.Length]);
            AddElement(plot => plot.AddScatter(xs, ys, pen));
        });
        return this;
    }

    /// <summary>
    /// Adds a stem series — a vertical line from <paramref name="baseline"/> (default 0) to each point, capped with
    /// a dot marker. <paramref name="color"/> defaults to the palette.
    /// </summary>
    public Plot AddStem(IReadOnlyCollection<double> xs, IReadOnlyCollection<double> ys, CColor? color = null, double baseline = 0)
    {
        UI.Invoke(() =>
        {
            var pen = new PointPen(SystemPointBrushes.Dot, color ?? Palette[_seriesCount % Palette.Length]);
            AddElement(plot => plot.AddStem(xs, ys, pen, baseline));
        });
        return this;
    }

    /// <summary>
    /// Adds a vertical bar series — each point drawn as a filled bar from <paramref name="baseline"/> (default 0) to
    /// its value, with an eighth-block sub-cell top. <paramref name="color"/> defaults to the palette;
    /// <paramref name="width"/> is the bar width as a fraction (0..1) of the spacing between bars.
    /// </summary>
    public Plot AddBars(IReadOnlyCollection<double> xs, IReadOnlyCollection<double> ys, CColor? color = null, double baseline = 0, double width = 0.8)
    {
        UI.Invoke(() =>
        {
            var c = color ?? Palette[_seriesCount % Palette.Length];
            AddElement(plot => plot.AddBars(xs, ys, c, baseline, width));
        });
        return this;
    }

    /// <summary>
    /// Adds a histogram of <paramref name="values"/> — the values are binned and each bin drawn as a touching bar
    /// (bar height = bin count). <paramref name="bins"/> ≤ 0 picks a bin count automatically (√n, clamped);
    /// <paramref name="color"/> defaults to the palette.
    /// </summary>
    public Plot AddHistogram(IReadOnlyList<double> values, int bins = 0, CColor? color = null)
    {
        UI.Invoke(() =>
        {
            var (mids, counts) = Histogram(values, bins);
            if (mids.Length == 0) return;
            var c = color ?? Palette[_seriesCount % Palette.Length];
            // Width 1.0 so adjacent bins touch, as a histogram should.
            AddElement(plot => plot.AddBars(mids, counts, c, 0, 1.0));
        });
        return this;
    }

    // Bins the finite values into equal-width buckets, returning each bin's midpoint (x) and count (bar height).
    private static (double[] mids, double[] counts) Histogram(IReadOnlyList<double> values, int bins)
    {
        var finite = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToArray();
        if (finite.Length == 0) return ([], []);

        double min = finite.Min(), max = finite.Max();
        if (bins <= 0) bins = Math.Clamp((int)Math.Ceiling(Math.Sqrt(finite.Length)), 1, 60);

        // All values equal: a single bar at that value.
        if (max <= min) return ([min], [finite.Length]);

        double width = (max - min) / bins;
        var counts = new double[bins];
        foreach (var v in finite)
            counts[Math.Clamp((int)((v - min) / width), 0, bins - 1)]++;   // the max value lands in the last bin

        var mids = new double[bins];
        for (int b = 0; b < bins; b++) mids[b] = min + (b + 0.5) * width;
        return (mids, counts);
    }

    private void AddElement(Action<CPlot> config)
    {
        _seriesCount++;
        _config.Add(config);
        Rebuild();
    }

    private static IPointBrush BrushFor(PlotBrush brush) => brush switch
    {
        PlotBrush.Braille => SystemPointBrushes.Braille,
        PlotBrush.Quadrant => SystemPointBrushes.Quadrant,
        PlotBrush.Block => SystemPointBrushes.Block,
        PlotBrush.Dot => SystemPointBrushes.Dot,
        PlotBrush.Star => SystemPointBrushes.Star,
        _ => SystemPointBrushes.Braille,
    };

    /// <summary>Records an arbitrary configuration step (applied to the underlying plot on every rebuild).</summary>
    public Plot Configure(Action<CPlot> configure)
    {
        UI.Invoke(() => { _config.Add(configure); Rebuild(); });
        return this;
    }

    /// <summary>Configures the axis (visibility, pen).</summary>
    public Plot ConfigureAxis(Action<AxisSettings> configure) => Configure(p => configure(p.Axis));

    /// <summary>Configures the grid (visibility, pen).</summary>
    public Plot ConfigureGrid(Action<GridSettings> configure) => Configure(p => configure(p.Grid));

    /// <summary>Configures the ticks and their labels (spacing, pen, format).</summary>
    public Plot ConfigureTicks(Action<TickSettings> configure) => Configure(p => configure(p.Ticks));

    /// <summary>Removes all series and configuration, leaving an empty plot.</summary>
    public Plot Clear()
    {
        UI.Invoke(() => { _config.Clear(); _seriesCount = 0; Rebuild(); });
        return this;
    }

    private void Rebuild()
    {
        _dirty = true;
        Invalidate();
    }

    // A plot fills its container and re-fits on resize; it must never be scrolled. Inside a ControlFrame this makes
    // the frame hand the plot the bounded viewport height instead of the unbounded scroll height (which would
    // otherwise balloon the plot to the size clamp and show only a thin slice).
    protected internal override bool FillsFrameViewport => true;

    protected override void Render()
    {
        var w = Size.Width;
        var h = Size.Height;
        if (w <= 0 || h <= 0) return;

        // Rebuild the underlying plot when the content changed or the control was resized.
        if (_dirty || _plot is null || _builtWidth != w || _builtHeight != h)
        {
            _dirty = false;
            _builtWidth = w;
            _builtHeight = h;
            _plot = BuildPlot(w, h);
        }

        consoleBuffer.Initialize();

        // Skip when there's nothing to draw or no room for the axes/labels. ConsolePlot pads degenerate data ranges
        // (a single point / flat series) internally, so Draw is safe for any non-empty data at a usable size — no
        // try/catch needed here, and the UI frame loop is the ultimate backstop for anything unforeseen.
        if (_plot is null || w < MinWidth || h < MinHeight) return;
        _plot.Draw();
        _plot.Render();   // blits GetImage() into consoleBuffer (see PlotImage)
    }

    private PlotImage? BuildPlot(int width, int height)
    {
        if (_config.Count == 0) return null;
        var plot = new PlotImage(width, height, consoleBuffer) { Background = _background };
        foreach (var apply in _config) apply(plot);
        return plot;
    }
    #endregion

    #region Fields
    private const int MinWidth = 8;
    private const int MinHeight = 4;

    // Pleasant, high-contrast default series colours, cycled by series index. Avoids ConsolePlot's own default
    // palette, whose first entry is black — invisible on a dark terminal.
    private static readonly CColor[] Palette =
    [
        new(89, 145, 240),  new(240, 120, 100), new(120, 200, 120), new(230, 190, 90),
        new(190, 130, 230), new(110, 205, 220), new(235, 140, 200), new(160, 170, 180),
    ];

    private readonly List<Action<CPlot>> _config = [];
    private PlotImage? _plot;
    private int _seriesCount;
    private int _builtWidth = -1;
    private int _builtHeight = -1;
    private bool _dirty = true;
    private CColor? _background;
    #endregion
}

/// <summary>
/// Selects the glyph set (and thus the sub-cell resolution) a <see cref="Plot"/> series is drawn with. Higher
/// resolution packs more plotted points into each character cell for a smoother line.
/// </summary>
public enum PlotBrush
{
    /// <summary>Braille dots — 2×4 sub-cells per character (8 points/cell), the smoothest. The default.</summary>
    Braille,
    /// <summary>Quadrant blocks — 2×2 sub-cells per character (4 points/cell), solid blocks rather than dots.</summary>
    Quadrant,
    /// <summary>A solid full block <c>█</c> per point (1×1).</summary>
    Block,
    /// <summary>A <c>•</c> per point (1×1).</summary>
    Dot,
    /// <summary>A <c>*</c> per point (1×1).</summary>
    Star,
}
