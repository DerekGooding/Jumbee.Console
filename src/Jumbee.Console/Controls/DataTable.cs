namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console;
using Spectre.Console.Rendering;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// An interactive data grid. Columns and rows are supplied as text; the visible row window is laid out by
/// Spectre.Console's <see cref="Table"/> (column sizing, borders, wrapping), and this control adds the
/// interactivity Spectre lacks: a highlighted selected row, keyboard navigation, a scroll viewport over many rows
/// with its own scrollbar, click-to-select, and <see cref="SelectionChanged"/>/<see cref="RowActivated"/> events.
/// The header stays fixed while the rows scroll. (Inline cell editing is not supported yet.)
/// </summary>
public class DataTable : Control
{
    #region Constructors
    public DataTable(params string[] columns)
    {
        _columns = columns?.ToList() ?? new List<string>();
    }
    #endregion

    #region Events
    /// <summary>Raised when the selected row changes; the argument is the new row index (or -1 when empty).</summary>
    public event EventHandler<int>? SelectionChanged;

    /// <summary>Raised when the selected row is activated (Enter / double-click); the argument is the row index.</summary>
    public event EventHandler<int>? RowActivated;
    #endregion

    #region Properties
    public override bool HandlesInput => true;
    protected override bool WantsMouse => true;

    public IReadOnlyList<string> Columns => _columns;
    public int RowCount => _rows.Count;

    /// <summary>The selected row index, or -1 when there are no rows.</summary>
    public int SelectedIndex
    {
        get => _rows.Count == 0 ? -1 : _selected;
        set => Select(value);
    }

    /// <summary>The selected row's cells, or <see langword="null"/> when there are no rows.</summary>
    public string[]? SelectedRow => _rows.Count == 0 ? null : _rows[Math.Clamp(_selected, 0, _rows.Count - 1)];
    #endregion

    #region Methods
    public void AddColumn(string header)
    {
        _columns.Add(header ?? string.Empty);
        _chromeTotal = -1;   // column set changed -> re-measure header/border height
        Invalidate();
    }

    /// <summary>Appends a row and returns its index.</summary>
    public int AddRow(params string[] cells)
    {
        _rows.Add(cells ?? []);
        Invalidate();
        return _rows.Count - 1;
    }

    public void RemoveRow(int index)
    {
        if (index < 0 || index >= _rows.Count) return;
        _rows.RemoveAt(index);
        if (_selected >= _rows.Count) _selected = Math.Max(0, _rows.Count - 1);
        Invalidate();
    }

    public void Clear()
    {
        _rows.Clear();
        _selected = 0;
        _scroll = 0;
        Invalidate();
    }

    protected internal override HelpInfo? GetHelpInfo() => new HelpInfo("Table", "Data table", "A scrollable data grid.")
        .WithKey("Up / Down", "Select a row")
        .WithKey("PgUp / PgDn", "Page")
        .WithKey("Enter", "Activate the row");

    protected override void OnInput(InputEvent inputEvent)
    {
        var visible = Math.Max(1, VisibleRows());
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.UpArrow: Select(_selected - 1); break;
            case ConsoleKey.DownArrow: Select(_selected + 1); break;
            case ConsoleKey.Home: Select(0); break;
            case ConsoleKey.End: Select(_rows.Count - 1); break;
            case ConsoleKey.PageUp: Select(_selected - visible); break;
            case ConsoleKey.PageDown: Select(_selected + visible); break;
            case ConsoleKey.Enter:
                if (_rows.Count > 0) RowActivated?.Invoke(this, _selected);
                break;
            default: return;
        }
        inputEvent.Handled = true;
    }

    protected override void OnClick(Position position)
    {
        // Map the clicked screen row back to a data row (the rows start below the header chrome).
        var dataRow = _scroll + (position.Y - ChromeTop());
        if (dataRow >= 0 && dataRow < _rows.Count) Select(dataRow);
    }

    private void Select(int index)
    {
        if (_rows.Count == 0) return;
        index = Math.Clamp(index, 0, _rows.Count - 1);
        if (index == _selected) return;
        _selected = index;
        Invalidate();
        SelectionChanged?.Invoke(this, _selected);
    }

    protected override void Render()
    {
        ansiConsole.Clear(true);
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0 || _columns.Count == 0) return;

        var visible = VisibleRows();
        EnsureSelectedVisible(visible);

        // Lay out the visible window via a Spectre Table sized to leave the last column for the scrollbar.
        var shown = Math.Min(visible, Math.Max(0, _rows.Count - _scroll));
        ansiConsole.Write(BuildTable(_scroll, shown));

        HighlightSelectedRow(visible);
        DrawScrollBar(visible);
    }

    // Fills the buffer's selected-row line with the Selection style (full width, including cell padding) — Spectre
    // can't background a whole row, so we recolour the rendered cells in place, keeping each glyph.
    private void HighlightSelectedRow(int visible)
    {
        if (_rows.Count == 0) return;
        var offset = _selected - _scroll;
        if (offset < 0 || offset >= visible) return;
        var line = ChromeTop() + offset;
        if (line < 0 || line >= ActualHeight) return;

        var sel = UI.StyleTheme.Selection;
        var fg = sel.ForegroundColor?.ToConsoleGUIColor();
        var bg = sel.BackgroundColor?.ToConsoleGUIColor();
        for (var x = 1; x < ContentWidth - 1; x++)   // inside the left/right borders
        {
            var ch = consoleBuffer[x, line].Character;
            consoleBuffer.Write(new Position(x, line), new Character(ch.Content ?? ' ', fg, bg, ch.Decoration));
        }
    }

    private void DrawScrollBar(int visible)
    {
        var col = ActualWidth - 1;
        if (col < 0 || _rows.Count <= visible || visible <= 0) return;   // nothing scrolled off

        var top = ChromeTop();
        var thumb = Math.Clamp((int)((long)visible * visible / _rows.Count), 1, visible);
        var maxScroll = _rows.Count - visible;
        var thumbPos = maxScroll <= 0 ? 0 : (int)((long)(visible - thumb) * _scroll / maxScroll);
        for (var i = 0; i < visible; i++)
        {
            var y = top + i;
            if (y < 0 || y >= ActualHeight) continue;
            var isThumb = i >= thumbPos && i < thumbPos + thumb;
            consoleBuffer.Write(new Position(col, y), isThumb ? ScrollThumb : ScrollTrack);
        }
    }

    private Table BuildTable(int from, int count)
    {
        var table = new Table { Border = TableBorder.Rounded, Width = Math.Max(1, ContentWidth), Expand = true };
        foreach (var column in _columns)
            table.AddColumn(new TableColumn(new Markup(Markup.Escape(column))) { NoWrap = true });   // 1 line per row
        for (var i = from; i < from + count && i < _rows.Count; i++)
            table.AddRow(_rows[i].Select(c => (IRenderable)new Markup(Markup.Escape(c ?? string.Empty))).ToArray());
        return table;
    }

    private void EnsureSelectedVisible(int visible)
    {
        if (_rows.Count == 0) { _scroll = 0; return; }
        _selected = Math.Clamp(_selected, 0, _rows.Count - 1);
        if (visible <= 0) { _scroll = _selected; return; }
        if (_selected < _scroll) _scroll = _selected;
        else if (_selected >= _scroll + visible) _scroll = _selected - visible + 1;
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _rows.Count - visible));
    }

    // Data rows that fit below the header chrome.
    private int VisibleRows() => Math.Max(0, ActualHeight - ChromeTotal());

    // Non-data chrome rows (top border + header + header separator + bottom border). Measured from a ONE-row probe
    // — a header-only table omits the header separator that appears once there is data, so it would mislead.
    private int ChromeTotal() { Measure(); return _chromeTotal; }

    // Rows drawn above the first data row (everything except the bottom border).
    private int ChromeTop() { Measure(); return _chromeTop; }

    private void Measure()
    {
        // Re-measure when the columns change OR the width changes (an early measure at a transient tiny width would
        // render a degenerate table and cache the wrong chrome).
        if (_chromeTotal >= 0 && _measuredWidth == ContentWidth) return;

        var probe = new Table { Border = TableBorder.Rounded, Width = Math.Max(1, ContentWidth), Expand = true };
        foreach (var column in _columns)
            probe.AddColumn(new TableColumn(new Markup(Markup.Escape(column))) { NoWrap = true });
        probe.AddRow(_columns.Select(_ => (IRenderable)new Markup(" ")).ToArray());   // one (no-wrap) data row
        // Count rendered rows by newlines (how the buffer writer advances), not SplitLines. The table emits a
        // trailing newline after its bottom border, so the newline count equals the visible line count:
        // top + header + separator + row + bottom = 5 for this one-row probe.
        var newlines = 0;
        foreach (var s in probe.GetSegments(ansiConsole))
        {
            newlines += s.TextSpan.Count('\n');
        }

        var lines = Math.Max(1, newlines);
        _chromeTotal = Math.Max(0, lines - 1);   // everything that isn't the data row
        _chromeTop = Math.Max(0, lines - 2);     // everything above the data row
        _measuredWidth = ContentWidth;
    }

    // The columns the table draws into: the control width minus the reserved scrollbar column.
    private int ContentWidth => Math.Max(1, ActualWidth - ScrollbarWidth);
    #endregion

    #region Fields
    private const int ScrollbarWidth = 1;
    private static readonly Character ScrollThumb = new('█', new CColor(0x9e, 0x9e, 0x9e), null, ConsoleGUI.Data.Decoration.None);
    private static readonly Character ScrollTrack = new('░', new CColor(0x44, 0x44, 0x44), null, ConsoleGUI.Data.Decoration.None);
    private readonly List<string> _columns;
    private readonly List<string[]> _rows = new();
    private int _selected;
    private int _scroll;
    private int _chromeTotal = -1;   // -1 = not yet measured (re-measured when columns or width change)
    private int _chromeTop = -1;
    private int _measuredWidth = -1;
    #endregion
}
