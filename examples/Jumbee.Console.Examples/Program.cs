namespace Jumbee.Console.Examples;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class Program
{
    static void Main(string[] args)
    {
        if (args.Contains("--verify")) { Environment.Exit(Verify.Run()); return; }
        ConsoleGUI.ConsoleManager.EmulateBlinkingCursor = true;

        // Show keyboard focus as a border on the focused pane (not the default full-background tint). Set before
        // building controls so they capture it.
        UI.StyleTheme = new ExamplesTheme();

        // --- the three panes + footer (plain controls; the shell is wired up right here) ---
        var tree = new Tree("Jumbee.Console").WithFrame(title: "Examples");
        var host = new ExampleHost().WithFrame(title: "Example");                 // swappable middle pane
        var source = new MultiTabCodeEditor().WithFrame(title: "Source");        // read-only source viewer
        source.Tabs.ClosableTabs = false;
        source.Tabs.ShowAddButton = false;
        var status = new StatusBar();

        // Group the catalogue under category nodes; remember which node maps to which example. Categories in
        // CategoryGlyphs get a custom folder glyph + colour so a themed section (e.g. Appearance) stands out.
        var map = new Dictionary<Tree.TreeNode, IExample>();
        foreach (var group in ExampleCatalog.All.GroupBy(e => e.Category))
        {
            var category = tree.AddNode(group.Key);
            if (CategoryGlyphs.TryGetValue(group.Key, out var glyph))
            {
                category.CollapsedGlyph = glyph.collapsed;
                category.ExpandedGlyph = glyph.expanded;
                category.DisclosureGlyphColor = glyph.color;
            }
            foreach (var example in group) map[category.AddChild(example.Title)] = example;
        }

        // Layout: tree | (example | source), over the status bar. Nested SplitPanels are resizable (drag/arrow the
        // divider); Ctrl+arrows move focus between the panes.
        var editorSplit = new SplitPanel(SplitOrientation.Horizontal, host, source, splitPosition: 102);
        var treeSplit = new SplitPanel(SplitOrientation.Horizontal, tree, editorSplit, splitPosition: 40);
        var root = new DockPanel(DockedControlPlacement.Bottom, status, treeSplit);

        // Select an example: swap the middle demo, retitle the pane, refresh the footer, reload the source tabs.
        void Show(IExample example)
        {
            host.Show(example, example.Description);
            host.Frame!.Title = example.Title;
            status.Current = $"{example.Category} › {example.Title}";
            source.Clear();
            foreach (var file in example.SourceFiles)
                source.OpenDocument(file, SourceLoader.Read(file), SourceLoader.LanguageFor(file)).ReadOnly = true;
            source.Tabs.SelectedIndex = 0;   // show the example's own source first, not the base-control tab
        }
        tree.NodeActivated += (_, node) => { if (map.TryGetValue(node, out var example)) Show(example); };
        Show(ExampleCatalog.Default);

        // Collapse/restore a side pane (shrink its split extent to the minimum and back).
        var (treeCollapsed, treeWidth) = (false, 28);
        var (editorCollapsed, editorWidth) = (false, 62);
        void ToggleTree()
        {
            if (treeCollapsed) treeSplit.SplitPosition = treeWidth;
            else { treeWidth = treeSplit.SplitPosition; treeSplit.SplitPosition = 1; }
            treeCollapsed = !treeCollapsed;
        }
        void ToggleEditor()
        {
            if (editorCollapsed) editorSplit.SplitPosition = editorWidth;
            else { editorWidth = editorSplit.SplitPosition; editorSplit.SplitPosition = int.MaxValue / 2; }
            editorCollapsed = !editorCollapsed;
        }

        var hud = new PerfHud();
        var run = UI.Start(root, width: 150, height: 42, isAnsiTerminal: true, input: new VtInputSource(anyMotion: true));
        hud.RegisterToggle();                                                    // Ctrl+G: the glass perf HUD
        UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.B), ToggleTree);            // collapse/restore the tree
        UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.E), ToggleEditor);          // collapse/restore the source pane
        // Quit: stop the active example's live feed first (so its timers/threads don't run through shutdown), then
        // stop the UI loop.
        void Quit() { host.DeactivateActive(); UI.Stop(); }
        UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.Q), Quit);
        UI.SetFocus(tree);
        run.Wait();
        host.DeactivateActive();   // belt-and-braces for a stop triggered elsewhere (e.g. a termination signal)
    }

    // Category name -> custom folder glyphs (collapsed / expanded) + disclosure colour, so a themed section stands
    // out in the tree. Glyphs are 2 cells wide to match the tree's default disclosure width and keep labels aligned.
    private static readonly Dictionary<string, (string collapsed, string expanded, Color color)> CategoryGlyphs = new()
    {
        ["Appearance"] = ("◇ ", "◈ ", new Color(0xc8, 0x92, 0xf0)),   // hollow / filled diamond in soft orchid
    };
}
