namespace Jumbee.Console.Examples;

using System.Linq;

/// <summary>A scrollable, keyboard-navigable list. Arrows / Home / End / PgUp / PgDn move the selection.</summary>
public sealed class ListBoxExample : ExampleBase
{
    public override string Category => "Controls";
    public override string Title => "List Box";
    public override string Description => "A selectable, keyboard-navigable list with smooth scrolling when it overflows.";

    public override IFocusable Build()
    {
        var items = Enumerable.Range(1, 40).Select(i => $"Item {i:00}").ToArray();   // enough to scroll
        return new ListBox(items).WithFrame(title: "Items");
    }
}
