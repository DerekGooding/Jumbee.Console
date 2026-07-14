namespace Jumbee.Console.Examples;

using Spectre.Console;

/// <summary>
/// The landing page: the pitch and how Jumbee differs from other TUI toolkits, hosted in a
/// <see cref="SpectreControl{T}"/> Spectre panel.
/// </summary>
public sealed class WelcomeExample : SpectreControl<Panel>, IExample
{
    public WelcomeExample() : base(BuildPanel()) { }

    private static Panel BuildPanel()
    {
        var body = new Markup(
            "[bold #8fd0ff]Jumbee.Console[/] blends a [italic]retained-mode[/] core with modern ANSI input/output and " +
            "[italic]Spectre.Console[/] rendering — so controls feel like their desktop-GUI equivalents.\n\n" +
            "[bold]What we focus on[/]\n" +
            "  [#7CFC00]•[/] [bold]Flexibility[/] — modal dialogs, smooth scrolling, resizable layouts and rich\n" +
            "     Spectre widgets, that still degrade gracefully to legacy non-ANSI terminals.\n" +
            "  [#7CFC00]•[/] [bold]Ease of use[/] — a small API (Layout · ControlFrame · CompositeControl). Terminal\n" +
            "     apps should be simple to write.\n" +
            "  [#7CFC00]•[/] [bold]Performance[/] — responsive and lightweight (watch the footer, or press [bold]Ctrl+G[/]).\n\n" +
            "[grey]This browser is itself built from Jumbee's Tree, resizable SplitPanels and the MultiTabCodeEditor.\n" +
            "Pick an example on the left — its source is on the right. The source is the documentation.[/]");

        return new Panel(body)
        {
            Header = new PanelHeader(" ◈ Welcome "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(foreground: Color.SkyBlue1),
            Padding = new Padding(2, 1, 2, 1),
            Expand = true,
        };
    }

    #region IExample
    string IExample.Category => "Welcome";
    string IExample.Title => "Why Jumbee.Console";
    string IExample.Description =>
        "A retained-mode TUI core that speaks modern ANSI and renders with Spectre.Console — GUI-grade controls that stay simple and fast.";
    #endregion
}
