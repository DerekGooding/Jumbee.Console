namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console.Rendering;

/// <summary>
/// One entry in a <see cref="ContextMenu"/>: a label, an optional right-aligned shortcut hint, an action invoked
/// when chosen, and an enabled flag. Use <see cref="Separator"/> for a non-selectable divider line.
/// </summary>
public sealed class MenuItem
{
    public string Text { get; init; } = string.Empty;
    public string? Shortcut { get; init; }
    public Action? Action { get; init; }
    public bool Enabled { get; init; } = true;
    public bool IsSeparator { get; init; }

    public MenuItem() { }
    public MenuItem(string text, Action? action = null) { Text = text; Action = action; }

    /// <summary>A non-selectable divider row.</summary>
    public static readonly MenuItem Separator = new() { IsSeparator = true, Enabled = false };

    internal bool Selectable => !IsSeparator && Enabled;
}

/// <summary>
/// A floating, keyboard-navigable menu of <see cref="MenuItem"/>s, shown anchored in an <see cref="Overlay"/> (it
/// frames itself). Up/Down move the highlight (skipping separators and disabled items), Enter/Space choose, Escape
/// or a click outside dismiss. Choosing an item runs its <see cref="MenuItem.Action"/> and raises
/// <see cref="ItemActivated"/>; <see cref="Closed"/> fires whenever the menu closes (chosen or dismissed). This is
/// the shared primitive behind drop-downs / context menus / a <see cref="MenuBar"/>'s menus.
/// </summary>
public class ContextMenu : RenderableControl
{
    #region Constructors
    public ContextMenu(IEnumerable<MenuItem> items)
    {
        _items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
        _highlighted = NextSelectable(-1, +1);
        Width = ContentWidth();
        Height = _items.Count;
        this.WithRoundedBorder();   // a bordered popup, like the Select dropdown
        OnLostFocus += () => Closed?.Invoke(this, EventArgs.Empty);   // dismissed (Escape / click-outside) or chosen
    }
    #endregion

    #region Events
    /// <summary>Raised when an item is chosen (after its <see cref="MenuItem.Action"/> runs).</summary>
    public event EventHandler<MenuItem>? ItemActivated;

    /// <summary>Raised whenever the menu closes — whether an item was chosen or it was dismissed.</summary>
    public event EventHandler? Closed;
    #endregion

    #region Properties
    public override bool HandlesInput => true;
    protected override bool WantsMouse => true;   // hover highlights an item
    public IReadOnlyList<MenuItem> Items => _items;
    #endregion

    #region Methods
    /// <summary>Frames and floats the menu in <paramref name="overlay"/> with its top-left at (<paramref name="x"/>,
    /// <paramref name="y"/>), then focuses it.</summary>
    public void Show(Overlay overlay, int x, int y)
    {
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        overlay.Show(this, x, y);
    }

    protected override void OnInput(InputEvent inputEvent)
    {
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.UpArrow: Move(-1); break;
            case ConsoleKey.DownArrow: Move(+1); break;
            case ConsoleKey.Home: SetHighlight(NextSelectable(-1, +1)); break;
            case ConsoleKey.End: SetHighlight(NextSelectable(_items.Count, -1)); break;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar: Activate(_highlighted); break;
            default: return;   // let other keys bubble (e.g. Escape is handled by the Overlay)
        }
        inputEvent.Handled = true;
    }

    protected override void OnMouseMove(Position position)
    {
        if (position.Y >= 0 && position.Y < _items.Count && _items[position.Y].Selectable)
            SetHighlight(position.Y);
    }

    protected override void OnClick(Position position)
    {
        if (position.Y >= 0 && position.Y < _items.Count) Activate(position.Y);
    }

    protected internal override HelpInfo? GetHelpInfo() => new HelpInfo("Menu", "Menu", "A pop-up menu.")
        .WithKey("Up / Down", "Move")
        .WithKey("Enter", "Choose")
        .WithKey("Esc", "Close");

    private void Move(int dir) => SetHighlight(NextSelectable(_highlighted, dir));

    private void SetHighlight(int index)
    {
        if (index < 0 || index == _highlighted) return;
        _highlighted = index;
        Invalidate();
    }

    private void Activate(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        var item = _items[index];
        if (!item.Selectable) return;

        _overlay?.Hide();   // close first (restores focus), then run the effect
        item.Action?.Invoke();
        ItemActivated?.Invoke(this, item);
    }

    // The next selectable index from <paramref name="from"/> in <paramref name="dir"/> (±1), or the current
    // highlight if none (so navigation at the edge is a no-op rather than wrapping onto a separator).
    private int NextSelectable(int from, int dir)
    {
        for (var i = from + dir; i >= 0 && i < _items.Count; i += dir)
            if (_items[i].Selectable) return i;
        return _highlighted;
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var width = Math.Min(ActualWidth > 0 ? ActualWidth : maxWidth, maxWidth);
        if (width <= 0) yield break;

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item.IsSeparator)
            {
                yield return new Segment(new string('─', width), UI.StyleTheme.BorderText.SpectreConsoleStyle);
            }
            else
            {
                var style = !item.Enabled ? UI.StyleTheme.TextMuted
                    : i == _highlighted ? UI.StyleTheme.Selection
                    : UI.StyleTheme.Text;
                yield return new Segment(FormatRow(item, width), style.SpectreConsoleStyle);
            }

            if (i < _items.Count - 1) yield return Segment.LineBreak;
        }
    }

    private static string FormatRow(MenuItem item, int width)
    {
        var left = " " + item.Text;
        var right = string.IsNullOrEmpty(item.Shortcut) ? string.Empty : item.Shortcut + " ";
        var gap = width - left.Length - right.Length;
        if (gap < 1)
        {
            // Not enough room for the shortcut: truncate the label and drop the shortcut.
            var text = (" " + item.Text);
            return (text.Length > width ? text[..width] : text.PadRight(width));
        }
        return left + new string(' ', gap) + right;
    }

    private int ContentWidth()
    {
        var w = 0;
        foreach (var item in _items)
        {
            if (item.IsSeparator) continue;
            var len = item.Text.Length + (string.IsNullOrEmpty(item.Shortcut) ? 0 : item.Shortcut!.Length + 2);
            w = Math.Max(w, len);
        }
        return w + 2;   // a leading space + a trailing space
    }
    #endregion

    #region Fields
    private readonly List<MenuItem> _items;
    private int _highlighted;
    private Overlay? _overlay;
    #endregion
}
