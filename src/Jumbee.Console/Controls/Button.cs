namespace Jumbee.Console;

using System;
using System.Collections.Generic;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// A focusable, clickable button that renders a text label. Activates on a mouse click or on Enter/Space while
/// focused, raising <see cref="Activated"/>. The background reflects hover and press state.
/// </summary>
public class Button : RenderableControl
{
    #region Constructors
    public Button(string text)
    {
        _text = text;
        _normalStyle = UI.StyleTheme.Primary;
        _hoverStyle = UI.StyleTheme.PrimaryHover;
        _pressStyle = UI.StyleTheme.PrimaryActive;
        Width = LabelWidth(text);
        Height = 1;
    }
    #endregion

    #region Events
    /// <summary>Raised when the button is activated by a mouse click or by Enter/Space while focused.</summary>
    public event EventHandler? Activated;
    #endregion

    #region Properties
    public override bool HandlesInput => true;

    public string Text
    {
        get => _text;
        set => SetAtomicProperty(ref _text, value, updatesLayout: true, watch: (_, _) => Width = LabelWidth(_text));
    }

    /// <summary>Style at rest. Defaults to <see cref="IStyleTheme.Primary"/>.</summary>
    public Style NormalStyle { get => _normalStyle; set => SetAtomicProperty(ref _normalStyle, value); }

    /// <summary>Style while hovered. Defaults to <see cref="IStyleTheme.PrimaryHover"/>.</summary>
    public Style HoverStyle { get => _hoverStyle; set => SetAtomicProperty(ref _hoverStyle, value); }

    /// <summary>Style while pressed. Defaults to <see cref="IStyleTheme.PrimaryActive"/>.</summary>
    public Style PressStyle { get => _pressStyle; set => SetAtomicProperty(ref _pressStyle, value); }
    #endregion

    #region Methods
    /// <summary>Programmatically activate the button (the same path as a click).</summary>
    public void Activate() => Activated?.Invoke(this, EventArgs.Empty);

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var style = IsMousePressed ? _pressStyle : IsMouseOver ? _hoverStyle : _normalStyle;

        var label = $" {_text} ";
        if (label.Length < maxWidth) label = label.PadRight(maxWidth);
        else if (label.Length > maxWidth) label = label[..Math.Max(0, maxWidth)];

        yield return new Segment(label, style);
    }

    // Mouse click (press+release on this control) is synthesized by the base Control.
    protected override void OnClick(Position position) => Activate();

    protected override void OnInput(InputEvent inputEvent)
    {
        if (inputEvent.Key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            Activate();
            inputEvent.Handled = true;
        }
    }

    private static int LabelWidth(string text) => text.Length + 2;   // a space of padding either side
    #endregion

    #region Fields
    private string _text;
    private Style _normalStyle;
    private Style _hoverStyle;
    private Style _pressStyle;
    #endregion
}
