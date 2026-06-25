namespace Jumbee.Console;

using System.Collections.Generic;
using System.Linq;

using ConsoleGUI;
using ConsoleGUI.Space;

using CBox = ConsoleGUI.Controls.Box;
using CMargin = ConsoleGUI.Controls.Margin;
using COverlay = ConsoleGUI.Controls.Overlay;

/// <summary>
/// A layered layout: a persistent <see cref="Bottom"/> layer with an optional floating popup composited on top
/// (wraps ConsoleGUI's <see cref="COverlay"/>). Where the popup has no content the bottom shows through, so a
/// small centered/anchored popup floats over the main UI. Use <see cref="Show(Control)"/> / <see cref="Hide"/>
/// for dropdowns, dialogs, tooltips, etc. While shown, keyboard input goes to the focused popup; clicking the
/// popup works as normal, and (by default) clicking outside it closes it.
/// </summary>
public class Overlay : Layout<COverlay>
{
    #region Constructors
    public Overlay(ILayout bottom) : base(new COverlay())
    {
        _bottom = bottom;
        control.BottomContent = bottom.CControl;
    }
    #endregion

    #region Properties
    /// <summary>The persistent base layer.</summary>
    public ILayout Bottom => _bottom;

    /// <summary>The popup currently shown, or <see langword="null"/>.</summary>
    public Control? Top => _top;

    public bool IsShowing => _top is not null;

    /// <summary>When <see langword="true"/> (default), the popup closes when it loses focus (e.g. a click outside).</summary>
    public bool CloseOnFocusLost { get; set; } = true;
    #endregion

    #region Methods
    /// <summary>Show <paramref name="popup"/> centered over the bottom layer.</summary>
    public void Show(Control popup) => Show(popup, CenterIn(popup));

    /// <summary>Show <paramref name="popup"/> with its top-left anchored at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public void Show(Control popup, int x, int y) => Show(popup, AnchorAt(popup, x, y));

    /// <summary>Close the popup and restore focus to whatever was focused before it was shown.</summary>
    public void Hide() => Close(restoreFocus: true);

    private void Show(Control popup, IControl positioned) => UI.Invoke(() =>
    {
        if (_top is not null) Detach(_top);
        _previousFocus = UI.Focused;
        _top = popup;
        control.TopContent = positioned;
        if (CloseOnFocusLost) popup.OnLostFocus += OnTopLostFocus;
        UI.SetFocus(popup);
    });

    private void Close(bool restoreFocus) => UI.Invoke(() =>
    {
        if (_top is null) return;
        Detach(_top);
        _top = null;
        control.TopContent = null;   // empty top -> bottom shows through (DrawingContext treats null as transparent)
        if (restoreFocus && _previousFocus is not null) UI.SetFocus(_previousFocus);
        _previousFocus = null;
    });

    private void Detach(Control popup) => popup.OnLostFocus -= OnTopLostFocus;

    // The popup lost focus (e.g. a click landed on the bottom layer) -> close without stealing the new focus.
    private void OnTopLostFocus()
    {
        if (_top is not null && !_top.IsFocused) Close(restoreFocus: false);
    }

    private static IControl CenterIn(Control popup) => new CBox
    {
        HorizontalContentPlacement = CBox.HorizontalPlacement.Center,
        VerticalContentPlacement = CBox.VerticalPlacement.Center,
        Content = popup.FocusableControl,
    };

    private static IControl AnchorAt(Control popup, int x, int y) => new CMargin
    {
        Offset = new Offset(x, y, 0, 0),
        Content = popup.FocusableControl,
    };

    // Flattens the bottom layer's focusables plus the popup (when shown). Drives Controls, which UI uses to
    // register/route focus: while the popup is shown it is the only focused control, so input goes to it.
    private List<IFocusable> Flatten()
    {
        var list = _bottom.Controls.ToList();
        if (_top is not null) list.Add(_top);
        return list;
    }
    #endregion

    #region Layout overrides
    public override int Rows => Flatten().Count;
    public override int Columns => 1;
    public override IFocusable this[int row, int column] => Flatten()[row];
    #endregion

    #region Fields
    private readonly ILayout _bottom;
    private Control? _top;
    private IFocusable? _previousFocus;
    #endregion
}
