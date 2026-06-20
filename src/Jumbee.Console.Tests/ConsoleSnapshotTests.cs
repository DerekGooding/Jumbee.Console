namespace Jumbee.Console.Tests;

using System;
using System.Threading;

using ConsoleGUI.Input;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class ConsoleSnapshotTests
{
    #region Helpers
    private static void Press(Control control, ConsoleKey key)
    {
        var args = new UI.InputEventArgs(
            new Lock(),
            new InputEvent(new ConsoleKeyInfo('\0', key, shift: false, alt: false, control: false)));
        control.OnInput(args);
    }
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
            .WithScrollBarStyle(ScrollBarStyle.Block.WithColors(thumb: Color.Cyan1, arrows: Color.Yellow));
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
