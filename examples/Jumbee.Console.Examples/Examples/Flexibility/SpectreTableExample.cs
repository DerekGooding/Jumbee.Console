namespace Jumbee.Console.Examples;

using Spectre.Console;   // for the Table AddColumn/AddRow/Centered extension methods
using S = Spectre.Console;

/// <summary>
/// Hosting a rich Spectre.Console renderable (a Table) directly — it <em>is</em> a <see cref="SpectreControl{T}"/>.
/// The whole Spectre widget catalogue is available, styled markup and all.
/// </summary>
public sealed class SpectreTableExample : SpectreControl<S.Table>, IExample
{
    public SpectreTableExample() : base(BuildTable()) { }

    private static S.Table BuildTable()
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
        return table;
    }

    #region IExample
    string IExample.Category => "Flexibility";
    string IExample.Title => "Spectre Rendering";
    string IExample.Description =>
        "Any Spectre.Console renderable (tables, rules, markup, bar charts…) drops straight into a Jumbee control.";
    #endregion
}
