namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleGUI;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console.Rendering;

/// <summary>
/// A drop-down selector. Closed, it shows the current value with a ▼ marker; clicking it (or Enter/Space while
/// focused) opens its options in the ambient <see cref="UI.Overlay"/>, anchored just below. Choosing an option
/// (click or Enter) commits it; Escape or a click outside cancels.
/// </summary>
public class Select : RenderableControl
{
    #region Constructors
    public Select(params string[] options)
    {
        _options = options.ToList();
        Height = 1;
        Width = PreferredWidth();
    }
    #endregion

    #region Events
    /// <summary>Raised when a different value is committed.</summary>
    public event EventHandler<string>? SelectionChanged;
    #endregion

    #region Properties
    public IReadOnlyList<string> Options => _options;

    public string Placeholder { get; set; } = "Select…";

    public Color Foreground { get; set; } = Color.White;
    public Color Background { get; set; } = new(50, 50, 70);

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = _options.Count == 0 ? -1 : Math.Clamp(value, 0, _options.Count - 1);
            if (clamped == _selectedIndex) return;
            _selectedIndex = clamped;
            Invalidate();
            if (SelectedValue is { } v) SelectionChanged?.Invoke(this, v);
        }
    }

    public string? SelectedValue => _selectedIndex >= 0 && _selectedIndex < _options.Count ? _options[_selectedIndex] : null;

    public override bool HandlesInput => true;
    #endregion

    #region Methods
    /// <summary>Opens the dropdown into the ambient <see cref="UI.Overlay"/> (no-op before <see cref="UI.Start"/>
    /// or with no options).</summary>
    public void Open()
    {
        if (UI.Overlay is not { } host || _options.Count == 0) return;

        var list = new ListBox(_options.ToArray())
        {
            SelectedForegroundColor = Color.White,
            SelectedBackgroundColor = new Color(40, 90, 160),
            Width = PreferredWidth(),
            Height = Math.Min(_options.Count, MaxDropdownRows),
        };
        list.SelectedIndex = Math.Max(0, _selectedIndex);
        list.WithRoundedBorder(Color.Grey);

        list.Committed += (_, item) =>
        {
            var index = _options.IndexOf(item.Text ?? string.Empty);
            if (index >= 0) SelectedIndex = index;
            Close();
        };
        list.Cancelled += (_, _) => Close();

        if (_anchorX >= 0) host.Show(list, _anchorX, _anchorY);
        else host.Show(list);
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var style = new Spectre.Console.Style(Foreground, Background);
        var label = SelectedValue ?? Placeholder;

        var inner = $" {label}";
        if (inner.Length > maxWidth - 2) inner = inner[..Math.Max(0, maxWidth - 2)];
        var text = inner.PadRight(Math.Max(0, maxWidth - 1)) + "▼";   // value left, arrow at the right edge

        yield return new Segment(text, style);
    }

    protected override void OnClick(Position position)
    {
        // Anchor the dropdown just below this control: the click's absolute position minus its position relative
        // to us gives our top-left; drop down one row.
        if (ConsoleManager.MousePosition is { } m)
        {
            _anchorX = m.X - position.X;
            _anchorY = m.Y - position.Y + 1;
        }
        Open();
    }

    protected override void OnInput(InputEvent inputEvent)
    {
        if (inputEvent.Key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            Open();
            inputEvent.Handled = true;
        }
    }

    private void Close()
    {
        UI.Overlay?.Hide();
        UI.SetFocus(this);
    }

    private int PreferredWidth()
    {
        var longest = _options.Count == 0 ? 0 : _options.Max(o => o.Length);
        return Math.Max(longest, Placeholder.Length) + 3;   // leading space + arrow + a little padding
    }
    #endregion

    #region Fields
    private const int MaxDropdownRows = 8;
    private readonly List<string> _options;
    private int _selectedIndex = -1;
    private int _anchorX = -1;
    private int _anchorY = -1;
    #endregion
}
