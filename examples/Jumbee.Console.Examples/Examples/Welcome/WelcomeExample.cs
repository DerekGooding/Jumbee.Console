namespace Jumbee.Console.Examples;

using S = Spectre.Console;

/// <summary>The landing page: the pitch and how Jumbee differs from other TUI toolkits.</summary>
public sealed class WelcomeExample : ExampleBase
{
    public override string Category => "Welcome";
    public override string Title => "Why Jumbee.Console";
    public override string Description =>
        "A retained-mode TUI core that speaks modern ANSI and renders with Spectre.Console — GUI-grade controls that stay simple and fast.";

    public override IFocusable Build()
    {
        var body = new S.Markup(
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

        var panel = new S.Panel(body)
        {
            Header = new S.PanelHeader(" ◈ Welcome "),
            Border = S.BoxBorder.Rounded,
            BorderStyle = new S.Style(foreground: S.Color.SkyBlue1),
            Padding = new S.Padding(2, 1, 2, 1),
            Expand = true,
        };
        return new SpectreControl<S.Panel>(panel);
    }
}
