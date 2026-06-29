namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A bar chart. Based on Spectre.Console.BarChart
/// </summary>
public partial class BarChart : RenderableControl, Spectre.Console.IHasCulture
{
    #region Constructors
    public BarChart(ChartOrientation orientation, params (string label, double value, Color color)[] items)
    {
        Orientation = orientation;
        foreach (var item in items)
        {
            var index = Interlocked.Increment(ref itemIndex);
            data[index] = new BarChartItem(this, index, item.label, item.value, item.color);
        }
        // Defer the first build to the next render on the UI thread.
        _structureDirty = true;
    }

    public BarChart(params (string label, double value, Color color)[] items) : this(ChartOrientation.Horizontal, items) {}
    #endregion

    #region Properties
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

    public CultureInfo? Culture { get; set; }

    private double? _maxValue;
    public double? MaxValue
    {
        get => _maxValue;
        set => SetAtomicProperty(ref _maxValue, value,
            validate: v => v < 0d ? 0d : v,                // a chart axis max can't be negative
            watch: (_, _) => UI.Invoke(UpdateAllBars));
    }

    public Func<double, CultureInfo, string>? ValueFormatter { get; set; }

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
    public void Update() => Invalidate();

    // Item creation (and the atomic index) happens on the calling thread so AddItem can return immediately;
    // the dictionary mutation and chart rebuild are marshaled to the UI thread.
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
    /// Removes an item by index. The result is reliable only when called on the UI thread.
    /// </summary>
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

    public bool RemoveItem(BarChartItem item) => RemoveItem(item.Index);

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
    protected override bool RendersInteractiveState => false;

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
    protected int itemIndex = -1;
    protected char VerticalUnicodeBar { get; set; } = '█';
    protected char AsciiBar { get; set; } = '-';
    protected static char HorizontalUnicodeBar { get; set; } = '█';
    protected readonly Dictionary<int, BarChartItem> data = new();

    protected Spectre.Console.Grid _grid = new();
    protected Spectre.Console.Grid? _containerGrid = new();
    protected List<IBarControl> _bars = new();

    // Set when a structural change (items, orientation, value display, label) needs the grid rebuilt; consumed
    // on the UI thread in Render. Volatile so a write from a background-thread setter is seen by the UI thread.
    private volatile bool _structureDirty;
    #endregion
}
