namespace Jumbee.Console.AgentHarnessDemo;

using System;
using System.Collections.Generic;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using Jumbee.Console;

using Spectre.Console.Rendering;

/// <summary>A clickable sidebar row — a tinted glyph and a label that highlights on hover and runs an action when
/// clicked (or Enter/Space). Non-focusable (click-only), like the harness's Home / Artifacts / Customize rows.</summary>
internal sealed class NavItem : RenderableControl
{
    #region Constructors
    public NavItem(string glyph, string label, Color glyphColor, Action? activate = null)
    {
        _glyph = glyph;
        _label = label;
        _glyphColor = glyphColor;
        _activate = activate;
        Focusable = false;   // a nav affordance, not a tab stop
        Height = 1;
    }
    #endregion

    #region Methods
    protected override bool WantsMouse => true;   // non-focusable but still hover/clickable (see Control cell listener)
    public override bool HandlesInput => true;
    protected override int IntrinsicHeight() => 1;

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0) yield break;
        var bg = IsMouseOver ? (Spectre.Console.Color?)Palette.RaisedBg : null;
        var glyphStyle = new Spectre.Console.Style(foreground: _glyphColor, background: bg);
        var labelStyle = new Spectre.Console.Style(foreground: Palette.Text, background: bg);

        var used = 1 + Spectre.Console.StringExtensions.GetCellWidth(_glyph) + 2 + _label.Length;
        yield return new Segment(" ", labelStyle);
        yield return new Segment(_glyph, glyphStyle);
        yield return new Segment("  ", labelStyle);
        yield return new Segment(_label, labelStyle);
        if (maxWidth > used) yield return new Segment(new string(' ', maxWidth - used), labelStyle);   // extend hover bar
    }

    protected override void OnClick(Position position) => _activate?.Invoke();

    protected override void OnInput(InputEvent inputEvent)
    {
        if (inputEvent.Key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            _activate?.Invoke();
            inputEvent.Handled = true;
        }
    }
    #endregion

    #region Fields
    private readonly string _glyph;
    private readonly string _label;
    private readonly Color _glyphColor;
    private readonly Action? _activate;
    #endregion
}
