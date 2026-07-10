namespace Jumbee.Console.Examples;

using System.Linq;

/// <summary>
/// A scrollable, keyboard-navigable list — it simply <em>is</em> a <see cref="ListBox"/>.
/// Arrows / Home / End / PgUp / PgDn move the selection; it scrolls smoothly when it overflows.
/// </summary>
public sealed class ListBoxExample : ListBox, IExample
{
    public ListBoxExample() : base(Enumerable.Range(1, 40).Select(i => $"Item {i:00}").ToArray()) { }

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "List Box";
    string IExample.Description => "A selectable, keyboard-navigable list with smooth scrolling when it overflows.";
    #endregion
}
