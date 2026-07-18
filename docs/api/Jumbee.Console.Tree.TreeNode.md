# <a id="Jumbee_Console_Tree_TreeNode"></a> Class Tree.TreeNode

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Represents a tree node.

```csharp
public class Tree.TreeNode
```

#### Inheritance

object ← 
[Tree.TreeNode](Jumbee.Console.Tree.TreeNode.md)

## Remarks

Based on <xref href="Spectre.Console.TreeNode" data-throw-if-not-resolved="false"></xref> but modified to have a mutable label and concurrent child nodes collection.

## Properties

### <a id="Jumbee_Console_Tree_TreeNode_Children"></a> Children

Gets the tree node's child nodes.

```csharp
public ICollection<Tree.TreeNode> Children { get; }
```

#### Property Value

 ICollection<[Tree](Jumbee.Console.Tree.md).[TreeNode](Jumbee.Console.Tree.TreeNode.md)\>

### <a id="Jumbee_Console_Tree_TreeNode_CollapsedGlyph"></a> CollapsedGlyph

Disclosure glyph shown before THIS node while it has children and is collapsed, overriding the
    tree-wide <xref href="Jumbee.Console.IGlyphTheme.TreeCollapsed" data-throw-if-not-resolved="false"></xref> glyph. <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> (the default) uses the
    tree-wide glyph.

```csharp
public string? CollapsedGlyph { get; set; }
```

#### Property Value

 string?

### <a id="Jumbee_Console_Tree_TreeNode_DisclosureGlyphColor"></a> DisclosureGlyphColor

Foreground colour for THIS node's disclosure glyph (expanded/collapsed). <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>
    (the default) draws it in the tree's guide style, as before.

```csharp
public Color? DisclosureGlyphColor { get; set; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)?

#### Remarks

Ignored while the node is selected.

### <a id="Jumbee_Console_Tree_TreeNode_Expanded"></a> Expanded

Gets or sets a value indicating whether or not the tree node is expanded.

```csharp
public bool Expanded { get; set; }
```

#### Property Value

 bool

#### Remarks

Collapsing hides this node's children (they no longer render and are skipped by navigation);
    expanding shows them again. Toggling redraws the tree.

### <a id="Jumbee_Console_Tree_TreeNode_ExpandedGlyph"></a> ExpandedGlyph

Disclosure glyph shown before THIS node while it has children and is expanded, overriding the
    tree-wide <xref href="Jumbee.Console.IGlyphTheme.TreeExpanded" data-throw-if-not-resolved="false"></xref> glyph. <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> (the default) uses
    the tree-wide glyph.

```csharp
public string? ExpandedGlyph { get; set; }
```

#### Property Value

 string?

#### Remarks

Include any trailing spacing and keep its cell width equal to the tree's other glyphs so labels
    stay aligned. Dormant while the node is a leaf (it then shows the leaf glyph).

### <a id="Jumbee_Console_Tree_TreeNode_Index"></a> Index

This node's stable index within its parent's children.

```csharp
public uint Index { get; }
```

#### Property Value

 uint

### <a id="Jumbee_Console_Tree_TreeNode_IsRemoved"></a> IsRemoved

Whether this node has been removed from the tree.

```csharp
public bool IsRemoved { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_Tree_TreeNode_Label"></a> Label

The renderable drawn as the node's label.

```csharp
public IRenderable Label { get; set; }
```

#### Property Value

 IRenderable

### <a id="Jumbee_Console_Tree_TreeNode_LeafGlyph"></a> LeafGlyph

A glyph shown before THIS node when it is a leaf (has no children), overriding the tree-wide
    <xref href="Jumbee.Console.Tree.LeafGlyph" data-throw-if-not-resolved="false"></xref>. <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> (the default) uses the tree-wide glyph.

```csharp
public string? LeafGlyph { get; set; }
```

#### Property Value

 string?

#### Remarks

Include any trailing spacing, and keep its cell width equal to the tree's other glyphs so labels
    stay aligned. Has no effect while the node has children (it then shows the disclosure glyph).

### <a id="Jumbee_Console_Tree_TreeNode_LeafGlyphColor"></a> LeafGlyphColor

Foreground colour for this node's <xref href="Jumbee.Console.Tree.TreeNode.LeafGlyph" data-throw-if-not-resolved="false"></xref>, overriding the tree-wide
    <xref href="Jumbee.Console.Tree.LeafGlyphColor" data-throw-if-not-resolved="false"></xref>. <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> (the default) uses the tree-wide colour.

```csharp
public Color? LeafGlyphColor { get; set; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)?

#### Remarks

Ignored while the node is selected (the glyph highlights together with the label).

### <a id="Jumbee_Console_Tree_TreeNode_Parent"></a> Parent

This node's parent, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> for the root.

```csharp
public Tree.TreeNode? Parent { get; protected set; }
```

#### Property Value

 [Tree](Jumbee.Console.Tree.md).[TreeNode](Jumbee.Console.Tree.TreeNode.md)?

### <a id="Jumbee_Console_Tree_TreeNode_Selected"></a> Selected

Gets or sets a value indicating whether the node is selected.

```csharp
public bool Selected { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_Tree_TreeNode_Text"></a> Text

The node's plain-text label, if it was created from a string; otherwise <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>.

```csharp
public string? Text { get; }
```

#### Property Value

 string?

### <a id="Jumbee_Console_Tree_TreeNode_Tree"></a> Tree

The tree this node belongs to.

```csharp
public Tree Tree { get; protected set; }
```

#### Property Value

 [Tree](Jumbee.Console.Tree.md)

### <a id="Jumbee_Console_Tree_TreeNode_Item_System_UInt32_"></a> this\[uint\]

Gets the child node with the given index, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> if none.

```csharp
public Tree.TreeNode? this[uint id] { get; }
```

#### Property Value

 [Tree](Jumbee.Console.Tree.md).[TreeNode](Jumbee.Console.Tree.TreeNode.md)?

## Methods

### <a id="Jumbee_Console_Tree_TreeNode_AddChild_Spectre_Console_Rendering_IRenderable_System_String_"></a> AddChild\(IRenderable, string?\)

Adds a child node with the given renderable label (and optional plain text) and returns it.

```csharp
public Tree.TreeNode AddChild(IRenderable label, string? text = null)
```

#### Parameters

`label` IRenderable

`text` string?

#### Returns

 [Tree](Jumbee.Console.Tree.md).[TreeNode](Jumbee.Console.Tree.TreeNode.md)

### <a id="Jumbee_Console_Tree_TreeNode_AddChild_System_String_"></a> AddChild\(string\)

Adds a child node with the given text label and returns it.

```csharp
public Tree.TreeNode AddChild(string label)
```

#### Parameters

`label` string

#### Returns

 [Tree](Jumbee.Console.Tree.md).[TreeNode](Jumbee.Console.Tree.TreeNode.md)

### <a id="Jumbee_Console_Tree_TreeNode_AddChildren_Spectre_Console_Rendering_IRenderable___"></a> AddChildren\(params IRenderable\[\]\)

Adds several child nodes from the given renderable labels.

```csharp
public void AddChildren(params IRenderable[] children)
```

#### Parameters

`children` IRenderable\[\]

### <a id="Jumbee_Console_Tree_TreeNode_AddChildren_System_String___"></a> AddChildren\(params string\[\]\)

Adds several child nodes from the given text labels.

```csharp
public void AddChildren(params string[] children)
```

#### Parameters

`children` string\[\]

### <a id="Jumbee_Console_Tree_TreeNode_RemoveChild_System_UInt32_"></a> RemoveChild\(uint\)

Removes the child node with the given index; returns <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> if it was removed.

```csharp
public bool RemoveChild(uint id)
```

#### Parameters

`id` uint

#### Returns

 bool

### <a id="Jumbee_Console_Tree_TreeNode_UpdateTree"></a> UpdateTree\(\)

Requests a redraw of the owning tree unless this node has been removed.

```csharp
protected void UpdateTree()
```

