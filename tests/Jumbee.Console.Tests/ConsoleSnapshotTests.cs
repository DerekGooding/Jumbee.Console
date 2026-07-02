namespace Jumbee.Console.Tests;

using System;
using System.Threading;

using ConsoleGUI;
using ConsoleGUI.Input;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class ConsoleSnapshotTests
{
    #region Helpers
    private static void Press(Control control, ConsoleKey key) => UI.SendInput(control, key);
    #endregion

    [Fact]
    public void ListBox_TextSnapshot_ContainsItems()
    {
        var list = new ListBox();
        list.AddItem("Alpha");
        list.AddItem("Beta");
        list.AddItem("Gamma");
        list.WithRoundedBorder(Color.Green).WithTitle("Items");

        var text = ConsoleSnapshot.ToText(list, 24, 10);

        Assert.Contains("Items", text);   // title in the top border
        Assert.Contains("Alpha", text);
        Assert.Contains("Beta", text);
        Assert.Contains("Gamma", text);
    }

    [Fact]
    public void ControlFrame_InlineTitle_RendersTitleInTopBorderRow()
    {
        var label = new TextLabel(TextLabelOrientation.Horizontal, "body", Color.White) { Width = 20, Height = 1 };
        label.WithRoundedBorder(Color.Cyan1)
             .WithTitle("Hi", new TitleStyle(TitlePos.TopCenter, TitleBorderStyle.Inline));

        var text = ConsoleSnapshot.ToText(label, 24, 6);
        var firstLine = text.Split('\n')[0];

        // Inline title is drawn within the single top border row.
        Assert.Contains("Hi", firstLine);
    }

    [Fact]
    public void Tree_TextSnapshot_ContainsRootAndNodes()
    {
        var tree = new Tree("Root");
        var folder = tree.AddNode("Folder");
        folder.AddChildren("Leaf1", "Leaf2");
        tree.WithRoundedBorder(Color.Purple);

        var text = ConsoleSnapshot.ToText(tree, 24, 10);

        Assert.Contains("Root", text);
        Assert.Contains("Folder", text);
        Assert.Contains("Leaf1", text);
    }

    [Fact]
    public void Tree_ShowsDisclosureGlyphs_ForNodesWithChildren()
    {
        var tree = new Tree("Root");
        var folder = tree.AddNode("Folder");
        folder.AddChildren("Leaf1", "Leaf2");

        var text = ConsoleSnapshot.ToText(tree, 24, 10);

        // Root and Folder have children -> expanded glyph; the glyph isn't shown on leaves.
        Assert.Contains("▼ Root", text);
        Assert.Contains("▼ Folder", text);
        Assert.Contains("Leaf1", text);
    }

    [Fact]
    public void Tree_CollapseAndExpand_HidesAndShowsChildren()
    {
        var tree = new Tree("Root");
        var folder = tree.AddNode("Folder");
        folder.AddChildren("Leaf1", "Leaf2");

        // Select the Folder then collapse it with Left: its children disappear and the glyph flips to collapsed.
        // (The first Down selects the root when nothing is selected yet, so a second Down reaches the Folder.)
        Press(tree, ConsoleKey.DownArrow);   // select root
        Press(tree, ConsoleKey.DownArrow);   // select Folder
        Press(tree, ConsoleKey.LeftArrow);   // collapse Folder
        var collapsed = ConsoleSnapshot.ToText(tree, 24, 10);
        Assert.Contains("► Folder", collapsed);
        Assert.DoesNotContain("Leaf1", collapsed);

        // Re-expand with Right: children come back and the glyph flips back.
        Press(tree, ConsoleKey.RightArrow);
        var expanded = ConsoleSnapshot.ToText(tree, 24, 10);
        Assert.Contains("▼ Folder", expanded);
        Assert.Contains("Leaf1", expanded);
    }

    // The cells whose glyph is the default leaf bullet "•", with their background colour (null = no highlight).
    private static System.Collections.Generic.List<ConsoleGUI.Data.Color?> LeafBulletBackgrounds(ConsoleBuffer buf)
    {
        var found = new System.Collections.Generic.List<ConsoleGUI.Data.Color?>();
        for (var y = 0; y < buf.Size.Height; y++)
            for (var x = 0; x < buf.Size.Width; x++)
                if (buf[x, y].Character.Content == '•') found.Add(buf[x, y].Character.Background);
        return found;
    }

    [Fact]
    public void Tree_TextLeaf_ShowsLeafGlyph_AndFoldsItIntoSelectionHighlight()
    {
        var tree = new Tree("Root");
        var folder = tree.AddNode("Folder");
        folder.AddChild("Alpha");
        folder.AddChild("Beta");

        // Text leaves render the themed leaf glyph; selecting one highlights the glyph together with its text.
        var text = ConsoleSnapshot.ToText(tree, 24, 10);
        Assert.Contains("• Alpha", text);
        Assert.Contains("• Beta", text);

        // Select Alpha (Down x3: root, Folder, Alpha) and confirm exactly one bullet carries the selection
        // background — i.e. the glyph is folded into the highlight, not left as a separate plain marker.
        Press(tree, ConsoleKey.DownArrow);
        Press(tree, ConsoleKey.DownArrow);
        Press(tree, ConsoleKey.DownArrow);
        var buf = ConsoleSnapshot.Render(tree, 24, 10);
        var backgrounds = LeafBulletBackgrounds(buf);
        Assert.Equal(2, backgrounds.Count);                          // two leaf bullets drawn
        Assert.Single(backgrounds, bg => bg is not null);            // only the selected leaf's bullet is highlighted
    }

    [Fact]
    public void Tree_IRenderableLeaf_IsHighlighted_WhenSelected_PreservingItsColours()
    {
        var tree = new Tree("Root");
        var folder = tree.AddNode("Folder");
        folder.AddChild(new Spectre.Console.Markup("[green]GET[/] x"));   // IRenderable leaf, no Text

        // The leaf glyph is drawn before the IRenderable label...
        Assert.Contains("•", ConsoleSnapshot.ToText(tree, 24, 10));

        // ...and selecting it now highlights: the selection style is overlaid on its rendered segments. The bullet
        // carries the selection background, and the "GET" label keeps its own green foreground under that background.
        Press(tree, ConsoleKey.DownArrow);
        Press(tree, ConsoleKey.DownArrow);
        Press(tree, ConsoleKey.DownArrow);
        var buf = ConsoleSnapshot.Render(tree, 24, 10);

        Assert.Single(LeafBulletBackgrounds(buf), bg => bg is not null);   // the selected leaf's bullet is highlighted

        var g = FirstCellWithContent(buf, 'G');   // the 'G' of the green "GET"
        Assert.NotNull(g);
        Assert.NotNull(g!.Value.Character.Background);                                   // has the selection background
        Assert.Equal((40, 50, 80), ToRgb(g.Value.Character.Background!.Value));          // default IStyleTheme.Selection bg
        Assert.NotNull(g.Value.Character.Foreground);
        Assert.True(g.Value.Character.Foreground!.Value.Green > g.Value.Character.Foreground.Value.Red,
            "the label keeps its green foreground (not replaced by the selection foreground)");
    }

    private static ConsoleGUI.Data.Cell? FirstCellWithContent(ConsoleBuffer buf, char c)
    {
        for (var y = 0; y < buf.Size.Height; y++)
            for (var x = 0; x < buf.Size.Width; x++)
                if (buf[x, y].Character.Content == c) return buf[x, y];
        return null;
    }

    private static (int, int, int) ToRgb(ConsoleGUI.Data.Color c) => (c.Red, c.Green, c.Blue);

    // A click at a content-space position routed through the tree's mouse listener.
    private static void ClickTree(Tree tree, int x, int y)
    {
        var ml = (ConsoleGUI.Input.IMouseListener)tree;
        var pos = new ConsoleGUI.Space.Position(x, y);
        ml.OnMouseDown(pos);
        ml.OnMouseUp(pos);
    }

    [Fact]
    public void Tree_Click_SelectsNodeUnderPointer()
    {
        var tree = new Tree("Root");          // row 0
        var folder = tree.AddNode("Folder");  // row 1
        var alpha = folder.AddChild("Alpha"); // row 2
        folder.AddChild("Beta");              // row 3
        ConsoleSnapshot.Render(tree, 24, 10); // establish layout

        ClickTree(tree, 8, 2);                // on Alpha's label

        Assert.True(alpha.Selected);
        Assert.True(folder.Expanded);         // a label click doesn't toggle
    }

    [Fact]
    public void Tree_Click_OnDisclosureGlyph_TogglesNode()
    {
        var tree = new Tree("Root");
        var folder = tree.AddNode("Folder");  // row 1, depth 1 -> glyph at x = 4
        folder.AddChild("Alpha");
        ConsoleSnapshot.Render(tree, 24, 10);

        ClickTree(tree, 4, 1);                // on Folder's "▼" glyph
        Assert.False(folder.Expanded);        // collapsed
        Assert.True(folder.Selected);
        Assert.DoesNotContain("Alpha", ConsoleSnapshot.ToText(tree, 24, 10));

        // A label click in between (a different position) so the next glyph click is a fresh single click, not a
        // double — re-expanding the node.
        ClickTree(tree, 8, 1);                // Folder's label: selects, no toggle (still collapsed)
        Assert.False(folder.Expanded);
        ClickTree(tree, 4, 1);                // glyph again -> re-expand
        Assert.True(folder.Expanded);
        Assert.Contains("Alpha", ConsoleSnapshot.ToText(tree, 24, 10));
    }

    [Fact]
    public void Tree_DoubleClick_OnLabel_TogglesNode()
    {
        var tree = new Tree("Root");
        var folder = tree.AddNode("Folder");  // row 1
        folder.AddChild("Alpha");
        ConsoleSnapshot.Render(tree, 24, 10);

        // Two clicks on the label at the same position register as a double-click (toggles once).
        ClickTree(tree, 8, 1);
        ClickTree(tree, 8, 1);

        Assert.False(folder.Expanded);
    }

    [Fact]
    public void Tree_NodeActivated_FiresForLeaf_OnEnterAndDoubleClick_ButNotForParents()
    {
        var tree = new Tree("Root");
        var folder = tree.AddNode("Folder");
        var alpha = folder.AddChild("Alpha");
        ConsoleSnapshot.Render(tree, 24, 10);

        var activated = new System.Collections.Generic.List<Tree.TreeNode>();
        tree.NodeActivated += (_, n) => activated.Add(n);

        // Enter on a selected parent toggles (no activation); Enter on a selected leaf activates.
        Press(tree, ConsoleKey.DownArrow);   // root
        Press(tree, ConsoleKey.DownArrow);   // Folder (parent)
        Press(tree, ConsoleKey.Enter);       // toggles Folder, no activation
        Assert.Empty(activated);

        Press(tree, ConsoleKey.RightArrow);  // re-expand Folder
        Press(tree, ConsoleKey.DownArrow);   // Alpha (leaf)
        Press(tree, ConsoleKey.Enter);       // activates Alpha
        Assert.Equal(new[] { alpha }, activated);

        // Double-clicking a leaf also activates it.
        ClickTree(tree, 8, 2);               // single click Alpha
        ClickTree(tree, 8, 2);               // -> double click
        Assert.Equal(new[] { alpha, alpha }, activated);
    }

    [Fact]
    public void Tree_Hover_TintsHoveredRow_AndClearsOnLeave()
    {
        var tree = new Tree("Root") { HoverHighlighting = true };   // opt in (off by default)
        var folder = tree.AddNode("Folder");
        folder.AddChild("Alpha");   // row 2
        folder.AddChild("Beta");    // row 3
        var ml = (ConsoleGUI.Input.IMouseListener)tree;

        // No hover yet: no leaf bullet carries a background.
        var idle = LeafBulletBackgrounds(ConsoleSnapshot.Render(tree, 24, 10));
        Assert.All(idle, bg => Assert.Null(bg));

        // Hover Alpha's row: exactly its bullet (folded with the label) gets the hover background.
        ml.OnMouseMove(new ConsoleGUI.Space.Position(8, 2));
        var hovering = LeafBulletBackgrounds(ConsoleSnapshot.Render(tree, 24, 10));
        Assert.Single(hovering, bg => bg is not null);

        // Pointer leaves: the tint is cleared.
        ml.OnMouseLeave();
        var left = LeafBulletBackgrounds(ConsoleSnapshot.Render(tree, 24, 10));
        Assert.All(left, bg => Assert.Null(bg));
    }

    [Fact]
    public void Tree_Hover_IsIgnored_WhenHighlightingDisabled()
    {
        var tree = new Tree("Root");   // HoverHighlighting defaults to false
        var folder = tree.AddNode("Folder");
        folder.AddChild("Alpha");

        ((ConsoleGUI.Input.IMouseListener)tree).OnMouseMove(new ConsoleGUI.Space.Position(8, 2));

        // Moving the pointer over a row tints nothing while highlighting is off.
        Assert.All(LeafBulletBackgrounds(ConsoleSnapshot.Render(tree, 24, 10)), bg => Assert.Null(bg));
    }

    [Fact]
    public void Tree_Navigation_SkipsCollapsedChildren()
    {
        var tree = new Tree("Root");
        var folder = tree.AddNode("Folder");
        folder.AddChildren("Leaf1", "Leaf2");
        var sibling = tree.AddNode("Sibling");

        // Collapse Folder, then Down should land on Sibling (not the hidden Leaf1).
        Press(tree, ConsoleKey.DownArrow);   // select root
        Press(tree, ConsoleKey.DownArrow);   // select Folder
        Press(tree, ConsoleKey.LeftArrow);   // collapse it
        Press(tree, ConsoleKey.DownArrow);   // next visible node

        Assert.True(sibling.Selected);
        Assert.False(folder.Selected);
    }

    [Fact]
    public void ListBox_AutoScroll_BringsSelectionIntoView()
    {
        var list = new ListBox();
        for (var i = 1; i <= 40; i++) list.AddItem($"Item {i}");
        list.WithRoundedBorder(Color.Blue).WithTitle("Scroll");

        // Initial frame: top of the list is visible.
        var before = ConsoleSnapshot.ToText(list, 24, 10);
        Assert.Contains("Item 1", before);

        // Navigate well past the viewport.
        for (var i = 0; i < 25; i++) Press(list, ConsoleKey.DownArrow);

        var after = ConsoleSnapshot.ToText(list, 24, 10);

        Assert.True(list.Frame!.Top > 0, "Frame should have scrolled.");
        Assert.Contains("Item 26", after);   // only visible after auto-scrolling
    }

    [Fact]
    public void Tree_Navigation_ScrollsAndSelects_WithSnapshots()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "snapshots");

        var tree = new Tree("Project", TreeGuide.BoldLine)
        {
            SelectedForegroundColor = Color.White,
            SelectedBackgroundColor = Color.Blue
        };
        for (var i = 1; i <= 8; i++)
            tree.AddNode($"Folder {i}").AddChildren($"file{i}a.cs", $"file{i}b.cs");
        tree.WithRoundedBorder(Color.Purple).WithTitle("Tree");

        // Snapshot the initial state (selection at the root, top of the view).
        ConsoleSnapshot.SavePng(tree, 30, 12, Path.Combine(dir, "nav_tree_step0.png"));

        // Drive the tree down past the viewport with a single call, then snapshot the result.
        var down = Enumerable.Repeat(ConsoleKey.DownArrow, 14).ToArray();
        ConsoleSnapshot.SavePngAfter(tree, 30, 12, Path.Combine(dir, "nav_tree_step1.png"), down);

        var after = ConsoleSnapshot.ToText(tree, 30, 12);

        Assert.True(tree.Frame!.Top > 0, "Tree should have scrolled after navigating down.");
        Assert.DoesNotContain("Project", after);   // root scrolled out of view
    }

    [Fact]
    public void ListBox_AltScroll_UsesModifierKeys()
    {
        var list = new ListBox();
        for (var i = 1; i <= 40; i++) list.AddItem($"Item {i}");
        list.WithRoundedBorder(Color.Teal).WithTitle("AltScroll");

        // Frame-level scroll (Alt+Down) moves the view without changing the ListBox selection.
        var altDown = Enumerable.Repeat(ConsoleSnapshot.Key(ConsoleKey.DownArrow, alt: true), 6).ToArray();
        var text = ConsoleSnapshot.ToTextAfter(list, 24, 8, altDown);

        Assert.Equal(6, list.Frame!.Top);          // six Alt+Down scrolls
        Assert.Contains("Item 7", text);           // only visible after scrolling
    }

    [Fact]
    public void RenderGallery_WritesPngsForVisualInspection()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "snapshots");

        // Title styles
        var titled = new TextLabel(TextLabelOrientation.Horizontal, "content", Color.White) { Width = 22, Height = 1 };
        titled.WithRoundedBorder(Color.Green)
              .WithTitle("Title", new TitleStyle(TitlePos.TopCenter, TitleBorderStyle.Inline, TitleColorStyle.Reverse));
        ConsoleSnapshot.SavePng(titled, 28, 6, Path.Combine(dir, "title_inline_reverse.png"));

        // ListBox with a styled scrollbar
        var list = new ListBox { SelectedForegroundColor = Color.White, SelectedBackgroundColor = Color.Blue };
        for (var i = 1; i <= 40; i++) list.AddItem($"Item {i}");
        list.WithRoundedBorder(Color.Purple).WithTitle("List")
            .WithScrollBarGlyphs(ScrollBarGlyphs.Block)
            .WithScrollBarStyle(ScrollBarStyle.Default.WithColors(thumb: Color.Cyan1, arrows: Color.Yellow));
        ConsoleSnapshot.SavePng(list, 26, 12, Path.Combine(dir, "listbox_scrollbar.png"));

        // Tree
        var tree = new Tree("Project", TreeGuide.BoldLine);
        for (var i = 1; i <= 4; i++) tree.AddNode($"Folder {i}").AddChildren($"file{i}a", $"file{i}b");
        tree.WithRoundedBorder(Color.Green).WithTitle("Tree");
        ConsoleSnapshot.SavePng(tree, 30, 14, Path.Combine(dir, "tree.png"));

        // Text decorations (markup -> cell Decoration -> font/text properties)
        var deco = new ListBox();
        deco.AddItem("[bold]Bold text[/]");
        deco.AddItem("[italic]Italic text[/]");
        deco.AddItem("[underline]Underlined[/]");
        deco.AddItem("[strikethrough]Strikethrough[/]");
        deco.AddItem("[dim]Dim text[/]");
        deco.AddItem("[invert]Inverted text[/]");
        deco.WithRoundedBorder(Color.Silver).WithTitle("Decorations");
        ConsoleSnapshot.SavePng(deco, 28, 12, Path.Combine(dir, "decorations.png"));

        Assert.True(File.Exists(Path.Combine(dir, "title_inline_reverse.png")));
        Assert.True(File.Exists(Path.Combine(dir, "listbox_scrollbar.png")));
        Assert.True(File.Exists(Path.Combine(dir, "tree.png")));
        Assert.True(File.Exists(Path.Combine(dir, "decorations.png")));
    }
}
