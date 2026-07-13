namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Spectre.Console.Rendering;
using Spectre.Console;

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
        /// <param name="renderable">The tree node label.</param>
        internal TreeNode(Tree tree, uint index, IRenderable label, TreeNode? parent = null, string? text = null)
        {
            Tree = tree;
            Index = index;
            Label = label;
            Parent = parent;
            Text = text;
        }

        internal TreeNode(Tree tree, uint index, string label, TreeNode? parent = null) : this(tree, index, new Markup(label), parent) {}
        #endregion

        #region Properties
        public Tree Tree { get; protected set; }

        public TreeNode? Parent { get; protected set; }

        public uint Index { get; }

        public string? Text { get; internal set; }

        public IRenderable Label
        {
            get => field;
            set
            {
                field = value;
                _cachedLabelLines = null;   // the label changed -> drop its cached render (see Tree.Render)
                UpdateTree();
            }
        }

        /// <summary>A glyph shown before THIS node when it is a leaf (has no children), overriding the tree-wide
        /// <see cref="Tree.LeafGlyph"/>. Include any trailing spacing, and keep its cell width equal to the tree's
        /// other glyphs so labels stay aligned. <see langword="null"/> (the default) uses the tree-wide glyph.
        /// Has no effect while the node has children (it then shows the disclosure glyph).</summary>
        public string? LeafGlyph
        {
            get => field;
            set { field = value; UpdateTree(); }
        }

        /// <summary>Foreground colour for this node's <see cref="LeafGlyph"/>, overriding the tree-wide
        /// <see cref="Tree.LeafGlyphColor"/>. <see langword="null"/> (the default) uses the tree-wide colour. Ignored
        /// while the node is selected (the glyph highlights together with the label).</summary>
        public Color? LeafGlyphColor
        {
            get => field;
            set { field = value; UpdateTree(); }
        }

        /// <summary>Disclosure glyph shown before THIS node while it has children and is expanded, overriding the
        /// tree-wide <see cref="IGlyphTheme.TreeExpanded"/> glyph. Include any trailing spacing and keep its cell
        /// width equal to the tree's other glyphs so labels stay aligned. <see langword="null"/> (the default) uses
        /// the tree-wide glyph. Dormant while the node is a leaf (it then shows the leaf glyph).</summary>
        public string? ExpandedGlyph
        {
            get => field;
            set { field = value; UpdateTree(); }
        }

        /// <summary>Disclosure glyph shown before THIS node while it has children and is collapsed, overriding the
        /// tree-wide <see cref="IGlyphTheme.TreeCollapsed"/> glyph. <see langword="null"/> (the default) uses the
        /// tree-wide glyph.</summary>
        public string? CollapsedGlyph
        {
            get => field;
            set { field = value; UpdateTree(); }
        }

        /// <summary>Foreground colour for THIS node's disclosure glyph (expanded/collapsed). <see langword="null"/>
        /// (the default) draws it in the tree's guide style, as before. Ignored while the node is selected.</summary>
        public Color? DisclosureGlyphColor
        {
            get => field;
            set { field = value; UpdateTree(); }
        }

        /// <summary>
        /// Gets the tree node's child nodes.
        /// </summary>
        public ICollection<TreeNode> Children => _children.Values;

        /// <summary>
        /// Gets or sets a value indicating whether or not the tree node is expanded. Collapsing hides this node's
        /// children (they no longer render and are skipped by navigation); expanding shows them again. Toggling
        /// redraws the tree.
        /// </summary>
        public bool Expanded
        {
            get => _expanded;
            set
            {
                if (_expanded != value)
                {
                    _expanded = value;
                    UpdateTree();
                }
            }
        }

        public bool IsRemoved { get; internal set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the node is selected.
        /// </summary>
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    UpdateTree();
                }
            }
        }

        internal IRenderable Renderable => Label;

        internal ICollection<TreeNode> Nodes => _children.Values;
        #endregion

        #region Indexers
        public TreeNode? this[uint id] => _children.TryGetValue(id, out var node) ? node : null;
        #endregion
        
        #region Methods
        public TreeNode AddChild(IRenderable label, string? text = null)
        {
            // Create the node (with an atomic index) on the calling thread so we can return it immediately;
            // marshal the dictionary mutation onto the UI thread.
            var c = new TreeNode(this.Tree, Interlocked.Increment(ref childIndex), label, this, text);
            UI.Invoke(() =>
            {
                _children[c.Index] = c;
                UpdateTree();
            });
            return c;
        }

        public TreeNode AddChild(string label) => AddChild(new Markup(label), label);

        public void AddChildren(params IRenderable[] children)
        {
            foreach (IRenderable child in children)
            {
                AddChild(child);
            }
        }

        public void AddChildren(params string[] children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }

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

        protected void UpdateTree()
        {
            if (!IsRemoved) Tree.Update();
        }
        #endregion

        #region Fields
        private Dictionary<uint, TreeNode> _children = new();
        private uint childIndex = 0;
        private bool _selected;
        private bool _expanded = true;
        // Cached result of Segment.SplitLines(Label.Render(options, width)) for the node's UNFOLDED render, reused
        // across selection/hover/navigation repaints (which re-render the whole tree but don't change this node's
        // label). Keyed on the width it was produced at; invalidated when Label changes. See Tree.Render.
        internal List<SegmentLine>? _cachedLabelLines;
        internal int _cachedLabelWidth = -1;
        #endregion
    }
}