namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Interop;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

/// <summary>
/// Displays a flat list of items and allows user input navigation and selection.
/// </summary>
public partial class ListBox : RenderableControl
{
    #region Constructors
    public ListBox() => ApplyTheme();

    public ListBox(params IRenderable[] items) : this() => AddItems(items);

    public ListBox(params string[] items) : this() => AddItems(items);

    #endregion

    #region Properties
    public ICollection<ListBoxItem> Items => _items.Values;

    /// <summary>Foreground of the selected row. Defaults to the theme's <see cref="IStyleTheme.Selection"/>.</summary>
    public Color? SelectedForegroundColor
    {
        get => _selectedForegroundColor;
        set => SetAtomicProperty(ref _selectedForegroundColor, value, themeOverride: true);
    }

    /// <summary>Background of the selected row. Defaults to the theme's <see cref="IStyleTheme.Selection"/>.</summary>
    public Color? SelectedBackgroundColor
    {
        get => _selectedBackgroundColor;
        set => SetAtomicProperty(ref _selectedBackgroundColor, value, themeOverride: true);
    }

    /// <summary>How the selected row is indicated — highlight / underline / caret. Defaults to the theme's
    /// <see cref="IStyleTheme.SelectionStyle"/>.</summary>
    public SelectionStyle SelectionStyle
    {
        get => _selectionStyle;
        set => SetAtomicProperty(ref _selectionStyle, value, themeOverride: true);
    }

    /// <summary>The index of the highlighted item (in item order), clamped to the item range.</summary>
    public int SelectedIndex
    {
        get => _selectionIndex;
        set
        {
            var count = _items.Count;
            var clamped = count == 0 ? 0 : Math.Clamp(value, 0, count - 1);
            if (clamped == _selectionIndex) return;
            _selectionIndex = clamped;
            AutoScroll();
            Invalidate();
            SelectionChanged?.Invoke(this, _selectionIndex);
        }
    }

    /// <summary>The highlighted item, or <see langword="null"/> when empty.</summary>
    public ListBoxItem? SelectedItem
    {
        get
        {
            var ordered = OrderedItems();
            return _selectionIndex >= 0 && _selectionIndex < ordered.Length ? ordered[_selectionIndex] : null;
        }
    }

    /// <summary>An optional menu shown when a row is right-clicked. The right-click first selects that row and
    /// raises <see cref="ContextMenuOpening"/> (with the item), then the menu floats at the pointer. Item handlers
    /// can read <see cref="SelectedItem"/> to act on the right-clicked row. Left <see langword="null"/> = no menu.</summary>
    public ContextMenu? ContextMenu { get; set; }

    public override bool HandlesInput => true;
    #endregion

    #region Events
    /// <summary>Raised when the highlighted index changes (navigation or selection).</summary>
    public event EventHandler<int>? SelectionChanged;
    /// <summary>Raised when an item is committed (Enter or click).</summary>
    public event EventHandler<ListBoxItem>? Committed;
    /// <summary>Raised when the list is cancelled (Escape).</summary>
    public event EventHandler? Cancelled;
    /// <summary>Raised just before <see cref="ContextMenu"/> is shown for a right-clicked row, with that item (now
    /// the selected one). Use it to tailor the menu, or read <see cref="SelectedItem"/> from the menu's own item
    /// handlers. Only fires when <see cref="ContextMenu"/> is set.</summary>
    public event EventHandler<ListBoxItem>? ContextMenuOpening;
    #endregion

    #region Methods        
    // Item creation (and the atomic index) happens on the calling thread so AddItem can return immediately;
    // the dictionary mutation is marshaled to the UI thread (inline when already there) so reads during
    // rendering never see a concurrent write.
    public void AddItems(params IEnumerable<IRenderable> items)
    {
        var added = items.Select(i => new ListBoxItem(this, Interlocked.Increment(ref _itemIndex), i)).ToArray();
        UI.Invoke(() =>
        {
            foreach (var item in added) _items[item.Index] = item;
            Initialize();   // re-measure: the content height (item count) may have changed
        });
    }

    public void AddItems(params IEnumerable<string> items)
    {
        var added = items.Select(s => new ListBoxItem(this, Interlocked.Increment(ref _itemIndex), s)).ToArray();
        UI.Invoke(() =>
        {
            foreach (var item in added) _items[item.Index] = item;
            Initialize();   // re-measure: the content height (item count) may have changed
        });
    }

    public void AddItems(params (string text, Color? fgColor, Color? bgColor)[] items)
    {
        var added = items.Select(t => new ListBoxItem(this, Interlocked.Increment(ref _itemIndex), t.text, t.fgColor, t.bgColor)).ToArray();
        UI.Invoke(() =>
        {
            foreach (var item in added) _items[item.Index] = item;
            Initialize();   // re-measure: the content height (item count) may have changed
        });
    }

    public ListBoxItem AddItem(IRenderable item)
    {
        var listItem = new ListBoxItem(this, Interlocked.Increment(ref _itemIndex), item);
        UI.Invoke(() =>
        {
            _items[listItem.Index] = listItem;
            Initialize();
        });
        return listItem;
    }

    public ListBoxItem AddItem(string text, Color? foreground = null, Color? background = null)
    {
        var item = new ListBoxItem(this, Interlocked.Increment(ref _itemIndex), text, foreground, background);
        UI.Invoke(() =>
        {
            _items[item.Index] = item;
            Initialize();
        });
        return item;
    }

    /// <summary>
    /// Removes an item. The result is reliable only when called on the UI thread; off-thread callers should
    /// not rely on it (the removal is marshaled and applied on the next pump).
    /// </summary>
    public bool RemoveItem(ListBoxItem item)
    {
        var removed = false;
        UI.Invoke(() =>
        {
            if (_items.Remove(item.Index, out var r))
            {
                r.Detach();
                removed = true;
                Initialize();
            }
        });
        return removed;
    }

    public void Clear()
    {
        UI.Invoke(() =>
        {
            foreach (var item in _items.Values) item.Detach();
            _items.Clear();
            Initialize();
        });
    }

    public void Update() => Invalidate();

    // Default the selected-row colours from the theme so a bare ListBox shows a visible selection (re-applied on a
    // runtime theme switch; explicit SelectedForeground/BackgroundColor overrides are left alone).
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(SelectedForegroundColor))) _selectedForegroundColor = UI.StyleTheme.Selection.ForegroundColor;
        if (!IsThemeOverridden(nameof(SelectedBackgroundColor))) _selectedBackgroundColor = UI.StyleTheme.Selection.BackgroundColor;
        if (!IsThemeOverridden(nameof(SelectionStyle))) _selectionStyle = UI.StyleTheme.SelectionStyle;
        _selectionCaret = UI.GlyphTheme.SelectionCaret;
    }

    // Each item is one row; report the item count so a surrounding ControlFrame sizes us to our content and
    // scrolls accurately, instead of filling to the 1000-row clamp.
    protected override int MeasureHeight(int width) => Math.Max(1, _items.Count);

    protected internal override HelpInfo? GetHelpInfo() => new HelpInfo("List", "List", "A scrollable list of items; the selected item is highlighted.")
        .WithKey("↑ / ↓", "Move the selection")
        .WithKey("Home / End", "First / last item")
        .WithKey("PgUp / PgDn", "Move by a page")
        .WithKey("Enter", "Choose the selected item")
        .WithKey("Click", "Select an item");

    protected override void OnInput(InputEvent inputEvent)
    {
        var count = _items.Count;
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.UpArrow when count > 0:
                SelectedIndex = (_selectionIndex - 1 + count) % count;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.DownArrow when count > 0:
                SelectedIndex = (_selectionIndex + 1) % count;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Home when count > 0:
                SelectedIndex = 0;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.End when count > 0:
                SelectedIndex = count - 1;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.PageUp when count > 0:
                SelectedIndex = _selectionIndex - Page();
                inputEvent.Handled = true;
                break;
            case ConsoleKey.PageDown when count > 0:
                SelectedIndex = _selectionIndex + Page();
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Enter when count > 0:
                Commit();
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Escape:
                Cancelled?.Invoke(this, EventArgs.Empty);
                inputEvent.Handled = true;
                break;
        }
    }

    // A page for PageUp/PageDown: the visible viewport when framed, else a sensible default. SelectedIndex clamps.
    private int Page() => Math.Max(1, Frame?.ViewportSize.Height ?? 10);

    // A left click selects the row under the pointer and commits it; a right click selects it and opens the context
    // menu instead. Each item is one row, and the listener position is in content coordinates, so the row is position.Y.
    protected override void OnClick(Position position)
    {
        if (UI.MouseButton == TerminalMouseButton.Right) { OpenContextMenu(position.Y); return; }
        var count = _items.Count;
        var index = position.Y;
        if (index < 0 || index >= count) return;
        SelectedIndex = index;
        Commit();
    }

    // A fast double right-click still just (re)opens the menu rather than committing underneath it.
    protected override void OnDoubleClick(Position position)
    {
        if (UI.MouseButton == TerminalMouseButton.Right) OpenContextMenu(position.Y);
    }

    private void Commit()
    {
        if (SelectedItem is { } item) Committed?.Invoke(this, item);
    }

    // Right-click: select the row, announce it, then float ContextMenu at the pointer (in the ambient UI.Overlay).
    // Item handlers can read SelectedItem. No-op if no menu is set or the click missed every row.
    private void OpenContextMenu(int row)
    {
        if (ContextMenu is null || row < 0 || row >= _items.Count) return;
        SelectedIndex = row;
        if (SelectedItem is { } item) ContextMenuOpening?.Invoke(this, item);
        if (ConsoleGUI.ConsoleManager.MousePosition is { } m) ContextMenu.Show(m.X, m.Y);
        else ContextMenu.Show(0, row);
    }

    private ListBoxItem[] OrderedItems() => _items.Values.OrderBy(i => i.Index).ToArray();

    /// <summary>
    /// Scrolls the containing <see cref="ControlFrame"/> (if any) so the selected item stays within
    /// the viewport. Each item occupies one row, so the selected row's Y position is its index.
    /// </summary>
    private void AutoScroll()
    {
        if (Frame == null) return;

        var y = _selectionIndex;
        var top = Frame.Top;
        var viewportHeight = Frame.ViewportSize.Height;
        if (viewportHeight <= 0) return;

        if (y < top)
        {
            Frame.Top = y;
        }
        else if (y >= top + viewportHeight)
        {
            Frame.Top = y - viewportHeight + 1;
        }
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var items = OrderedItems();

        // Ensure selection index is valid
        if (_selectionIndex >= items.Length) _selectionIndex = Math.Max(0, items.Length - 1);
        if (_selectionIndex < 0 && items.Length > 0) _selectionIndex = 0;

        var renderables = new IRenderable[items.Length];

        // In Caret mode reserve a caret-width gutter on every row (the selected row shows the caret, the rest a
        // blank) so the item text stays put as the selection moves instead of jumping.
        var caret = _selectionStyle == SelectionStyle.Caret;
        var gutter = caret ? new string(' ', _selectionCaret.GetCellWidth()) : "";

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var selected = i == _selectionIndex;
            if (item.Text != null)
            {
                if (selected)
                {
                    // Indicate the selected row per SelectionStyle: a highlight, an underline, or a caret prefix.
                    var style = _selectionStyle.TextStyle(_selectedForegroundColor, _selectedBackgroundColor);
                    renderables[i] = new Markup(_selectionStyle.Prefix(_selectionCaret) + item.Text, style);
                }
                else if (caret)
                {
                    // Blank gutter, keeping the item's own colours, so unselected rows line up with the selected one.
                    renderables[i] = new Markup(gutter + item.Text, new Spectre.Console.Style(item.ForegroundColor, item.BackgroundColor));
                }
                else
                {
                    renderables[i] = item.Content;
                }
            }
            else
            {
                // An IRenderable item can't be folded into markup, so a selected one is highlighted by RESTYLING its
                // rendered segments — the selection style is overlaid on each, keeping the segment's own colours so a
                // colourful label stays colourful under the highlight (the same approach Tree uses for IRenderable
                // nodes). The caret gutter (Caret mode) is reserved on every row so labels stay aligned as the
                // selection moves.
                var overlay = selected ? _selectionStyle.TextStyle(_selectedForegroundColor, _selectedBackgroundColor) : (Spectre.Console.Style?)null;
                renderables[i] = new HighlightedRenderable(item.Content, overlay, caret ? _selectionCaret : null, selected);
            }
        }

        var rows = new Rows(renderables);
        return ((IRenderable)rows).Render(options, maxWidth);
    }
    #endregion

    #region Types
    /// <summary>
    /// Wraps a <see cref="ListBoxItem"/>'s <see cref="IRenderable"/> content so a selected row is highlighted the way
    /// <see cref="Tree"/> highlights a selected <see cref="IRenderable"/> node: the selection style is overlaid on
    /// each rendered segment — keeping the segment's own foreground/background where it set one, so a colourful label
    /// stays colourful under the highlight. In <see cref="SelectionStyle.Caret"/> mode a caret glyph (selected) or a
    /// blank spacer (others) is reserved on the left so labels stay aligned as the selection moves.
    /// </summary>
    /// <param name="inner">The item's content.</param>
    /// <param name="overlay">The selection style to overlay when the row is selected; <see langword="null"/> otherwise.</param>
    /// <param name="caret">The caret glyph to reserve a gutter for in Caret mode; <see langword="null"/> in other modes.</param>
    /// <param name="selected">Whether this row is the selected one.</param>
    private sealed class HighlightedRenderable(IRenderable inner, Spectre.Console.Style? overlay, string? caret, bool selected) : IRenderable
    {
        private int GutterWidth => caret?.GetCellWidth() ?? 0;

        public Measurement Measure(RenderOptions options, int maxWidth)
        {
            var g = GutterWidth;
            var m = inner.Measure(options, Math.Max(0, maxWidth - g));
            return new Measurement(m.Min + g, m.Max + g);
        }

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var g = GutterWidth;
            var lines = Segment.SplitLines(inner.Render(options, Math.Max(0, maxWidth - g)));
            var result = new List<Segment>();
            for (var i = 0; i < lines.Count; i++)
            {
                if (g > 0)
                {
                    // Caret glyph on the selected row's first line (in the selection style), else a blank spacer.
                    result.Add(selected && i == 0
                        ? new Segment(caret!, overlay ?? Spectre.Console.Style.Plain)
                        : new Segment(new string(' ', g)));
                }

                // Overlay the selection style on a selected row's segments (each keeps its own colours where set).
                if (overlay is { } o)
                    foreach (var seg in lines[i]) result.Add(seg.WithStyle(o.Combine(seg.Style)));
                else
                    result.AddRange(lines[i]);

                result.Add(Segment.LineBreak);
            }
            return result;
        }
    }
    #endregion

    #region Fields
    private readonly Dictionary<int, ListBoxItem> _items = new();
    private int _itemIndex = 0;
    private int _selectionIndex = 0;
    private Color? _selectedBackgroundColor;
    private Color? _selectedForegroundColor;
    private SelectionStyle _selectionStyle;
    private string _selectionCaret = "";
    #endregion
}
