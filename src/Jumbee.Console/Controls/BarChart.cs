namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A bar chart.
/// </summary>
/// <remarks>Based on Spectre.Console.BarChart.</remarks>
public partial class BarChart : RenderableControl, Spectre.Console.IHasCulture
{
    #region Constructors
    /// <summary>Initializes a new <see cref="BarChart"/> with the given orientation and initial items.</summary>
    public BarChart(ChartOrientation orientation, params (string label, double value, Color color)[] items)
    {
        Focusable = false;   // a passive display control: never a focus/tab target
        Orientation = orientation;
        foreach (var item in items)
        {
            var index = Interlocked.Increment(ref itemIndex);
            data[index] = new BarChartItem(this, index, item.label, item.value, item.color);
        }
        // Defer the first build to the next render on the UI thread.
        _structureDirty = true;
    }

    /// <summary>Initializes a new horizontal <see cref="BarChart"/> with the given initial items.</summary>
    public BarChart(params (string label, double value, Color color)[] items) : this(ChartOrientation.Horizontal, items) {}
    #endregion

    #region Properties
    /// <summary>The chart's items.</summary>
    public ICollection<BarChartItem> Data => data.Values;

    /// <summary>
    /// Gets or sets the bar chart orientation.
    /// </summary>
    public ChartOrientation Orientation
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                MarkStructureDirty();
            }
        }
    }

    /// <summary>
    /// Gets or sets the bar chart label.
    /// </summary>
    public string? Label
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                MarkStructureDirty();
            }
        }
    }

    /// <summary>
    /// Gets or sets the bar chart label alignment.
    /// </summary>
    public Justify? LabelAlignment
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                MarkStructureDirty();
            }
        }
    }

    private bool _showValues = true;
    /// <summary>Whether each bar's value is shown alongside it. Defaults to <see langword="true"/>.</summary>
    public bool ShowValues
    {
        get => _showValues;
        set
        {
            if (_showValues != value)
            {
                _showValues = value;
                MarkStructureDirty();
            }
        }
    }

    /// <summary>The culture used to format values, or <see langword="null"/> for the invariant culture.</summary>
    public CultureInfo? Culture { get; set; }

    private double? _maxValue;
    /// <summary>The axis maximum, or <see langword="null"/> to derive it from the largest item value. Never negative.</summary>
    public double? MaxValue
    {
        get => _maxValue;
        set => SetAtomicProperty(ref _maxValue, value,
            validate: v => v < 0d ? 0d : v,                // a chart axis max can't be negative
            watch: (_, _) => UI.Invoke(UpdateAllBars));
    }

    /// <summary>An optional custom formatter for bar values, or <see langword="null"/> to use the default culture formatting.</summary>
    public Func<double, CultureInfo, string>? ValueFormatter { get; set; }

    /// <summary>The chart width in cells; setting it re-lays out all bars.</summary>
    public int? BarWidth
    {
        get => Width;
        set
        {
            if (value.HasValue && Width != value.Value)
            {
                Width = value.Value;   // Control.Width marshals via UI.Invoke
                UI.Invoke(() =>
                {
                    UpdateAllBars();
                    Invalidate();
                });
            }
        }
    }

    /// <summary>When set to <see langword="true"/>, centers the chart label (sets <see cref="LabelAlignment"/> to <see cref="Justify.Center"/>).</summary>
    public bool CenterLabel
    {
        set
        {
            if (value)
            {
                LabelAlignment = Justify.Center;   // its setter marks the structure dirty
            }
        }
    }
    #endregion

    #region Indexers
    /// <summary>Sets the values of the items matching the given labels (counts must match).</summary>
    public double[] this[params string[] labels]
    {
        set
        {
            if (labels.Length != value.Length)
            {
                throw new ArgumentException("Labels and values count mismatch");
            }

            var values = value;
            UI.Invoke(() =>
            {
                foreach (var kvp in data)
                {
                    var idx = Array.IndexOf(labels, kvp.Value.Label);
                    if (idx >= 0)
                    {
                        kvp.Value.Value = values[idx];   // triggers UpdateItemValue (inline on the UI thread)
                    }
                }
            });
        }
    }
    #endregion

    #region Methods
    /// <summary>Requests a redraw of the chart.</summary>
    public void Update() => Invalidate();

    // Item creation (and the atomic index) happens on the calling thread so AddItem can return immediately;
    // the dictionary mutation and chart rebuild are marshaled to the UI thread.
    /// <summary>Adds an item with the given label, value and colour, and returns it.</summary>
    public BarChartItem AddItem(string label, double value, Color color)
    {
        var index = Interlocked.Increment(ref itemIndex);
        var item = new BarChartItem(this, index, label, value, color);
        UI.Invoke(() =>
        {
            data[index] = item;
            MarkStructureDirty();
        });
        return item;
    }

    /// <summary>Adds multiple items and returns this chart for chaining.</summary>
    public BarChart AddItems(params (string label, double value, Color color)[] items)
    {
        var added = items.Select(i => (index: Interlocked.Increment(ref itemIndex), i)).ToArray();
        UI.Invoke(() =>
        {
            foreach (var (index, i) in added)
            {
                data[index] = new BarChartItem(this, index, i.label, i.value, i.color);
            }
            MarkStructureDirty();
        });
        return this;
    }

    /// <summary>
    /// Removes an item by index.
    /// </summary>
    /// <remarks>The result is reliable only when called on the UI thread.</remarks>
    public bool RemoveItem(int index)
    {
        var removed = false;
        UI.Invoke(() =>
        {
            if (data.Remove(index, out var item))
            {
                MarkStructureDirty();
                item.Detach();
                removed = true;
            }
        });
        return removed;
    }

    /// <summary>Removes the given item. Reliable only when called on the UI thread.</summary>
    public bool RemoveItem(BarChartItem item) => RemoveItem(item.Index);

    /// <inheritdoc/>
    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        var width = Math.Min(Width, maxWidth);
        return new Measurement(width, width);
    }

    private int GetListIndex(int id)
    {
        // Data keys are sorted in CreateChartElements to populate the lists; match that order.
        var sortedKeys = data.Keys.OrderBy(k => k).ToList();
        return sortedKeys.IndexOf(id);
    }

    internal void UpdateItemValue(int id, double value)
    {
        UI.Invoke(() =>
        {
            int i = GetListIndex(id);
            if (i >= 0 && i < _bars.Count)
            {
                var bar = _bars[i];
                if (bar is VerticalBar vb)
                {
                    vb.Value = value;
                    int itemLabelHeight = 1;
                    int itemValueHeight = ShowValues ? 1 : 0;
                    int effectiveHeight = Height > 0 ? Height : (10 + itemLabelHeight + itemValueHeight + (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
                    vb.Height = Math.Max(1, effectiveHeight - itemLabelHeight - itemValueHeight - (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
                }
                else if (bar is HorizontalBar pb)
                {
                    pb.Value = value;
                    int itemLabelWidth = 1;
                    int itemValueWidth = ShowValues ? 1 : 0;
                    int effectiveWidth = Width > 0 ? Width : (10 + itemLabelWidth + itemValueWidth);
                    pb.Width = Math.Max(1, effectiveWidth - itemLabelWidth - itemValueWidth);
                }
                Invalidate();
            }
        });
    }

    internal void UpdateItemColor(int id, Color color)
    {
        UI.Invoke(() =>
        {
            int i = GetListIndex(id);
            if (i >= 0)
            {
                if (i < _bars.Count) _bars[i].Color = color;
                Invalidate();
            }
        });
    }

    /// <summary>Recomputes every bar's max value and size to match the current data and control dimensions.</summary>
    protected void UpdateAllBars()
    {
        var maxValue = Math.Max(MaxValue ?? 0d, data.Values.Select(item => item.Value).DefaultIfEmpty(0).Max());
        foreach (var bar in _bars)
        {
            bar.MaxValue = maxValue;
            if (bar is HorizontalBar pb)
            {
                pb.Width = Width;
            }
            else if (bar is VerticalBar vb)
            {
                int itemLabelHeight = 1;
                int itemValueHeight = ShowValues ? 1 : 0;
                int effectiveHeight = Height > 0 ? Height : (10 + itemLabelHeight + itemValueHeight + (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
                vb.Height = Math.Max(1, effectiveHeight - itemLabelHeight - itemValueHeight - (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
            }
        }
    }

    /// <summary>Builds the optional container grid that stacks the chart <see cref="Label"/> above the bars.</summary>
    protected void CreateChartLabel()
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            _containerGrid = null;
        }
        else
        {
            _containerGrid = new Spectre.Console.Grid();
            _containerGrid.Collapse();
            _containerGrid.AddColumn(new GridColumn().Centered());

            _containerGrid.AddRow(new Markup(Label).Justify(LabelAlignment.HasValue ? (Spectre.Console.Justify)LabelAlignment.Value : Spectre.Console.Justify.Center));
            _containerGrid.AddRow(_grid);
        }
    }

    /// <summary>Rebuilds the grid and bar renderables from the current data and orientation.</summary>
    protected void CreateChartElements()
    {
        _grid = new Spectre.Console.Grid();
        _grid.Collapse();
        _bars.Clear();
        var sortedData = data.Values.OrderBy(x => x.Index).ToList();
        var maxValue = Math.Max(MaxValue ?? 0d, sortedData.Select(item => item.Value).DefaultIfEmpty(0).Max());

        if (Orientation == ChartOrientation.Vertical)
        {
            foreach (var _ in sortedData)
            {
                _grid.AddColumn(new GridColumn().Centered());
            }

            int itemLabelHeight = 1;
            int itemValueHeight = ShowValues ? 1 : 0;
            int effectiveHeight = Height > 0 ? Height : (10 + itemLabelHeight + itemValueHeight + (string.IsNullOrWhiteSpace(Label) ? 0 : 1));
            int barHeight = Math.Max(1, effectiveHeight - itemLabelHeight - itemValueHeight - (string.IsNullOrWhiteSpace(Label) ? 0 : 1));

            var barRenderables = new List<IRenderable>();
            foreach (var item in sortedData)
            {
                var bar = new VerticalBar
                {
                    Value = item.Value,
                    MaxValue = maxValue,
                    Height = barHeight,
                    Color = item.Color,
                    UnicodeBar = VerticalUnicodeBar,
                    AsciiBar = AsciiBar
                };
                _bars.Add(bar);
                barRenderables.Add(bar);
            }
            _grid.AddRow(barRenderables.ToArray());

            if (ShowValues)
            {
                var valueRenderables = new List<IRenderable>();
                foreach (var item in sortedData)
                {
                    var valStr = ValueFormatter != null
                        ? ValueFormatter(item.Value, Culture ?? CultureInfo.InvariantCulture)
                        : item.Value.ToString(Culture ?? CultureInfo.InvariantCulture);
                    var mk = new Markup(valStr, new Spectre.Console.Style(foreground: item.Color));
                    valueRenderables.Add(mk);
                }
                _grid.AddRow(valueRenderables.ToArray());
            }

            var labelRenderables = new List<IRenderable>();
            foreach (var item in sortedData)
            {
                var mk = new Markup(item.Label, new Spectre.Console.Style(foreground: item.Color));
                labelRenderables.Add(mk);
            }
            _grid.AddRow(labelRenderables.ToArray());
        }
        else // Horizontal
        {
            _grid.AddColumn(new GridColumn().PadRight(2).RightAligned());
            _grid.AddColumn(new GridColumn().PadLeft(0));

            foreach (var item in sortedData)
            {
                var mkLabel = new Markup(item.Label, new Spectre.Console.Style(foreground: item.Color));

                var bar = new HorizontalBar
                {
                    Value = item.Value,
                    MaxValue = maxValue,
                    ShowRemaining = false,
                    Color = item.Color,
                    UnicodeBar = HorizontalUnicodeBar,
                    AsciiBar = AsciiBar,
                    ShowValue = ShowValues,
                    Culture = Culture,
                    ValueFormatter = ValueFormatter
                };
                _bars.Add(bar);

                _grid.AddRow(mkLabel, bar);
            }
        }
        CreateChartLabel();
    }

    /// <summary>
    /// Marks the chart's structure (bars, rows/columns, label) as needing a rebuild on the next render and
    /// requests a redraw. The rebuild itself happens lazily inside <see cref="Render"/> on the UI thread, so
    /// multiple structural changes within a frame coalesce into a single <see cref="CreateChartElements"/> call.
    /// </summary>
    private void MarkStructureDirty()
    {
        _structureDirty = true;
        Invalidate();
    }

    // Content-only render (never reads focus/hover): reuse the cached buffer on interactive-state changes.
    /// <inheritdoc/>
    protected override bool RendersInteractiveState => false;

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Rebuild structure lazily on the UI thread, coalescing any structural changes since the last render.
        if (_structureDirty)
        {
            CreateChartElements();
            _structureDirty = false;
        }

        var width = Math.Min(Width, maxWidth);
        var grid = _containerGrid ?? _grid;
        grid.Width = width;
        return ((IRenderable)grid).Render(options, width);
    }
    #endregion

    #region Fields
    /// <summary>The last-assigned item index; incremented atomically to key new items.</summary>
    protected int itemIndex = -1;
    /// <summary>The glyph used to draw filled vertical bars.</summary>
    protected char VerticalUnicodeBar { get; set; } = '█';
    /// <summary>The glyph used to draw bars in ASCII (non-Unicode) mode.</summary>
    protected char AsciiBar { get; set; } = '-';
    /// <summary>The glyph used to draw filled horizontal bars.</summary>
    protected static char HorizontalUnicodeBar { get; set; } = '█';
    /// <summary>The chart items keyed by their index.</summary>
    protected readonly Dictionary<int, BarChartItem> data = new();

    /// <summary>The grid holding the bar renderables.</summary>
    protected Spectre.Console.Grid _grid = new();
    /// <summary>The optional outer grid stacking the label above <see cref="_grid"/>, or <see langword="null"/> when there is no label.</summary>
    protected Spectre.Console.Grid? _containerGrid = new();
    /// <summary>The bar renderables in render order.</summary>
    protected List<IBarControl> _bars = new();

    // Set when a structural change (items, orientation, value display, label) needs the grid rebuilt; consumed
    // on the UI thread in Render. Volatile so a write from a background-thread setter is seen by the UI thread.
    private volatile bool _structureDirty;
    #endregion
}
