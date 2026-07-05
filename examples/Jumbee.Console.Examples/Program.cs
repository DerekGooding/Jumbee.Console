namespace Jumbee.Console.Examples;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class Program
{
    static void Main(string[] args)
    {
        if (args.Contains("--verify")) { Environment.Exit(Verify.Run()); return; }

        // Show keyboard focus as a border on the focused pane (not the default full-background tint). Set before
        // building controls so they capture it.
        UI.StyleTheme = new ExamplesTheme();

        // --- the three panes + footer (plain controls; the shell is wired up right here) ---
        var tree = new Tree(".NET · Jumbee.Console");
        var host = new ExampleHost();                 // swappable middle pane
        var source = new MultiTabCodeEditor();        // read-only source viewer
        source.Tabs.ClosableTabs = false;
        source.Tabs.ShowAddButton = false;
        var status = new StatusBar();

        // Group the catalogue under category nodes; remember which node maps to which example.
        var map = new Dictionary<Tree.TreeNode, IExample>();
        foreach (var group in ExampleCatalog.All.GroupBy(e => e.Category))
        {
            var category = tree.AddNode(group.Key);
            foreach (var example in group) map[category.AddChild(example.Title)] = example;
        }

        // Layout: tree | (example | source), over the status bar. Nested SplitPanels are resizable (drag/arrow the
        // divider); Ctrl+arrows move focus between the panes.
        var treeFramed = tree.WithFrame(title: "Examples");
        var hostFramed = host.WithFrame(title: "Example");
        var sourceFramed = source.WithFrame(title: "Source");
        var editorSplit = new SplitPanel(SplitOrientation.Horizontal, hostFramed, sourceFramed, splitPosition: 62);
        var treeSplit = new SplitPanel(SplitOrientation.Horizontal, treeFramed, editorSplit, splitPosition: 28);
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
        UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.Q), UI.Stop);
        UI.SetFocus(tree);
        run.Wait();
    }
}
