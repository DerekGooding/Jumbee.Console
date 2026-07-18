namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using Spectre.Console.Rendering;

/// <summary>
/// A narrow, non-interactive column of right-aligned line numbers, highlighting the active row. Intended as an
/// adornment inside a composite (e.g. <see cref="CodeEditor"/>); width auto-grows with the digit count.
/// </summary>
/// <remarks>
/// By default it numbers rows sequentially (<c>1, 2, 3 …</c>). To stay aligned with <em>soft-wrapped</em> text,
/// set <see cref="RowsProvider"/>: it is pulled every render and returns, per visual row, the number to show
/// (0 = a wrapped continuation row, drawn blank) plus the active row — so a resize stays correct with no event.
/// </remarks>
public class LineNumberGutter : RenderableControl
{
    #region Constructors
    public LineNumberGutter()
    {
        Focusable = false;   // an adornment: never in the tab order, never owns the cursor
        Width = DigitsWidth();
        ApplyTheme();
    }
    #endregion

    #region Properties
    /// <summary>The total number of lines to number. The control widens as the digit count grows.</summary>
    public int LineCount
    {
        get => _lineCount;
        set => SetAtomicProperty(ref _lineCount, Math.Max(1, value), updatesLayout: true, watch: (_, _) => Width = DigitsWidth());
    }

    /// <summary>The zero-based active row, drawn in <see cref="ActiveStyle"/> (used when <see cref="RowsProvider"/> is null).</summary>
    public int ActiveLine { get => _activeLine; set => SetAtomicProperty(ref _activeLine, value); }

    /// <summary>
    /// Optional per-render source of wrap-aware labels: returns, for every visual row, the line number to show
    /// (0 = wrapped continuation, drawn blank) and the active visual row.
    /// </summary>
    /// <remarks>
    /// When set it overrides the sequential numbering. Pulled on every render so it tracks edits and resizes. The
    /// gutter renders one label per visual row (it is content-tall, like the editor); a surrounding frame scrolls
    /// them together, so no scroll offset is needed here.
    /// </remarks>
    public Func<(IReadOnlyList<int> labels, int activeRow)>? RowsProvider
    {
        get => _rowsProvider;
        set { _rowsProvider = value; Invalidate(); }
    }

    /// <summary>Requests a repaint (e.g. when the paired editor's line count or caret changes).</summary>
    public void Refresh() => Invalidate();

    /// <summary>Style of inactive line numbers. Defaults to <see cref="IStyleTheme.TextMuted"/>.</summary>
    public Style NumberStyle { get => _numberStyle; set => SetAtomicProperty(ref _numberStyle, value, themeOverride: true); }

    /// <summary>Style of the active line number. Defaults to <see cref="IStyleTheme.TextAccent"/>.</summary>
    public Style ActiveStyle { get => _activeStyle; set => SetAtomicProperty(ref _activeStyle, value, themeOverride: true); }
    #endregion

    #region Methods
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(NumberStyle))) _numberStyle = UI.StyleTheme.TextMuted;
        if (!IsThemeOverridden(nameof(ActiveStyle))) _activeStyle = UI.StyleTheme.TextAccent;
    }

    // Content-only render (never reads focus/hover): reuse the cached buffer on interactive-state changes.
    protected override bool RendersInteractiveState => false;

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var width = Math.Min(ActualWidth > 0 ? ActualWidth : maxWidth, maxWidth);
        if (width <= 0) yield break;
        var digits = Math.Max(1, width - 1);   // reserve one trailing column as a separator
        var rows = Math.Max(1, ActualHeight);

        var provided = _rowsProvider?.Invoke();   // wrap-aware labels, pulled fresh each render

        for (var r = 0; r < rows; r++)
        {
            int label;
            bool active;
            if (provided is { } p)
            {
                label = r < p.labels.Count ? p.labels[r] : 0;   // 0 = continuation / past the end -> blank
                active = r == p.activeRow;
            }
            else
            {
                label = r < _lineCount ? r + 1 : 0;             // standalone: sequential numbering
                active = r == _activeLine;
            }

            if (label > 0)
            {
                var num = label.ToString().PadLeft(digits) + " ";
                yield return new Segment(num, (active ? _activeStyle : _numberStyle).SpectreConsoleStyle);
            }
            else
            {
                yield return new Segment(new string(' ', width), _numberStyle.SpectreConsoleStyle);
            }
            if (r < rows - 1) yield return Segment.LineBreak;
        }
    }

    private int DigitsWidth() => Math.Max(MinDigits, _lineCount.ToString().Length) + 1;
    #endregion

    #region Fields
    private const int MinDigits = 2;
    private int _lineCount = 1;
    private int _activeLine;
    private Func<(IReadOnlyList<int> labels, int activeRow)>? _rowsProvider;
    private Style _numberStyle;
    private Style _activeStyle;
    #endregion
}
