namespace Jumbee.Console.Examples;

using System.Linq;

/// <summary>A scrollable, keyboard-navigable list — it simply <em>is</em> a <see cref="ListBox"/>. Arrows / Home /
/// End / PgUp / PgDn move the selection; it scrolls smoothly when it overflows its pane.</summary>
public sealed class ListBoxExample : ListBox, IExample
{
    public ListBoxExample() : base(Enumerable.Range(1, 40).Select(i => $"Item {i:00}").ToArray()) { }

    public string Category => "Controls";
    public string Title => "List Box";
    public string Description => "A selectable, keyboard-navigable list with smooth scrolling when it overflows.";
}
