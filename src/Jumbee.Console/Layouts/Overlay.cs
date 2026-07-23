
using ConsoleGUI;
using ConsoleGUI.Space;
using System;
using CBox = ConsoleGUI.Controls.Box;
using CMargin = ConsoleGUI.Controls.Margin;
using COverlay = ConsoleGUI.Controls.Overlay;

namespace Jumbee.Console;
/// <summary>
/// A layered layout: a persistent <see cref="Bottom"/> layer with an optional floating popup composited on top
/// (wraps ConsoleGUI's <see cref="COverlay"/>).
/// </summary>
/// <remarks>
/// Where the popup has no content the bottom shows through, so a small centered/anchored popup floats over the main
/// UI. Use <see cref="Show(Control)"/> / <see cref="Hide"/> for dropdowns, dialogs, tooltips, etc. While shown,
/// keyboard input goes to the focused popup; clicking the popup works as normal, and (by default) clicking outside it
/// closes it.
/// </remarks>
public class Overlay : Layout<COverlay>
{
    #region Constructors

    /// <summary>Initializes a new <see cref="Overlay"/> with <paramref name="bottom"/> as its persistent base layer.</summary>
    public Overlay(ILayout bottom) : base(new COverlay())
    {
        _bottom = bottom;
        control.BottomContent = bottom.CControl;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The persistent base layer.</summary>
    public ILayout Bottom => _bottom;

    /// <summary>The popup currently shown, or <see langword="null"/>.</summary>
    public Control? Top => _top;

    /// <summary><see langword="true"/> when a popup is currently shown.</summary>
    public bool IsShowing => _top is not null;

    /// <summary><see langword="true"/> when the current popup is modal (shown over a click-blocking scrim).</summary>
    public bool IsModal => _modal;

    /// <summary>When <see langword="true"/> (default), a non-modal popup closes when it loses focus (e.g. a click outside).</summary>
    public bool CloseOnFocusLost { get; set; } = true;

    /// <summary>Key that closes any open popup, intercepted before the popup sees it. <see langword="null"/> disables it.</summary>
    public ConsoleKey? CloseKey { get; set; } = ConsoleKey.Escape;

    /// <summary>The tint a modal scrim blends the layer beneath toward (see <see cref="ModalDim"/>).</summary>
    /// <remarks>Defaults to the theme's <see cref="IStyleTheme.Scrim"/> colour (picked up live on a theme switch);
    /// set it to override per overlay.</remarks>
    public Color ModalScrim
    {
        get => _modalScrim ?? UI.StyleTheme.Scrim.BackgroundColor ?? DefaultScrim;
        set => _modalScrim = value;
    }

    /// <summary>How strongly a modal scrim dims the layer beneath it: 0 = fully see-through, 1 = a solid
    /// <see cref="ModalScrim"/> fill (the classic opaque modal).</summary>
    /// <remarks>Defaults to the theme's <see cref="IStyleTheme.ScrimDim"/> (0.6), so the controls behind show through,
    /// dimmed, while the popup stands out; set it to override per overlay. The scrim blocks clicks regardless of this
    /// value.</remarks>
    public float ModalDim
    {
        get => _modalDim ?? UI.StyleTheme.ScrimDim;
        set => _modalDim = value;
    }

    #endregion Properties

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

    /// <summary>Re-anchors the current (non-passive) popup at (<paramref name="x"/>, <paramref name="y"/>) without
    /// touching focus.</summary>
    /// <remarks>A popup that changes its own size while open (e.g. a <see cref="ContextMenu"/> opening a submenu)
    /// calls this so the overlay re-measures and re-lays-out the popup at its new size.</remarks>
    public void Reanchor(int x, int y) => UI.Invoke(() =>
    {
        if (_top is { } t && !_passive) control.TopContent = AnchorAt(t, x, y);
    });

    /// <summary>
    /// Shows <paramref name="popup"/> anchored at (<paramref name="x"/>, <paramref name="y"/>) as a <em>passive</em>
    /// layer: it is drawn over (and is mouse-clickable), but does NOT take focus and does NOT capture keyboard
    /// routing/navigation — the layer beneath keeps focus and keeps receiving keys.
    /// </summary>
    /// <remarks>
    /// The caller owns dismissal (e.g. an <see cref="Autocomplete"/> popup that floats under a still-focused text
    /// field). Use <see cref="Hide"/> to close it.
    /// </remarks>
    public void ShowPassive(Control popup, int x, int y) => UI.Invoke(() =>
    {
        if (_top is not null) Detach(_top);
        _top = popup;
        _modal = false;
        _passive = true;
        control.TopContent = AnchorAt(popup, x, y);
        // No focus change, no OnLostFocus wiring: a non-capturing visual layer; the caller manages dismissal.
    });

    private void Show(Control popup, IControl positioned, bool modal) => UI.Invoke(() =>
    {
        _passive = false;
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
        _passive = false;
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

    // Anchor the popup at (x, y). The inner CBox (Left/Top, not Stretch) lets the popup keep its own preferred
    // size instead of being stretched to the container's bottom-right edge by the margin — so dropdowns/menus are
    // snug to their content rather than full-width.
    private static IControl AnchorAt(Control popup, int x, int y) => new CMargin
    {
        Offset = new Offset(x, y, 0, 0),
        Content = new CBox
        {
            HorizontalContentPlacement = CBox.HorizontalPlacement.Left,
            VerticalContentPlacement = CBox.VerticalPlacement.Top,
            Content = popup.FocusableControl,
        },
    };

    // Full-area scrim with the popup centered on it. The scrim shows the layer beneath dimmed (see DimScrim) and its
    // cells carry no mouse listener, so clicks over them are swallowed (the layer beneath is never hit) — that is
    // what makes the popup modal. ModalDim controls how see-through it is.
    private IControl ScrimAround(Control popup) =>
        new DimScrim(_bottom.CControl, CenterIn(popup), ModalScrim, ModalDim);

    // Tunnel phase (see Layout.OnInput): close any open popup on CloseKey before the popup itself sees the key.
    /// <summary>Closes any open popup when <see cref="CloseKey"/> is pressed, before the popup sees the key.</summary>
    protected override bool InterceptInput(UI.InputEventArgs inputEventArgs)
    {
        if (IsShowing && !_passive && CloseKey is { } key && inputEventArgs.InputEvent?.Key.Key == key)
        {
            Hide();
            return true;
        }
        return false;
    }

    #endregion Methods

    #region Layout overrides

    // With no popup the overlay is transparent to navigation and input routing — it delegates its 2-D cell grid to
    // the bottom layer, so spatial nav (Ctrl+arrows) and routing see the bottom's real structure. While a popup is
    // shown it presents ONLY the popup (a single cell), so focus/input/nav are exclusive to it until it closes.
    // A passive top is drawn but never captures routing/nav, so the bottom layer stays addressable beneath it.
    /// <summary>Number of rows in the layout grid (1 while a capturing popup is shown, otherwise the bottom layer's rows).</summary>
    public override int Rows => _top is not null && !_passive ? 1 : _bottom.Rows;

    /// <summary>Number of columns in the layout grid (1 while a capturing popup is shown, otherwise the bottom layer's columns).</summary>
    public override int Columns => _top is not null && !_passive ? 1 : _bottom.Columns;

    /// <summary>Gets the control at the given <paramref name="row"/> and <paramref name="column"/> (the popup while one is shown, otherwise the bottom layer's cell).</summary>
    public override IFocusable this[int row, int column] => _top is not null && !_passive ? _top : _bottom[row, column];

    #endregion Layout overrides

    #region Fields

    private static readonly Color DefaultScrim = new(10, 10, 15);   // fallback when the theme leaves Scrim without a bg
    private readonly ILayout _bottom;
    private Control? _top;
    private bool _modal;
    private bool _passive;   // a non-capturing visual layer (e.g. an autocomplete popup); see ShowPassive
    private IFocusable? _previousFocus;
    private Color? _modalScrim;   // null = use the theme's Scrim colour
    private float? _modalDim;     // null = use the theme's ScrimDim

    #endregion Fields
}