namespace Jumbee.Console.Tests;

using System;
using System.Collections.Generic;
using System.Linq;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

/// <summary>Tests for the 0.1.4 Tree/item API additions: <see cref="Tree.SelectionChanged"/>,
/// <see cref="TreeGuide.None"/>, and the <c>Tag</c> data slots on <see cref="Tree.TreeNode"/> / ListBoxItem.</summary>
public class TreeApiTests
{
    private static ConsoleKeyInfo K(ConsoleKey key) => new('\0', key, false, false, false);

    [Fact]
    public void Tree_SelectionChanged_FiresOncePerMove_WithTheNewNode()
    {
        var tree = new Tree("root");
        for (var i = 0; i < 4; i++) tree.AddNode($"node{i}");
        ConsoleSnapshot.Render(tree, 20, 10);
        UI.SendInput(tree, K(ConsoleKey.Home));   // establish a starting selection before subscribing

        var seen = new List<string?>();
        tree.SelectionChanged += (_, n) => seen.Add(n.Text);

        UI.SendInput(tree, K(ConsoleKey.DownArrow));
        UI.SendInput(tree, K(ConsoleKey.DownArrow));

        Assert.Equal(2, seen.Count);                       // one event per actual move, not per key or per node cleared
        Assert.Equal(tree.SelectedNode?.Text, seen[^1]);   // carries the newly-selected node
    }

    [Fact]
    public void Tree_GuideNone_DrawsNoConnectorLines_ButStillShowsChildren()
    {
        var lined = new Tree("root", TreeGuide.Line);
        lined.AddNode("parent").AddChild("child");
        var linedText = ConsoleSnapshot.ToText(lined, 24, 8);

        var none = new Tree("root", TreeGuide.None);
        none.AddNode("parent").AddChild("child");
        var noneText = ConsoleSnapshot.ToText(none, 24, 8);

        Assert.Contains("child", linedText);
        Assert.Contains("child", noneText);   // hierarchy still renders, just by indentation

        var connectors = new[] { '│', '├', '└', '─', '|', '+' };   // │ ├ └ ─ and ASCII fallbacks
        Assert.True(linedText.IndexOfAny(connectors) >= 0, "the Line guide should draw connector glyphs");
        Assert.True(noneText.IndexOfAny(connectors) < 0, "TreeGuide.None should draw no connector glyphs");
    }

    [Fact]
    public void TreeNode_Tag_RoundTrips_AndUpdateTreeIsPublic()
    {
        var tree = new Tree("root");
        var payload = new object();
        var node = tree.AddNode("feed");

        node.Tag = payload;
        Assert.Same(payload, node.Tag);   // maps a node back to a domain object without a side dictionary

        node.UpdateTree();                // public escape hatch — compiles and runs (was protected -> CS0122)
    }

    [Fact]
    public void ListBoxItem_Tag_RoundTrips()
    {
        var list = new ListBox("a", "b");
        var item = list.Items.First();
        var payload = new object();

        item.Tag = payload;
        Assert.Same(payload, item.Tag);
    }
}
