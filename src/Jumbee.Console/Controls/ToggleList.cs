namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console.Rendering;

/// <summary>
/// Shared base for the vertical, navigable toggle lists (<see cref="RadioSet"/>, <see cref="SelectionList"/>).
/// Each option is one row rendered as a three-cell indicator followed by the option text. Up/Down move the
/// highlight cursor (auto-scrolling the surrounding <see cref="Control.Frame"/>); Space/Enter activate the
/// highlighted row and a click activates the clicked row. Subclasses define the per-row checked state, the
/// indicator glyph, and what "activate" does (single- vs multi-select).
/// </summary>
public abstract class ToggleList : RenderableControl
{
    #region Constructors
    protected ToggleList(IEnumerable<string> options)
    {
        _options = options.ToList();
        Height = Math.Max(1, _options.Count);
        Width = PreferredWidth();
    }
    #endregion

    #region Properties
    public override bool HandlesInput => true;

    public IReadOnlyList<string> Options => _options;

    /// <summary>The highlighted row (navigation cursor), clamped to the option range.</summary>
    public int CursorIndex
    {
        get => _cursor;
        set
        {
            var clamped = _options.Count == 0 ? 0 : Math.Clamp(value, 0, _options.Count - 1);
            if (clamped == _cursor) return;
            _cursor = clamped;
            AutoScroll();
            Invalidate();
        }
    }

    public Color Foreground { get => _foreground; set => SetAtomicProperty(ref _foreground, value); }
    public Color Accent { get => _accent; set => SetAtomicProperty(ref _accent, value); }
    public Color Muted { get => _muted; set => SetAtomicProperty(ref _muted, value); }
    public Color HighlightForeground { get => _highlightForeground; set => SetAtomicProperty(ref _highlightForeground, value); }
    public Color HighlightBackground { get => _highlightBackground; set => SetAtomicProperty(ref _highlightBackground, value); }
    #endregion

    #region Methods
    /// <summary><see langword="true"/> if the option at <paramref name="index"/> is currently selected/checked.</summary>
    protected abstract bool IsChecked(int index);

    /// <summary>The three-cell indicator glyph for the option at <paramref name="index"/> (e.g. <c>[X]</c>).</summary>
    protected abstract string IndicatorGlyph(int index);

    /// <summary>Acts on the option at <paramref name="index"/> (select it, or toggle its checked state).</summary>
    protected abstract void Activate(int index);

    protected override void OnInput(InputEvent inputEvent)
    {
        var count = _options.Count;
        if (count == 0) return;

        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.UpArrow:
                CursorIndex = (_cursor - 1 + count) % count;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.DownArrow:
                CursorIndex = (_cursor + 1) % count;
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                Activate(_cursor);
                inputEvent.Handled = true;
                break;
        }
    }

    // Each option is one row and the listener position is in content coordinates, so the row index is position.Y.
    protected override void OnClick(Position position)
    {
        var index = position.Y;
        if (index < 0 || index >= _options.Count) return;
        CursorIndex = index;
        Activate(index);
    }

    // A double-click is two presses; treat it as clicking the row twice.
    protected override void OnDoubleClick(Position position) => OnClick(position);

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        for (int i = 0; i < _options.Count; i++)
        {
            var highlighted = i == _cursor;
            Color? background = highlighted ? _highlightBackground : null;
            var labelColor = highlighted ? _highlightForeground : _foreground;
            var glyphColor = IsChecked(i) ? _accent : _muted;

            yield return new Segment(IndicatorGlyph(i), MakeStyle(glyphColor, background));

            var label = " " + _options[i];
            var fill = Math.Max(0, maxWidth - IndicatorWidth);
            label = label.Length > fill ? label[..fill] : label.PadRight(fill);
            yield return new Segment(label, MakeStyle(labelColor, background));

            if (i < _options.Count - 1) yield return Segment.LineBreak;
        }
    }

    /// <summary>The cell width of the indicator (three cells for all current subclasses).</summary>
    protected virtual int IndicatorWidth => 3;

    private static Spectre.Console.Style MakeStyle(Color foreground, Color? background) =>
        new(foreground: foreground, background: background is { } b ? b.ToSpectreColor() : null);

    private void AutoScroll()
    {
        if (Frame == null) return;

        var y = _cursor;
        var top = Frame.Top;
        var viewportHeight = Frame.ViewportSize.Height;
        if (viewportHeight <= 0) return;

        if (y < top) Frame.Top = y;
        else if (y >= top + viewportHeight) Frame.Top = y - viewportHeight + 1;
    }

    private int PreferredWidth()
    {
        var longest = _options.Count == 0 ? 0 : _options.Max(o => o.Length);
        return IndicatorWidth + 1 + longest;   // indicator + space + text
    }
    #endregion

    #region Fields
    private protected readonly List<string> _options;
    private int _cursor;
    private Color _foreground = Color.White;
    private Color _accent = Color.Green1;
    private Color _muted = Color.Grey66;
    private Color _highlightForeground = Color.White;
    private Color _highlightBackground = new(40, 50, 80);
    #endregion
}
