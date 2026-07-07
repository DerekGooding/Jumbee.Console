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

    /// <summary>
    /// Adds an OHLC candlestick series — each point drawn as a candle (high/low wick + open/close body) coloured by
    /// direction. <paramref name="up"/> defaults to green (close ≥ open), <paramref name="down"/> to red.
    /// </summary>
    public Plot AddCandles(
        IReadOnlyList<double> xs, IReadOnlyList<double> opens, IReadOnlyList<double> highs,
        IReadOnlyList<double> lows, IReadOnlyList<double> closes, CColor? up = null, CColor? down = null)
    {
        UI.Invoke(() =>
        {
            var u = up ?? new CColor(80, 200, 120);
            var d = down ?? new CColor(230, 90, 90);
            AddElement(plot => plot.AddCandles(xs, opens, highs, lows, closes, u, d));
        });
        return this;
    }

    /// <summary>
    /// Adds a box-and-whisker series from the five-number summary of each box — <paramref name="mins"/>,
    /// <paramref name="q1s"/>, <paramref name="medians"/>, <paramref name="q3s"/>, <paramref name="maxes"/> (all the
    /// same length as <paramref name="xs"/>). <paramref name="color"/> defaults to the palette; <paramref name="medianColor"/>
    /// defaults to <paramref name="color"/>; <paramref name="width"/> is the box width as a fraction (0..1) of the spacing.
    /// </summary>
    public Plot AddBox(
        IReadOnlyList<double> xs, IReadOnlyList<double> mins, IReadOnlyList<double> q1s,
        IReadOnlyList<double> medians, IReadOnlyList<double> q3s, IReadOnlyList<double> maxes,
        CColor? color = null, CColor? medianColor = null, double width = 0.6)
    {
        UI.Invoke(() =>
        {
            var c = color ?? Palette[_seriesCount % Palette.Length];
            var m = medianColor ?? c;
            AddElement(plot => plot.AddBox(xs, mins, q1s, medians, q3s, maxes, c, m, width));
        });
        return this;
    }

    /// <summary>
    /// Adds a box-and-whisker series from raw data <paramref name="groups"/> — one box per group, with the quartiles
    /// (min/Q1/median/Q3/max, linear-interpolation percentiles) computed here. Boxes are positioned at
    /// <paramref name="positions"/> (defaults to 1, 2, 3, …). <paramref name="color"/> defaults to the palette.
    /// </summary>
    public Plot AddBoxes(
        IReadOnlyList<IReadOnlyList<double>> groups, IReadOnlyList<double>? positions = null,
        CColor? color = null, CColor? medianColor = null, double width = 0.6)
    {
        UI.Invoke(() =>
        {
            var xs = new List<double>();
            var mins = new List<double>();
            var q1s = new List<double>();
            var medians = new List<double>();
            var q3s = new List<double>();
            var maxes = new List<double>();
            for (int i = 0; i < groups.Count; i++)
            {
                if (!Quartiles(groups[i], out var min, out var q1, out var med, out var q3, out var max))
                    continue;
                xs.Add(positions is not null && i < positions.Count ? positions[i] : i + 1);
                mins.Add(min); q1s.Add(q1); medians.Add(med); q3s.Add(q3); maxes.Add(max);
            }

            if (xs.Count == 0) return;
            var c = color ?? Palette[_seriesCount % Palette.Length];
            var m = medianColor ?? c;
            AddElement(plot => plot.AddBox(xs, mins, q1s, medians, q3s, maxes, c, m, width));
        });
        return this;
    }

    // The five-number summary of the finite values (linear-interpolation percentiles, numpy's default), or false
    // when there are no finite values.
    private static bool Quartiles(IReadOnlyList<double> values, out double min, out double q1, out double median, out double q3, out double max)
    {
        min = q1 = median = q3 = max = 0;
        var sorted = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToArray();
        if (sorted.Length == 0) return false;
        Array.Sort(sorted);

        min = sorted[0];
        max = sorted[^1];
        q1 = Percentile(sorted, 0.25);
        median = Percentile(sorted, 0.50);
        q3 = Percentile(sorted, 0.75);
        return true;

        static double Percentile(double[] s, double p)
        {
            if (s.Length == 1) return s[0];
            double rank = p * (s.Length - 1);
            int lo = (int)Math.Floor(rank);
            int hi = (int)Math.Ceiling(rank);
            return s[lo] + (s[hi] - s[lo]) * (rank - lo);
        }
    }

    /// <summary>
    /// Adds vertical error bars with symmetric error — each point (<paramref name="xs"/>, <paramref name="ys"/>)
    /// drawn as a whisker of ±<paramref name="errors"/> with caps and a centre marker. <paramref name="color"/>
    /// defaults to the palette; <paramref name="capWidth"/> is the cap half-width in cells.
    /// </summary>
    public Plot AddErrorBars(IReadOnlyList<double> xs, IReadOnlyList<double> ys, IReadOnlyList<double> errors, CColor? color = null, int capWidth = 1) =>
        AddErrorBars(xs, ys, errors, errors, color, capWidth);

    /// <summary>
    /// Adds vertical error bars with asymmetric error — each point (<paramref name="xs"/>, <paramref name="ys"/>)
    /// drawn as a whisker from <c>y − errLow</c> to <c>y + errHigh</c> with caps and a centre marker.
    /// <paramref name="color"/> defaults to the palette; <paramref name="capWidth"/> is the cap half-width in cells.
    /// </summary>
    public Plot AddErrorBars(
        IReadOnlyList<double> xs, IReadOnlyList<double> ys, IReadOnlyList<double> errLows, IReadOnlyList<double> errHighs,
        CColor? color = null, int capWidth = 1)
    {
        UI.Invoke(() =>
        {
            var c = color ?? Palette[_seriesCount % Palette.Length];
            AddElement(plot => plot.AddErrorBars(xs, ys, errLows, errHighs, c, capWidth));
        });
        return this;
    }

    /// <summary>
    /// Adds grouped (side-by-side) vertical bars — one sub-bar per series at each x. <paramref name="series"/> is
    /// one value list per series (each the same length as <paramref name="xs"/>). <paramref name="colors"/> defaults
    /// to the palette (one per series); <paramref name="width"/> is the group width as a fraction (0..1) of the spacing.
    /// </summary>
    public Plot AddGroupedBars(
        IReadOnlyList<double> xs, IReadOnlyList<IReadOnlyList<double>> series,
        IReadOnlyList<CColor>? colors = null, double baseline = 0, double width = 0.8)
    {
        UI.Invoke(() =>
        {
            var cs = ColorsFor(series.Count, colors);
            AddElement(plot => plot.AddGroupedBars(xs, series, cs, baseline, width));
        });
        return this;
    }

    /// <summary>
    /// Adds stacked vertical bars — the series stacked from <paramref name="baseline"/> at each x.
    /// <paramref name="series"/> is one value list per series (each the same length as <paramref name="xs"/>).
    /// <paramref name="colors"/> defaults to the palette (one per series).
    /// </summary>
    public Plot AddStackedBars(
        IReadOnlyList<double> xs, IReadOnlyList<IReadOnlyList<double>> series,
        IReadOnlyList<CColor>? colors = null, double baseline = 0, double width = 0.8)
    {
        UI.Invoke(() =>
        {
            var cs = ColorsFor(series.Count, colors);
            AddElement(plot => plot.AddStackedBars(xs, series, cs, baseline, width));
        });
        return this;
    }

    /// <summary>
    /// Adds horizontal bars — each category at a Y <paramref name="position"/> with its bar growing along X from
    /// <paramref name="baseline"/> to its value. <paramref name="color"/> defaults to the palette; <paramref name="width"/>
    /// is the bar thickness as a fraction (0..1) of the spacing.
    /// </summary>
    public Plot AddHBars(IReadOnlyList<double> positions, IReadOnlyList<double> values, CColor? color = null, double baseline = 0, double width = 0.8)
    {
        UI.Invoke(() =>
        {
            var c = color ?? Palette[_seriesCount % Palette.Length];
            AddElement(plot => plot.AddHBars(positions, values, c, baseline, width));
        });
        return this;
    }

    /// <summary>
    /// Adds a heatmap: a grid of <paramref name="values"/> (one list per row, row 0 drawn at the top) tiled over
    /// the plot area, each cell coloured by <paramref name="colormap"/>. Values are normalised into
    /// [<paramref name="min"/>, <paramref name="max"/>], defaulting to the data's own min/max. NaN cells are blank.
    /// Pass <paramref name="cellText"/> to draw each cell's value as centred text (readable-contrast on the cell
    /// colour) — e.g. <c>v =&gt; ((int)v).ToString()</c> for a confusion matrix.
    /// </summary>
    public Plot AddHeatmap(
        IReadOnlyList<IReadOnlyList<double>> values, PlotColormap colormap = PlotColormap.Viridis,
        double? min = null, double? max = null, Func<double, string>? cellText = null)
    {
        UI.Invoke(() =>
        {
            int rows = values.Count;
            if (rows == 0) return;
            int cols = values[0].Count;
            if (cols == 0) return;

            double dataMin = double.PositiveInfinity, dataMax = double.NegativeInfinity;
            foreach (var row in values)
                foreach (var v in row)
                    if (!double.IsNaN(v) && !double.IsInfinity(v))
                    {
                        if (v < dataMin) dataMin = v;
                        if (v > dataMax) dataMax = v;
                    }
            if (double.IsInfinity(dataMin)) return;   // no finite values

            double lo = min ?? dataMin, hi = max ?? dataMax;
            var map = ColormapFunc(colormap);
            // The grid tiles the unit-per-cell rectangle 0..cols × 0..rows; use Configure* to relabel the axes.
            AddElement(plot => plot.AddHeatmap(values, 0, cols, 0, rows, lo, hi, v => map(v), cellText is null ? null : v => cellText(v)));
        });
        return this;
    }

    /// <summary>
    /// Adds a confusion matrix — an annotated heatmap of <paramref name="counts"/> (row = actual class top-to-bottom,
    /// column = predicted class), each cell coloured by <paramref name="colormap"/> and labelled with its count. When
    /// <paramref name="rowLabels"/>/<paramref name="colLabels"/> are given, the class names are placed as categorical
    /// axis ticks at the cell centres. A wrapper over <see cref="AddHeatmap"/> + <see cref="SetXTicks"/>/<see cref="SetYTicks"/>.
    /// </summary>
    public Plot AddConfusionMatrix(
        IReadOnlyList<IReadOnlyList<double>> counts, IReadOnlyList<string>? rowLabels = null,
        IReadOnlyList<string>? colLabels = null, PlotColormap colormap = PlotColormap.Heat)
    {
        AddHeatmap(counts, colormap, cellText: v => ((long)Math.Round(v)).ToString());

        int rows = counts.Count;
        int cols = rows > 0 ? counts[0].Count : 0;
        // The grid tiles 0..cols × 0..rows with row 0 at the top, so column c's centre is at x = c+0.5 and row r's
        // centre is at y = rows−r−0.5 (image y is up).
        if (colLabels is not null && cols > 0)
            SetXTicks([.. Enumerable.Range(0, cols).Select(c => (c + 0.5, c < colLabels.Count ? colLabels[c] : ""))]);
        if (rowLabels is not null && rows > 0)
            SetYTicks([.. Enumerable.Range(0, rows).Select(r => (rows - r - 0.5, r < rowLabels.Count ? rowLabels[r] : ""))]);
        // Categorical ticks keep the grid at exact bounds (edge to edge), so the labels need a reserved margin
        // rather than being attached to the axis inside the grid.
        if (rowLabels is not null || colLabels is not null)
            ConfigureTicks(t => t.Labels.AttachToAxis = false);
        return this;
    }

    /// <summary>
    /// Pins the vertical axis to a fixed <paramref name="min"/>..<paramref name="max"/> range, so live updates move
    /// only the data (values outside the range are clipped) instead of the axis rescaling to the data each frame.
    /// Call once before streaming; <see cref="AutoRangeY"/> restores auto-scaling.
    /// </summary>
    public Plot SetYRange(double min, double max) => Configure(p => p.FixedYRange = (min, max));

    /// <summary>Pins the horizontal axis to a fixed range; see <see cref="SetYRange"/>.</summary>
    public Plot SetXRange(double min, double max) => Configure(p => p.FixedXRange = (min, max));

    /// <summary>
    /// Makes the horizontal axis a sliding window of <paramref name="width"/> units — it shows the most recent
    /// <c>[max(0, dataMax − width), dataMax]</c> of a monotonic (time-like) series, so the axis only advances
    /// rightward and never shows x &lt; 0. Ideal for streaming/financial data. <see cref="AutoRangeX"/> restores auto.
    /// </summary>
    public Plot SetXWindow(double width) => Configure(p => p.XWindow = width);

    /// <summary>Restores auto-scaling of the vertical axis to the data (undoes <see cref="SetYRange"/>).</summary>
    public Plot AutoRangeY() => Configure(p => p.FixedYRange = null);

    /// <summary>Restores auto-scaling of the horizontal axis (undoes <see cref="SetXRange"/>/<see cref="SetXWindow"/>).</summary>
    public Plot AutoRangeX() => Configure(p => { p.FixedXRange = null; p.XWindow = null; });

    /// <summary>Sets explicit horizontal-axis ticks (value + label) — e.g. categorical class names at cell centres.
    /// Replaces the auto numeric ticks and keeps the data bounds unadjusted. For labels in a reserved margin (rather
    /// than attached inside the grid), pair with <c>ConfigureTicks(t =&gt; t.Labels.AttachToAxis = false)</c> —
    /// <see cref="AddConfusionMatrix"/> does this for you.</summary>
    public Plot SetXTicks(IReadOnlyList<(double value, string label)> ticks) =>
        Configure(p => p.Ticks.CustomXTicks = ticks);

    /// <summary>Sets explicit vertical-axis ticks (value + label). See <see cref="SetXTicks"/>.</summary>
    public Plot SetYTicks(IReadOnlyList<(double value, string label)> ticks) =>
        Configure(p => p.Ticks.CustomYTicks = ticks);

    // Maps a colormap choice to a normalised-value → colour function.
    private static Func<double, CColor> ColormapFunc(PlotColormap colormap) => colormap switch
    {
        PlotColormap.Grayscale => t => Ramp(GrayscaleStops, t),
        PlotColormap.Heat => t => Ramp(HeatStops, t),
        PlotColormap.Cool => t => Ramp(CoolStops, t),
        _ => t => Ramp(ViridisStops, t),
    };

    // Piecewise-linear interpolation across evenly-spaced colour stops (t in [0, 1]).
    private static CColor Ramp(CColor[] stops, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        double scaled = t * (stops.Length - 1);
        int i = (int)Math.Floor(scaled);
        if (i >= stops.Length - 1) return stops[^1];
        double f = scaled - i;
        var a = stops[i];
        var b = stops[i + 1];
        return new CColor(
            (byte)(a.Red + (b.Red - a.Red) * f),
            (byte)(a.Green + (b.Green - a.Green) * f),
            (byte)(a.Blue + (b.Blue - a.Blue) * f));
    }

    private static readonly CColor[] ViridisStops =
    [
        new(68, 1, 84), new(59, 82, 139), new(33, 145, 140), new(94, 201, 98), new(253, 231, 37),
    ];
    private static readonly CColor[] HeatStops =
    [
        new(0, 0, 0), new(150, 0, 0), new(230, 90, 0), new(250, 200, 40), new(255, 255, 220),
    ];
    private static readonly CColor[] GrayscaleStops = [new(15, 15, 15), new(245, 245, 245)];
    private static readonly CColor[] CoolStops = [new(0, 220, 220), new(120, 120, 240), new(230, 60, 230)];

    // One colour per series: the caller's colours where given, else the palette cycled by series index.
    private static IReadOnlyList<CColor> ColorsFor(int count, IReadOnlyList<CColor>? colors)
    {
        var result = new CColor[count];
        for (int j = 0; j < count; j++)
            result[j] = colors is not null && j < colors.Count ? colors[j] : Palette[j % Palette.Length];
        return result;
    }

    /// <summary>
    /// Adds a text annotation anchored to the data point (<paramref name="x"/>, <paramref name="y"/>) — e.g. labelling
    /// a candle or data point. <paramref name="fg"/> defaults to white; <paramref name="bg"/> is optional (transparent
    /// when null). <paramref name="dx"/>/<paramref name="dy"/> nudge the label in cells (dy &gt; 0 = above the point);
    /// <paramref name="align"/> anchors it horizontally. Does not rescale the axes.
    /// </summary>
    public Plot AddLabel(double x, double y, string text, CColor? fg = null, CColor? bg = null, PlotLabelAlign align = PlotLabelAlign.Center, int dx = 0, int dy = 1)
    {
        UI.Invoke(() =>
        {
            var f = fg ?? CColor.White;
            var a = align switch
            {
                PlotLabelAlign.Left => LabelAlignment.Left,
                PlotLabelAlign.Right => LabelAlignment.Right,
                _ => LabelAlignment.Center,
            };
            // A label is not a palette series, so it doesn't advance the colour cycle — add its config directly.
            _config.Add(plot => plot.AddLabel(x, y, text, f, bg, a, dx, dy));
            Rebuild();
        });
        return this;
    }

    /// <summary>
    /// Adds a live line/scatter series and returns a <see cref="PlotSeries"/> handle to feed it data as it arrives
    /// (<see cref="PlotSeries.SetData"/>/<see cref="PlotSeries.Push"/>). <paramref name="color"/> defaults to the
    /// palette; <paramref name="brush"/> sets the sub-cell marker. Starts empty.
    /// </summary>
    public PlotSeries AddLiveSeries(CColor? color = null, PlotBrush brush = PlotBrush.Braille)
    {
        var pen = new PointPen(BrushFor(brush), color ?? Palette[_seriesCount % Palette.Length]);
        var handle = new PlotSeries(this, (cplot, xs, ys) => cplot.AddSeries(xs, ys, pen));
        RegisterLive(handle);
        return handle;
    }

    /// <summary>
    /// Adds a live bar series and returns a <see cref="PlotSeries"/> handle. Feed it with
    /// <see cref="PlotSeries.SetValues"/> (bars at x = 1, 2, 3, …) or <see cref="PlotSeries.SetData"/>.
    /// <paramref name="color"/> defaults to the palette. Starts empty.
    /// </summary>
    public PlotSeries AddLiveBars(CColor? color = null, double baseline = 0, double width = 0.8)
    {
        var c = color ?? Palette[_seriesCount % Palette.Length];
        var handle = new PlotSeries(this, (cplot, xs, ys) => cplot.AddBars(xs, ys, c, baseline, width));
        RegisterLive(handle);
        return handle;
    }

    // Registers a live series so its (mutable) data is replayed on every rebuild.
    private void RegisterLive(PlotSeries handle) =>
        UI.Invoke(() =>
        {
            _seriesCount++;
            _config.Add(handle.Apply);
            Rebuild();
        });

    // Applies a live-series data mutation on the UI thread, then redraws. The buffers are only ever touched here,
    // so a plain List stays race-free even when data arrives on a background thread.
    internal void UpdateSeries(Action mutate) => UI.Invoke(() => { mutate(); Rebuild(); });

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

/// <summary>Selects the colour map a <see cref="Plot"/> heatmap uses to turn cell values into colours.</summary>
public enum PlotColormap
{
    /// <summary>Perceptually-uniform dark-purple → blue → teal → green → yellow (the default).</summary>
    Viridis,
    /// <summary>Classic heat: black → red → orange → yellow → white.</summary>
    Heat,
    /// <summary>Dark → light grey.</summary>
    Grayscale,
    /// <summary>Cyan → blue → magenta.</summary>
    Cool,
}

/// <summary>Horizontal anchoring of a <see cref="Plot"/> annotation label relative to its point.</summary>
public enum PlotLabelAlign
{
    /// <summary>The text starts at the point and runs right.</summary>
    Left,
    /// <summary>The text is centred on the point.</summary>
    Center,
    /// <summary>The text ends at the point.</summary>
    Right,
}
