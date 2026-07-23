using Spectre.Console;
using Spectre.Console.Rendering;

namespace Jumbee.Console;

public partial class Tree
{
    /// <summary>
    /// Represents a tree node.
    /// </summary>
    /// <remarks>Based on <see cref="Spectre.Console.TreeNode"/> but modified to have a mutable label and concurrent child nodes collection.</remarks>
    public class TreeNode
    {
        #region Constructors

        /// <summary>
        /// Initializes a new <see cref="TreeNode"/> instance.
        /// </summary>
        /// <param name="tree">The tree this node belongs to.</param>
        /// <param name="index">The node's stable index within its parent's children.</param>
        /// <param name="label">The tree node label.</param>
        /// <param name="parent">The parent node, or <see langword="null"/> for the root.</param>
        /// <param name="text">The plain-text form of the label, when created from a string.</param>
        internal TreeNode(Tree tree, uint index, IRenderable label, TreeNode? parent = null, string? text = null)
        {
            Tree = tree;
            Index = index;
            Label = label;
            Parent = parent;
            Text = text;
        }

        internal TreeNode(Tree tree, uint index, string label, TreeNode? parent = null) : this(tree, index, new Markup(label), parent)
        {
        }

        #endregion Constructors

        #region Properties

        /// <summary>The tree this node belongs to.</summary>
        public Tree Tree { get; protected set; }

        /// <summary>This node's parent, or <see langword="null"/> for the root.</summary>
        public TreeNode? Parent { get; protected set; }

        /// <summary>This node's stable index within its parent's children.</summary>
        public uint Index { get; }

        /// <summary>The node's plain-text label, if it was created from a string; otherwise <see langword="null"/>.</summary>
        public string? Text { get; internal set; }

        /// <summary>Arbitrary application data associated with this node — e.g. the domain object it represents — so
        /// you can map a selected/activated node back to your model without a side dictionary. Not used by the tree.</summary>
        public object? Tag { get; set; }

        /// <summary>The renderable drawn as the node's label.</summary>
        public IRenderable Label
        {
            get;
            set
            {
                field = value;
                _cachedLabelLines = null;   // the label changed -> drop its cached render (see Tree.Render)
                UpdateTree();
            }
        }

        /// <summary>A glyph shown before THIS node when it is a leaf (has no children), overriding the tree-wide
        /// <see cref="Tree.LeafGlyph"/>. <see langword="null"/> (the default) uses the tree-wide glyph.</summary>
        /// <remarks>Include any trailing spacing, and keep its cell width equal to the tree's other glyphs so labels
        /// stay aligned. Has no effect while the node has children (it then shows the disclosure glyph).</remarks>
        public string? LeafGlyph
        {
            get;
            set { field = value; UpdateTree(); }
        }

        /// <summary>Foreground colour for this node's <see cref="LeafGlyph"/>, overriding the tree-wide
        /// <see cref="Tree.LeafGlyphColor"/>. <see langword="null"/> (the default) uses the tree-wide colour.</summary>
        /// <remarks>Ignored while the node is selected (the glyph highlights together with the label).</remarks>
        public Color? LeafGlyphColor
        {
            get;
            set { field = value; UpdateTree(); }
        }

        /// <summary>Disclosure glyph shown before THIS node while it has children and is expanded, overriding the
        /// tree-wide <see cref="IGlyphTheme.TreeExpanded"/> glyph. <see langword="null"/> (the default) uses
        /// the tree-wide glyph.</summary>
        /// <remarks>Include any trailing spacing and keep its cell width equal to the tree's other glyphs so labels
        /// stay aligned. Dormant while the node is a leaf (it then shows the leaf glyph).</remarks>
        public string? ExpandedGlyph
        {
            get;
            set { field = value; UpdateTree(); }
        }

        /// <summary>Disclosure glyph shown before THIS node while it has children and is collapsed, overriding the
        /// tree-wide <see cref="IGlyphTheme.TreeCollapsed"/> glyph. <see langword="null"/> (the default) uses the
        /// tree-wide glyph.</summary>
        public string? CollapsedGlyph
        {
            get;
            set { field = value; UpdateTree(); }
        }

        /// <summary>Foreground colour for THIS node's disclosure glyph (expanded/collapsed). <see langword="null"/>
        /// (the default) draws it in the tree's guide style, as before.</summary>
        /// <remarks>Ignored while the node is selected.</remarks>
        public Color? DisclosureGlyphColor
        {
            get;
            set { field = value; UpdateTree(); }
        }

        /// <summary>
        /// Gets the tree node's child nodes.
        /// </summary>
        public ICollection<TreeNode> Children => _children.Values;

        /// <summary>
        /// Gets or sets a value indicating whether or not the tree node is expanded.
        /// </summary>
        /// <remarks>Collapsing hides this node's children (they no longer render and are skipped by navigation);
        /// expanding shows them again. Toggling redraws the tree.</remarks>
        public bool Expanded
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    UpdateTree();
                }
            }
        } = true;

        /// <summary>Whether this node has been removed from the tree.</summary>
        public bool IsRemoved { get; internal set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the node is selected.
        /// </summary>
        public bool Selected
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    UpdateTree();
                }
            }
        }

        internal IRenderable Renderable => Label;

        internal ICollection<TreeNode> Nodes => _children.Values;

        #endregion Properties

        #region Indexers

        /// <summary>Gets the child node with the given index, or <see langword="null"/> if none.</summary>
        public TreeNode? this[uint id] => _children.TryGetValue(id, out var node) ? node : null;

        #endregion Indexers

        #region Methods

        /// <summary>Adds a child node with the given renderable label (and optional plain text) and returns it.</summary>
        public TreeNode AddChild(IRenderable label, string? text = null)
        {
            // Create the node (with an atomic index) on the calling thread so we can return it immediately;
            // marshal the dictionary mutation onto the UI thread.
            var c = new TreeNode(Tree, Interlocked.Increment(ref childIndex), label, this, text);
            UI.Invoke(() =>
            {
                _children[c.Index] = c;
                UpdateTree();
            });
            return c;
        }

        /// <summary>Adds a child node with the given text label and returns it.</summary>
        public TreeNode AddChild(string label) => AddChild(new Markup(label), label);

        /// <summary>Adds several child nodes from the given renderable labels.</summary>
        public void AddChildren(params IRenderable[] children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }

        /// <summary>Adds several child nodes from the given text labels.</summary>
        public void AddChildren(params string[] children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }

        /// <summary>Removes the child node with the given index; returns <see langword="true"/> if it was removed.</summary>
        public bool RemoveChild(uint id)
        {
            // Reliable only when called on the UI thread; off-thread the removal is marshaled and deferred.
            var removed = false;
            UI.Invoke(() =>
            {
                if (_children.Remove(id, out var c))
                {
                    c.Parent = null;
                    c.IsRemoved = true;
                    UpdateTree();
                    removed = true;
                }
            });
            return removed;
        }

        /// <summary>Requests a redraw of the owning tree unless this node has been removed. The mutable properties
        /// (<see cref="Label"/>, the glyphs, …) already call this, so it's only needed after changing something a
        /// setter doesn't cover (e.g. mutating the underlying renderable in place).</summary>
        public void UpdateTree()
        {
            if (!IsRemoved) Tree.Update();
        }

        #endregion Methods

        #region Fields

        private Dictionary<uint, TreeNode> _children = [];
        private uint childIndex = 0;

        // Cached result of Segment.SplitLines(Label.Render(options, width)) for the node's UNFOLDED render, reused
        // across selection/hover/navigation repaints (which re-render the whole tree but don't change this node's
        // label). Keyed on the width it was produced at; invalidated when Label changes. See Tree.Render.
        internal List<SegmentLine>? _cachedLabelLines;

        internal int _cachedLabelWidth = -1;

        #endregion Fields
    }
}