namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Text;

using Spectre.Console;

/// <summary>
/// The global help dialog: a fixed-size modal composite showing one tab per <see cref="HelpInfo"/> (built by
/// <see cref="UI.ShowHelp"/> from every control's <see cref="Control.GetHelpInfo"/>), with a Close button. Each
/// tab's panel renders the entry's markup <see cref="HelpInfo.Text"/> in a scrolling frame, with its key bindings
/// listed below (unless <see cref="HelpInfo.KeysInline"/>). Shown via <c>Overlay.ShowModal</c>; Esc closes it.
/// </summary>
public sealed class HelpControl : CompositeControl
{
    #region Constructors
    public HelpControl(IReadOnlyList<HelpInfo> infos, Action onClose, int initialTab = 0)
    {
        var tabs = new (string, IFocusable)[infos.Count];
        for (var i = 0; i < infos.Count; i++) tabs[i] = (infos[i].Title, BuildPanel(infos[i]));
        _tabs = new TabPanel(TabBarDock.Top, tabs);

        _close = new Button("Close");
        _close.Activated += (_, _) => onClose();

        // Fixed-size dialog (Width/Height win in CalculateSize), so the centered modal is a tidy box. The tab panel
        // fills the area above a one-row Close button.
        Width = DialogWidth;
        Height = DialogHeight;
        SetContent(new Grid([DialogHeight - 1, 1], [DialogWidth], [[_tabs], [_close]]));
        if (initialTab > 0) _tabs.SelectedIndex = initialTab;
    }
    #endregion

    #region Properties
    // Focus the active tab's header so the Left/Right arrows switch tabs straight away (and it matches the shown
    // tab); Esc closes, Ctrl-nav reaches the Close button.
    protected override Control? FocusChild =>
        _tabs.TabCount > 0 ? _tabs.Headers[Math.Max(0, _tabs.SelectedIndex)] : _close;
    #endregion

    #region Methods
    /// <summary>Selects the tab at <paramref name="index"/> (clamped).</summary>
    public void SelectTab(int index) => _tabs.SelectedIndex = index;

    // A tab's content: the entry's markup, with a "Keys" section appended unless the author inlined the keys.
    private static Control BuildPanel(HelpInfo info)
    {
        var sb = new StringBuilder(info.Text);
        if (!info.KeysInline && info.Keys.Count > 0)
        {
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append("[grey]Keys[/]");
            foreach (var k in info.Keys)
                sb.Append($"\n  [bold]{Markup.Escape(k.Keys)}[/]  {Markup.Escape(k.Description)}");
        }
        return new SpectreControl<Markup>(new Markup(sb.ToString())).WithSquareBorder();
    }
    #endregion

    #region Fields
    private const int DialogWidth = 72;
    private const int DialogHeight = 22;
    private readonly TabPanel _tabs;
    private readonly Button _close;
    #endregion
}
