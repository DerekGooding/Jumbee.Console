namespace Jumbee.Console;

using System;
using System.Collections.Generic;

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
            _seriesCount++;
            _config.Add(plot => plot.AddSeries(xs, ys, p));
            Rebuild();
        });
        return this;
    }

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

        // A plot needs room for the axes/labels and at least one series; ConsolePlot can also throw on degenerate
        // data (e.g. a single point where min == max). Guard so a too-small or empty plot renders blank instead of
        // crashing the UI thread.
        if (_plot is null || w < MinWidth || h < MinHeight) return;
        try
        {
            _plot.Draw();
            _plot.Render();   // blits GetImage() into consoleBuffer (see PlotImage)
        }
        catch
        {
            consoleBuffer.Initialize();
        }
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
