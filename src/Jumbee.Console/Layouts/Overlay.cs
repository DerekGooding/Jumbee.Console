namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleGUI;
using ConsoleGUI.Space;

using CBackground = ConsoleGUI.Controls.Background;
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

    /// <summary><see langword="true"/> when the current popup is modal (shown over a click-blocking scrim).</summary>
    public bool IsModal => _modal;

    /// <summary>When <see langword="true"/> (default), a non-modal popup closes when it loses focus (e.g. a click outside).</summary>
    public bool CloseOnFocusLost { get; set; } = true;

    /// <summary>Key that closes any open popup, intercepted before the popup sees it. <see langword="null"/> disables it.</summary>
    public ConsoleKey? CloseKey { get; set; } = ConsoleKey.Escape;

    /// <summary>Background colour painted behind a modal popup (blocks and obscures the layer beneath).</summary>
    public Color ModalScrim { get; set; } = new(10, 10, 15);
    #endregion

    #region Methods
    /// <summary>Show <paramref name="popup"/> centered over the bottom layer.</summary>
    public void Show(Control popup) => Show(popup, CenterIn(popup), modal: false);

    /// <summary>Show <paramref name="popup"/> with its top-left anchored at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public void Show(Control popup, int x, int y) => Show(popup, AnchorAt(popup, x, y), modal: false);

    /// <summary>
    /// Show <paramref name="popup"/> centered over a click-blocking scrim. The layer beneath cannot be clicked
    /// and the popup stays open until closed explicitly or via <see cref="CloseKey"/>.
    /// </summary>
    public void ShowModal(Control popup) => Show(popup, ScrimAround(popup), modal: true);

    /// <summary>Close the popup and restore focus to whatever was focused before it was shown.</summary>
    public void Hide() => Close(restoreFocus: true);

    private void Show(Control popup, IControl positioned, bool modal) => UI.Invoke(() =>
    {
        if (_top is not null) Detach(_top);
        _previousFocus = UI.Focused;
        _top = popup;
        _modal = modal;
        control.TopContent = positioned;
        // A modal popup keeps focus (the scrim swallows outside clicks), so only non-modal popups close on focus loss.
        if (!modal && CloseOnFocusLost) popup.OnLostFocus += OnTopLostFocus;
        UI.SetFocus(popup);
    });

    private void Close(bool restoreFocus) => UI.Invoke(() =>
    {
        if (_top is null) return;
        Detach(_top);
        _top = null;
        _modal = false;
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

    // Full-area opaque background with the popup centered on it. The filled cells carry no mouse listener, so
    // clicks over them are swallowed (the layer beneath is never hit) — that is what makes the popup modal.
    private IControl ScrimAround(Control popup) => new CBackground
    {
        Color = ModalScrim,
        Content = CenterIn(popup),
    };

    // Tunnel phase (see Layout.OnInput): close any open popup on CloseKey before the popup itself sees the key.
    protected override bool InterceptInput(UI.InputEventArgs inputEventArgs)
    {
        if (IsShowing && CloseKey is { } key && inputEventArgs.InputEvent?.Key.Key == key)
        {
            Hide();
            return true;
        }
        return false;
    }

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
    private bool _modal;
    private IFocusable? _previousFocus;
    #endregion
}
