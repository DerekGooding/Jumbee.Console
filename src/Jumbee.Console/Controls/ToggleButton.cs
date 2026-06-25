namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console.Rendering;

/// <summary>
/// Shared base for the single-state toggle widgets (<see cref="Checkbox"/>, <see cref="RadioButton"/>,
/// <see cref="Switch"/>). Renders a state indicator followed by an optional text label, toggles on a mouse
/// click or Enter/Space while focused, and raises <see cref="Changed"/> when the state flips. Subclasses supply
/// the indicator glyph (and its width) for the current <see cref="IsChecked"/> state.
/// </summary>
public abstract class ToggleButton : RenderableControl
{
    #region Constructors
    protected ToggleButton(string text)
    {
        _text = text;
        Height = 1;
        Width = PreferredWidth();
    }
    #endregion

    #region Events
    /// <summary>Raised with the new state whenever <see cref="IsChecked"/> changes.</summary>
    public event EventHandler<bool>? Changed;
    #endregion

    #region Properties
    public override bool HandlesInput => true;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetAtomicProperty(ref _isChecked, value, watch: (_, v) => Changed?.Invoke(this, v));
    }

    public string Text
    {
        get => _text;
        set => SetAtomicProperty(ref _text, value, updatesLayout: true, watch: (_, _) => Width = PreferredWidth());
    }

    /// <summary>Label colour.</summary>
    public Color Foreground { get => _foreground; set => SetAtomicProperty(ref _foreground, value); }

    /// <summary>Indicator colour when checked.</summary>
    public Color Accent { get => _accent; set => SetAtomicProperty(ref _accent, value); }

    /// <summary>Indicator colour when unchecked.</summary>
    public Color Muted { get => _muted; set => SetAtomicProperty(ref _muted, value); }

    /// <summary>Background painted across the whole row while the pointer is over the control.</summary>
    public Color HoverBackground { get => _hoverBackground; set => SetAtomicProperty(ref _hoverBackground, value); }
    #endregion

    #region Methods
    /// <summary>Flips the state (the same path as a click). Overridden by <see cref="RadioButton"/> to latch on.</summary>
    public virtual void Toggle() => IsChecked = !IsChecked;

    /// <summary>The cell width the indicator occupies (e.g. 3 for <c>[X]</c>).</summary>
    protected abstract int IndicatorWidth { get; }

    /// <summary>Renders the state indicator for the current <see cref="IsChecked"/> state at the row background.</summary>
    protected abstract Segment RenderIndicator(Color? background);

    /// <summary>Builds a Spectre style from a Jumbee foreground and optional background.</summary>
    protected static Spectre.Console.Style Style(Color foreground, Color? background) =>
        new(foreground: foreground, background: background is { } b ? b.ToSpectreColor() : null);

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        Color? background = IsMouseOver ? _hoverBackground : null;

        yield return RenderIndicator(background);

        var label = _text.Length > 0 ? " " + _text : string.Empty;
        var fill = Math.Max(0, maxWidth - IndicatorWidth);
        label = label.Length > fill ? label[..fill] : label.PadRight(fill);
        yield return new Segment(label, Style(_foreground, background));
    }

    protected override void OnClick(Position position) => Toggle();

    // A double-click is two presses; for a toggle that means two state changes (the same as clicking twice).
    protected override void OnDoubleClick(Position position) => Toggle();

    protected override void OnInput(InputEvent inputEvent)
    {
        if (inputEvent.Key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            Toggle();
            inputEvent.Handled = true;
        }
    }

    private int PreferredWidth() => IndicatorWidth + (_text.Length > 0 ? _text.Length + 1 : 0);
    #endregion

    #region Fields
    private bool _isChecked;
    private string _text;
    private Color _foreground = Color.White;
    private Color _accent = Color.Green1;
    private Color _muted = Color.Grey66;
    private Color _hoverBackground = new(45, 45, 60);
    #endregion
}
