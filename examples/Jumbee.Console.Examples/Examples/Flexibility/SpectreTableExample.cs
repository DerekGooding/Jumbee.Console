namespace Jumbee.Console.Examples;

using Spectre.Console;   // for the Table AddColumn/AddRow/Centered extension methods
using S = Spectre.Console;

/// <summary>Hosting a rich Spectre.Console renderable (a Table) directly inside a Jumbee control — the whole Spectre
/// widget catalogue is available, styled markup and all.</summary>
public sealed class SpectreTableExample : ExampleBase
{
    public override string Category => "Flexibility";
    public override string Title => "Spectre Rendering";
    public override string Description =>
        "Any Spectre.Console renderable (tables, rules, markup, bar charts…) drops straight into a Jumbee control via SpectreControl.";

    public override IFocusable Build()
    {
        var table = new S.Table
        {
            Border = S.TableBorder.Rounded,
            Expand = true,
            Title = new S.TableTitle("[bold]TUI toolkits at a glance[/]"),
        };
        table.AddColumn("Capability");
        table.AddColumn(new S.TableColumn("[#8fd0ff]Jumbee[/]").Centered());
        table.AddColumn(new S.TableColumn("Typical others").Centered());
        table.AddRow("Retained-mode core", "[green]✓[/]", "varies");
        table.AddRow("Spectre.Console rendering", "[green]✓[/]", "[grey]—[/]");
        table.AddRow("Modal dialogs", "[green]✓[/]", "some");
        table.AddRow("Smooth sub-cell scrolling", "[green]✓[/]", "[grey]rare[/]");
        table.AddRow("Legacy non-ANSI fallback", "[green]✓[/]", "varies");
        table.AddRow("Live perf HUD", "[green]✓[/]", "[grey]—[/]");
        return new SpectreControl<S.Table>(table);
    }
}
