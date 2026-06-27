namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Rendering;
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

    public override bool HandlesInput => true;
    #endregion

    #region Events
    /// <summary>Raised when the highlighted index changes (navigation or selection).</summary>
    public event EventHandler<int>? SelectionChanged;
    /// <summary>Raised when an item is committed (Enter or click).</summary>
    public event EventHandler<ListBoxItem>? Committed;
    /// <summary>Raised when the list is cancelled (Escape).</summary>
    public event EventHandler? Cancelled;
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
    }

    // Each item is one row; report the item count so a surrounding ControlFrame sizes us to our content and
    // scrolls accurately, instead of filling to the 1000-row clamp.
    protected override int MeasureHeight(int width) => Math.Max(1, _items.Count);

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

    // A click selects the row under the pointer and commits it. Each item is one row, and the listener position
    // is in the list's own content coordinates, so the row index is position.Y.
    protected override void OnClick(Position position)
    {
        var count = _items.Count;
        var index = position.Y;
        if (index < 0 || index >= count) return;
        SelectedIndex = index;
        Commit();
    }

    private void Commit()
    {
        if (SelectedItem is { } item) Committed?.Invoke(this, item);
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

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (i == _selectionIndex && item.Text != null && (_selectedForegroundColor.HasValue || _selectedBackgroundColor.HasValue))
            {
                renderables[i] = new Markup(item.Text, new Spectre.Console.Style(_selectedForegroundColor, _selectedBackgroundColor));
            }
            else
            {
                renderables[i] = item.Content;
            }
        }

        var rows = new Rows(renderables);
        return ((IRenderable)rows).Render(options, maxWidth);
    }
    #endregion

    #region Fields
    private readonly Dictionary<int, ListBoxItem> _items = new();
    private int _itemIndex = 0;
    private int _selectionIndex = 0;
    private Color? _selectedBackgroundColor;
    private Color? _selectedForegroundColor;
    #endregion
}
