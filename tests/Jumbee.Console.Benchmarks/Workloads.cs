namespace Jumbee.Console.Benchmarks;

using Spectre.Console;

/// <summary>
/// Representative Spectre renderables used by the benchmarks. Styled markup and mixed cell widths are included
/// so the segment/string churn resembles a real UI frame rather than a plain-text best case.
/// </summary>
internal static class Workloads
{
    public static Table BuildTable(int rows = 24)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold]Id[/]");
        table.AddColumn("Name");
        table.AddColumn("[green]Status[/]");
        table.AddColumn("Detail");

        for (var i = 0; i < rows; i++)
        {
            var status = (i % 3) switch
            {
                0 => "[green]OK[/]",
                1 => "[yellow]WARN[/]",
                _ => "[red]FAIL[/]",
            };
            table.AddRow(
                $"[grey]{i:D3}[/]",
                $"[yellow]item-{i}[/]",
                status,
                "some [italic]longer[/] descriptive text that is likely to wrap across the column");
        }

        return table;
    }

    public static Tree BuildTree(int branches = 6, int leaves = 5)
    {
        var root = new Tree("[bold underline]Root[/]");
        for (var b = 0; b < branches; b++)
        {
            var node = root.AddNode($"[blue]branch-{b}[/]");
            for (var l = 0; l < leaves; l++)
            {
                node.AddNode($"[grey]leaf-{b}.{l}[/] : [green]value {b * leaves + l}[/]");
            }
        }

        return root;
    }
}
