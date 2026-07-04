namespace Jumbee.Console.Examples;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The 3-pane shell: a <see cref="Tree"/> of examples (left), the selected example's live control (middle), and a
/// read-only <see cref="MultiTabCodeEditor"/> of its source (right), over an always-on perf <see cref="StatusBar"/>.
/// The shell is itself built from Jumbee's own controls — it demonstrates the library while browsing it.
/// </summary>
public sealed class ExampleBrowser
{
    /// <summary>Builds the shell layout and selects the default example. Call once, then pass the result to
    /// <see cref="UI.Start"/>.</summary>
    public ILayout Build()
    {
        // The right pane is a read-only source viewer: no close (✕) or new-tab (+) affordances.
        _source.Tabs.ClosableTabs = false;
        _source.Tabs.ShowAddButton = false;

        // Group examples under category nodes; remember which node maps to which example.
        foreach (var group in ExampleCatalog.All.GroupBy(e => e.Category))
        {
            var category = _tree.AddNode(group.Key);
            foreach (var example in group) _map[category.AddChild(example.Title)] = example;
        }
        _tree.NodeActivated += (_, node) => { if (_map.TryGetValue(node, out var example)) Show(example); };

        var treeFramed = _tree.WithFrame(title: "Examples");
        var hostFramed = _host.WithFrame(title: "Example");
        _hostFrame = _host.Frame;
        var sourceFramed = _source.WithFrame(title: "Source");

        // tree | (example | source), over the status bar.
        _editorSplit = new SplitPanel(SplitOrientation.Horizontal, hostFramed, sourceFramed, splitPosition: 62);
        _treeSplit = new SplitPanel(SplitOrientation.Horizontal, treeFramed, _editorSplit, splitPosition: 28);
        var root = new DockPanel(DockedControlPlacement.Bottom, _status, _treeSplit);

        Show(ExampleCatalog.Default);
        return root;
    }

    /// <summary>The example tree (focus it so the arrows/Enter navigate examples).</summary>
    public Tree Tree => _tree;

    // Cycle keyboard focus through the three panes (tree → live example → source editor), landing on a control that
    // can actually be used/scrolled in each. Ctrl+arrows also move focus spatially; F6 is a simple, conflict-free
    // cycle (Alt+arrows are taken by the editor's tab-switching and frame scrolling).
    public void CyclePane(int direction)
    {
        var panes = new IFocusable?[] { _tree, _host, _source.ActiveEditor?.Editor };
        for (var i = 0; i < panes.Length; i++)
        {
            _pane = ((_pane + direction) % panes.Length + panes.Length) % panes.Length;
            if (panes[_pane] is { } target) { UI.SetFocus(target); return; }
        }
    }

    // Collapse/restore the left tree pane (its extent is the outer split's first-pane position; "collapsed" shrinks
    // it to the split's minimum).
    public void ToggleTree()
    {
        if (_treeCollapsed) _treeSplit.SplitPosition = _treeWidth;
        else { _treeWidth = _treeSplit.SplitPosition; _treeSplit.SplitPosition = 1; }
        _treeCollapsed = !_treeCollapsed;
    }

    // Collapse/restore the right source pane. It's the inner split's SECOND pane, so growing the inner divider (the
    // middle example) all the way shrinks the editor to the split's minimum.
    public void ToggleEditor()
    {
        if (_editorCollapsed) _editorSplit.SplitPosition = _editorWidth;
        else { _editorWidth = _editorSplit.SplitPosition; _editorSplit.SplitPosition = int.MaxValue / 2; }
        _editorCollapsed = !_editorCollapsed;
    }

    private void Show(IExample example)
    {
        _host.Show(example.Build(), example.Description);
        if (_hostFrame is not null) _hostFrame.Title = example.Title;
        _status.Current = $"{example.Category} › {example.Title}";

        _source.Clear();
        foreach (var file in example.SourceFiles)
        {
            var editor = _source.OpenDocument(file, SourceLoader.Read(file), SourceLoader.LanguageFor(file));
            editor.ReadOnly = true;   // a viewer, not an editor
        }
    }

    private readonly Tree _tree = new(".NET · Jumbee.Console");
    private readonly ExampleHost _host = new();
    private readonly MultiTabCodeEditor _source = new();
    private readonly StatusBar _status = new();
    private readonly Dictionary<Tree.TreeNode, IExample> _map = new();
    private ControlFrame? _hostFrame;
    private SplitPanel _treeSplit = null!, _editorSplit = null!;
    private bool _treeCollapsed, _editorCollapsed;
    private int _treeWidth = 28, _editorWidth = 62;
    private int _pane;
}
