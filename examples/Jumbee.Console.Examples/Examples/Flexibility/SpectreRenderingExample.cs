namespace Jumbee.Console.Examples;

using System.Globalization;

using Spectre.Console;   // for the AddColumn/AddRow/Centered/AddItem extension methods

using S = Spectre.Console;

/// <summary>
/// Hosting rich Spectre.Console renderables directly — this control <em>is</em> a <see cref="SpectreControl{T}"/>.
/// The whole widget catalogue is available, styled markup and all, composed here with Spectre's own layout pieces.
/// </summary>
public sealed class SpectreRenderingExample : SpectreControl<S.Rows>, IExample
{
    public SpectreRenderingExample() : base(BuildContent()) { }

    // Rows stacks renderables top to bottom — a Spectre layout, not a Jumbee one. Everything below is a single
    // renderable handed to one control, which is the point: Spectre composes, Jumbee hosts the result.
    private static S.Rows BuildContent() => new S.Rows(
        // FigletText draws banner text from a FIGlet font. No font file needed — Spectre embeds the standard one
        // and FigletText uses it by default; pass a FigletFont to FigletText's constructor to use another.
        // Align centres a renderable inside the width it is given (Left/Center/Right, plus optional vertical).
        S.Align.Center(new S.FigletText("Jumbee").Color(S.Color.SkyBlue1)),

        BuildTable(),

        new S.Rule("[grey62]BreakdownChart[/]") { Justification = S.Justify.Left },
        BuildBreakdown(),

        new S.Rule("[grey62]Calendar, indented with a Padder[/]") { Justification = S.Justify.Left },
        // Padder insets a renderable — here left 4, top 1 — the way CSS padding would.
        new S.Padder(BuildCalendar(), new S.Padding(4, 1, 0, 0)),

        S.Align.Right(new S.Markup("[grey54]…and Align again, pushing this to the right edge[/]")));

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
        table.AddRow("Input while live widgets run", "[green]✓[/]", "[grey]—[/]");
        table.AddRow("Modal dialogs", "[green]✓[/]", "some");
        table.AddRow("Smooth sub-cell scrolling", "[green]✓[/]", "[grey]rare[/]");
        table.AddRow("Legacy non-ANSI fallback", "[green]✓[/]", "varies");
        return table;
    }

    // A single proportional bar plus a legend — good for "where did the frame time go".
    private static S.BreakdownChart BuildBreakdown() =>
        new S.BreakdownChart()
            .FullSize()
            .AddItem("Render", 38, S.Color.SkyBlue1)
            .AddItem("Composite", 22, S.Color.Green)
            .AddItem("Input", 9, S.Color.Yellow)
            .AddItem("Idle", 31, S.Color.Grey);

    private static S.Calendar BuildCalendar()
    {
        // InvariantCulture keeps the day names stable whatever the machine's locale is.
        var calendar = new S.Calendar(2026, 7)
        {
            Culture = CultureInfo.InvariantCulture,
            HighlightStyle = new S.Style(S.Color.SkyBlue1),
        };
        calendar.AddCalendarEvent(2026, 7, 4);
        calendar.AddCalendarEvent(2026, 7, 16);
        return calendar;
    }

    #region IExample
    string IExample.Category => "Flexibility";
    string IExample.Title => "Spectre Rendering";
    string IExample.Description =>
        "Spectre.Console renderables — Figlet banner, table, breakdown chart and calendar, composed with Align, Padder and Rows — hosted in one Jumbee control.";
    #endregion
}
