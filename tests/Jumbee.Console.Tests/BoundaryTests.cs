namespace Jumbee.Console.Tests;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

// Boundary pins its single child's size. The key use is capping a would-fill control (e.g. a stack-panel toolbar) to a
// fixed extent so it can be docked without collapsing the fill region.
public class BoundaryTests
{
    [Fact]
    public void Height_PinsAFillingChildToFixedRows_WhenDocked()
    {
        // A HorizontalStackPanel resizes to the full available height on its own; Boundary(height: 1) caps it so, as
        // the top-docked control, it takes exactly one row and the fill sibling gets the rest.
        var b = new Boundary(new HorizontalStackPanel(new TextLabel(TextLabelOrientation.Horizontal, "toolbar")), height: 1);
        var dock = new DockPanel(DockedControlPlacement.Top, b, new VerticalStackPanel(new TextLabel(TextLabelOrientation.Horizontal, "body")));

        ConsoleSnapshot.Render(dock, 40, 12);

        Assert.Equal(1, b.Size.Height);
    }

    [Fact]
    public void Width_PinsAFillingChildToFixedColumns_WhenDocked()
    {
        // A VerticalStackPanel resizes to the full available width; Boundary(width: 6) caps it so, as the left-docked
        // control, it takes exactly six columns.
        var b = new Boundary(new VerticalStackPanel(new TextLabel(TextLabelOrientation.Horizontal, "x")), width: 6);
        var dock = new DockPanel(DockedControlPlacement.Left, b, new VerticalStackPanel(new TextLabel(TextLabelOrientation.Horizontal, "body")));

        ConsoleSnapshot.Render(dock, 40, 12);

        Assert.Equal(6, b.Size.Width);
    }

    [Fact]
    public void Unbounded_LetsTheChildSizeFreely()
    {
        var b = new Boundary(new HorizontalStackPanel(new TextLabel(TextLabelOrientation.Horizontal, "abcd")));

        var text = ConsoleSnapshot.ToText(b, 40, 12);

        Assert.Contains("abcd", text);   // child renders through the boundary
    }
}
