namespace Jumbee.Console;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Interop;
using ConsoleGUI.Input;

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
        if (inputEvent.Key.Key == ConsoleKey.DownArrow)
        {
            NavigateTree(1);
            inputEvent.Handled = true;
        }
        else if (inputEvent.Key.Key == ConsoleKey.UpArrow)
        {
            NavigateTree(-1);
            inputEvent.Handled = true;
        }
    }
    
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
            
            IRenderable renderable = current.Renderable;
            // In Caret mode reserve a caret-width gutter on every node so the labels don't jump as the selection moves.
            var caret = _selectionStyle == SelectionStyle.Caret;
            if (!string.IsNullOrEmpty(current.Text))
            {
                if (current.Selected)
                {
                    // Indicate the selected node per SelectionStyle: a highlight, an underline, or a caret prefix.
                    var style = _selectionStyle.TextStyle(_selectedForegroundColor, _selectedBackgroundColor);
                    renderable = new Markup(_selectionStyle.Prefix(_selectionCaret) + current.Text, style);
                }
                else if (caret)
                {
                    renderable = new Markup(new string(' ', _selectionCaret.GetCellWidth()) + current.Text);
                }
            }

            var renderableLines = Segment.SplitLines(renderable.Render(options, maxWidth - Segment.CellCount(prefix)));

            foreach (var (_, isFirstLine, _, line) in renderableLines.Enumerate())
            {
                if (prefix.Count > 0)
                {
                    result.AddRange(prefix.ToList());
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
    #endregion
}
