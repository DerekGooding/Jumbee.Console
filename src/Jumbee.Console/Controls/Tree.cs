namespace Jumbee.Console;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Interop;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using CircularTreeException = Spectre.Console.Interop.CircularTreeException;

public enum TreeGuide
{
    Ascii,
    Line,
    BoldLine,
    DoubleLine
}

/// <summary>
/// Displays a hierarchical list of items in a tree layout.
/// </summary>
/// <remarks>
/// Based on <see cref="Spectre.Console.Tree"/> but modified to support mutable tree nodes, concurrent updates, and node selection via user input.
/// </remarks>
public partial class Tree : RenderableControl
{
    #region Constructors
    /// <summary>
    /// Create a tree with a root label.
    /// </summary>
    /// <param name="rootLabel">The tree root label.</param>
    public Tree(IRenderable rootLabel, TreeGuide? guide = null, Style? guideStyle = null, bool expanded = true) : base()
    {
        this._rootLabel = rootLabel;
        this._root = new TreeNode(this, 0, _rootLabel);
        this._style = guideStyle ?? Style.Plain;
        this._guide = guide ?? TreeGuide.Line;
        this.scguide = GetSpectreConsoleTreeGuide(this._guide);
        this._expanded = expanded;
        ApplyTheme();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree"/> class.
    /// </summary>
    /// <param name="rootText">The tree root label as a string.</param>
    public Tree(string rootText, TreeGuide? guide = null, Style ? guideStyle = null, bool expanded = true) : 
        this(new Markup(rootText), guide, guideStyle, expanded) 
    {
        _root.Text = rootText;
    }
    #endregion
    
    #region Properties
    public TreeNode Root => _root;

    /// <summary>
    /// Gets or sets the tree style.
    /// </summary>
    public Style Style
    {
        get => _style;
        set => SetAtomicProperty(ref _style, value);
    }
    /// <summary>
    /// Gets or sets the tree guide lines.
    /// </summary>
    public TreeGuide Guide
    {
        get => _guide;
        set => SetAtomicProperty(ref _guide, value, watch: (_, _) => scguide = GetSpectreConsoleTreeGuide(_guide));
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not the tree is expanded or not.
    /// </summary>
    public bool Expanded
    {
        get => _expanded;
        set => SetAtomicProperty(ref _expanded, value);
    }

    internal ICollection<TreeNode> Nodes => _root.Children;


    /// <summary>Foreground of the selected node. Defaults to the theme's <see cref="IStyleTheme.Selection"/>.</summary>
    public Color? SelectedForegroundColor
    {
        get => _selectedForegroundColor;
        set => SetAtomicProperty(ref _selectedForegroundColor, value, themeOverride: true);
    }

    /// <summary>Background of the selected node. Defaults to the theme's <see cref="IStyleTheme.Selection"/>.</summary>
    public Color? SelectedBackgroundColor
    {
        get => _selectedBackgroundColor;
        set => SetAtomicProperty(ref _selectedBackgroundColor, value, themeOverride: true);
    }

    /// <summary>How the selected node is indicated — highlight / underline / caret. Defaults to the theme's
    /// <see cref="IStyleTheme.SelectionStyle"/>.</summary>
    public SelectionStyle SelectionStyle
    {
        get => _selectionStyle;
        set => SetAtomicProperty(ref _selectionStyle, value, themeOverride: true);
    }

    /// <summary>The glyph shown before a node that has no children (a leaf), including trailing spacing. Defaults to
    /// the theme's <see cref="IGlyphTheme.TreeLeaf"/>. Keep its width equal to the disclosure glyphs so labels align.</summary>
    public string LeafGlyph
    {
        get => _leafGlyph;
        set => SetAtomicProperty(ref _leafGlyph, value, themeOverride: true);
    }

    /// <summary>Foreground colour of the <see cref="LeafGlyph"/>, or <see langword="null"/> for the default text
    /// colour. Defaults to the theme's <see cref="IStyleTheme.TreeLeaf"/> colour. When a (text) leaf is selected the
    /// glyph is highlighted together with the text instead of using this colour.</summary>
    public Color? LeafGlyphColor
    {
        get => _leafGlyphColor;
        set => SetAtomicProperty(ref _leafGlyphColor, value, themeOverride: true);
    }

    public override bool HandlesInput => true;

    #endregion

    #region Indexers
    public TreeNode? this[uint index] => _root[index];
    #endregion

    #region Methods
    // Default the selected-node colours from the theme so a bare Tree shows a visible selection (re-applied on a
    // runtime theme switch; explicit SelectedForeground/BackgroundColor overrides are left alone).
    protected override void ApplyTheme()
    {
        if (!IsThemeOverridden(nameof(SelectedForegroundColor))) _selectedForegroundColor = UI.StyleTheme.Selection.ForegroundColor;
        if (!IsThemeOverridden(nameof(SelectedBackgroundColor))) _selectedBackgroundColor = UI.StyleTheme.Selection.BackgroundColor;
        if (!IsThemeOverridden(nameof(SelectionStyle))) _selectionStyle = UI.StyleTheme.SelectionStyle;
        _selectionCaret = UI.GlyphTheme.SelectionCaret;
        _treeExpanded = UI.GlyphTheme.TreeExpanded;
        _treeCollapsed = UI.GlyphTheme.TreeCollapsed;
        if (!IsThemeOverridden(nameof(LeafGlyph))) _leafGlyph = UI.GlyphTheme.TreeLeaf;
        if (!IsThemeOverridden(nameof(LeafGlyphColor))) _leafGlyphColor = UI.StyleTheme.TreeLeaf.ForegroundColor;
    }

    public TreeNode AddNode(IRenderable label) => _root.AddChild(label);

    public TreeNode AddNode(string label) => _root.AddChild(label);

    public Tree AddNodes(params IRenderable[] labels)
    {
        _root.AddChildren(labels);
        return this;
    }

    public Tree AddNodes(params string[] labels)
    {
        _root.AddChildren(labels);
        return this;    
    }

    public bool RemoveNode(TreeNode node) => _root.RemoveChild(node.Index);   

    protected override void OnInput(InputEvent inputEvent)
    {
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.DownArrow:
                NavigateTree(1);
                inputEvent.Handled = true;
                break;
            case ConsoleKey.UpArrow:
                NavigateTree(-1);
                inputEvent.Handled = true;
                break;
            // Right: expand a collapsed parent, else step into its first child. Left: collapse an expanded parent,
            // else step out to the parent. Enter/Space toggle the selected parent. (No-op on a leaf with no parent.)
            case ConsoleKey.RightArrow:
                if (SelectedNode is { Nodes.Count: > 0 } r)
                {
                    if (!r.Expanded) r.Expanded = true;
                    else SelectNode(r.Nodes.OrderBy(n => n.Index).First());
                }
                inputEvent.Handled = true;
                break;
            case ConsoleKey.LeftArrow:
                if (SelectedNode is { } l)
                {
                    if (l.Nodes.Count > 0 && l.Expanded) l.Expanded = false;
                    else if (l.Parent is { } parent) SelectNode(parent);
                }
                inputEvent.Handled = true;
                break;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                if (SelectedNode is { Nodes.Count: > 0 } t) t.Expanded = !t.Expanded;
                inputEvent.Handled = true;
                break;
        }
    }

    /// <summary>The currently selected visible node, or <see langword="null"/> if none is selected.</summary>
    public TreeNode? SelectedNode => Flatten(_root).FirstOrDefault(n => n.Selected);

    // Mouse: each visible node is one row, so the listener's content-space Y is the flattened-node index (the frame
    // adjusts for scroll). A single click selects; clicking a parent's disclosure glyph toggles it; double-clicking
    // a parent's label toggles it — so a double-click anywhere on a parent row ends up toggled exactly once. (Wheel
    // scrolling is the inherited default: OnMouseWheel -> Frame.Scroll.)
    protected override void OnClick(Position position)
    {
        if (NodeAt(position.Y) is not { } node) return;
        if (node.Nodes.Count > 0 && OnGlyph(node, position.X)) node.Expanded = !node.Expanded;
        SelectNode(node);
    }

    protected override void OnDoubleClick(Position position)
    {
        if (NodeAt(position.Y) is not { } node) return;
        // A glyph click was already toggled by the preceding single-click; only toggle here for a label double-click.
        if (node.Nodes.Count > 0 && !OnGlyph(node, position.X)) node.Expanded = !node.Expanded;
        SelectNode(node);
    }

    // The visible node at a content row, or null if the row is past the tree.
    private TreeNode? NodeAt(int row)
    {
        var nodes = Flatten(_root).ToList();
        return row >= 0 && row < nodes.Count ? nodes[row] : null;
    }

    // True when content-space X falls within the node's gutter glyph (drawn at depth * guide-part width).
    private bool OnGlyph(TreeNode node, int x)
    {
        var glyphStart = Depth(node) * GuidePartWidth();
        var glyph = node.Nodes.Count > 0 ? (node.Expanded ? _treeExpanded : _treeCollapsed) : _leafGlyph;
        return x >= glyphStart && x < glyphStart + glyph.GetCellWidth();
    }

    // The node's depth (root = 0); its guide prefix is this many fixed-width guide parts.
    private static int Depth(TreeNode node)
    {
        var d = 0;
        for (var p = node.Parent; p is not null; p = p.Parent) d++;
        return d;
    }

    // The cell width of one guide part (e.g. "├── "); constant across parts for a given guide.
    private int GuidePartWidth() => scguide.GetSafeTreeGuide(safe: false).GetPart(TreeGuidePart.Continue).GetCellWidth();

    internal void Update() => this.Invalidate();

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var result = new List<Segment>();
        var visitedNodes = new HashSet<TreeNode>();

        var stack = new Stack<Queue<TreeNode>>();
        stack.Push(new Queue<TreeNode>(new[] { _root }));

        var levels = new List<Segment>();
        levels.Add(GetGuide(options, TreeGuidePart.Continue));

        while (stack.Count > 0)
        {
            var stackNode = stack.Pop();
            if (stackNode.Count == 0)
            {
                levels.RemoveLast();
                if (levels.Count > 0)
                {
                    levels.AddOrReplaceLast(GetGuide(options, TreeGuidePart.Fork));
                }

                continue;
            }

            var isLastChild = stackNode.Count == 1;
            var current = stackNode.Dequeue();
            if (!visitedNodes.Add(current))
            {
                throw new CircularTreeException("Cycle detected in tree - unable to render.");
            }

            stack.Push(stackNode);

            if (isLastChild)
            {
                levels.AddOrReplaceLast(GetGuide(options, TreeGuidePart.End));
            }

            var prefix = levels.Skip(1).ToList();
            
            // The gutter icon drawn before the label: a disclosure glyph for a parent (expanded/collapsed), or the
            // leaf glyph for a childless node. The leaf glyph carries its own foreground colour; the disclosure glyph
            // uses the guide style. Both should share a cell width so labels stay aligned.
            var hasChildren = current.Nodes.Count > 0;
            var icon = hasChildren ? (current.Expanded ? _treeExpanded : _treeCollapsed) : _leafGlyph;
            var iconWidth = icon.GetCellWidth();
            var iconStyle = !hasChildren && _leafGlyphColor is { } leafColor
                ? new Spectre.Console.Style(foreground: leafColor)
                : Style.SpectreConsoleStyle;

            var caret = _selectionStyle == SelectionStyle.Caret;
            // A node with Text renders through Markup, so a selected text node folds the icon INTO the highlighted
            // markup — the glyph is highlighted together with the text "in the usual manner". An IRenderable-only node
            // can't be folded or highlighted: its icon is drawn as a separate (coloured) segment and the label as-is.
            var selectedText = current.Selected && !string.IsNullOrEmpty(current.Text);

            IRenderable renderable = current.Renderable;
            if (selectedText)
            {
                var style = _selectionStyle.TextStyle(_selectedForegroundColor, _selectedBackgroundColor);
                renderable = new Markup(_selectionStyle.Prefix(_selectionCaret) + Markup.Escape(icon) + current.Text, style);
            }

            // In Caret mode reserve a caret-width gutter on every non-selected row so labels don't jump as selection moves.
            var caretPad = caret && !current.Selected ? _selectionCaret.GetCellWidth() : 0;
            var labelWidth = maxWidth - Segment.CellCount(prefix) - (selectedText ? 0 : iconWidth) - caretPad;
            var renderableLines = Segment.SplitLines(renderable.Render(options, labelWidth));

            foreach (var (_, isFirstLine, _, line) in renderableLines.Enumerate())
            {
                if (prefix.Count > 0)
                {
                    result.AddRange(prefix.ToList());
                }

                // For a non-folded node, draw the caret reservation (caret mode) then the gutter icon — the glyph on
                // the first line, a width-matched spacer on wrapped continuation lines.
                if (!selectedText)
                {
                    if (caretPad > 0) result.Add(new Segment(new string(' ', caretPad)));
                    result.Add(isFirstLine ? new Segment(icon, iconStyle) : new Segment(new string(' ', iconWidth)));
                }

                result.AddRange(line);
                result.Add(Segment.LineBreak);

                if (isFirstLine && prefix.Count > 0)
                {
                    var part = isLastChild ? TreeGuidePart.Space : TreeGuidePart.Continue;
                    prefix.AddOrReplaceLast(GetGuide(options, part));
                }
            }

            if (current.Expanded && current.Nodes.Count > 0)
            {
                levels.AddOrReplaceLast(GetGuide(options, isLastChild ? TreeGuidePart.Space : TreeGuidePart.Continue));
                levels.Add(GetGuide(options, current.Nodes.Count == 1 ? TreeGuidePart.End : TreeGuidePart.Fork));

                stack.Push(new Queue<TreeNode>(current.Nodes.OrderBy(n => n.Index)));
            }
        }

        return result;
    }
    
    protected static Spectre.Console.TreeGuide GetSpectreConsoleTreeGuide(TreeGuide guide) => guide switch
    {
        TreeGuide.Ascii => Spectre.Console.TreeGuide.Ascii,
        TreeGuide.Line => Spectre.Console.TreeGuide.Line,
        TreeGuide.BoldLine => Spectre.Console.TreeGuide.BoldLine,
        TreeGuide.DoubleLine => Spectre.Console.TreeGuide.DoubleLine,
        _ => Spectre.Console.TreeGuide.Line,
    };  

    private Segment GetGuide(RenderOptions options, TreeGuidePart part)
    {
        var guide = scguide.GetSafeTreeGuide(safe: !options.Unicode);
        return new Segment(guide.GetPart(part), Style);
    }

    private void NavigateTree(int direction)
    {
        var nodes = Flatten(_root).ToList();
        if (nodes.Count == 0) return;

        var current = nodes.FirstOrDefault(n => n.Selected);
        int nextIndex;

        if (current == null)
        {
            nextIndex = 0;
        }
        else
        {
            var currentIndex = nodes.IndexOf(current);
            nextIndex = (currentIndex + direction + nodes.Count) % nodes.Count;
            current.Selected = false;
        }

        nodes[nextIndex].Selected = true;
        AutoScroll(nextIndex);
    }

    // Move the selection to a specific (visible) node, clearing any previous selection and scrolling it into view.
    private void SelectNode(TreeNode node)
    {
        var nodes = Flatten(_root).ToList();
        var index = nodes.IndexOf(node);
        if (index < 0) return;   // not currently visible (e.g. under a collapsed ancestor)

        foreach (var n in nodes) if (n.Selected && !ReferenceEquals(n, node)) n.Selected = false;
        node.Selected = true;
        AutoScroll(index);
    }

    /// <summary>
    /// Scrolls the containing <see cref="ControlFrame"/> (if any) so the row at <paramref name="y"/>
    /// (the selected node's position in the flattened, visible tree) stays within the viewport.
    /// Each visible node occupies one row.
    /// </summary>
    private void AutoScroll(int y)
    {
        if (Frame == null) return;

        var top = Frame.Top;
        var viewportHeight = Frame.ViewportSize.Height;
        if (viewportHeight <= 0) return;

        if (y < top)
        {
            Frame.Top = y;
        }
        else if (y >= top + viewportHeight)
        {
            Frame.Top = y - viewportHeight + 1;
        }
    }

    private IEnumerable<TreeNode> Flatten(TreeNode node)
    {
        yield return node;
        if (node.Expanded)
        {
            foreach (var child in node.Nodes.OrderBy(n => n.Index))
            {
                foreach (var descendant in Flatten(child))
                {
                    yield return descendant;
                }
            }
        }
    }
    #endregion

    #region Fields
    public IRenderable _rootLabel;
    public TreeNode _root;
    protected Style _style;
    protected TreeGuide _guide;
    protected Spectre.Console.TreeGuide scguide; 
    protected bool _expanded;
    private Color? _selectedForegroundColor;
    private Color? _selectedBackgroundColor;
    private SelectionStyle _selectionStyle;
    private string _selectionCaret = "";
    private string _treeExpanded = "";
    private string _treeCollapsed = "";
    private string _leafGlyph = "";
    private Color? _leafGlyphColor;
    #endregion
}
