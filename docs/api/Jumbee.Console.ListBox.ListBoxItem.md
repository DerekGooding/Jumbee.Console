# <a id="Jumbee_Console_ListBox_ListBoxItem"></a> Class ListBox.ListBoxItem

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

An item in a ListBox.

```csharp
public class ListBox.ListBoxItem
```

#### Inheritance

object ← 
[ListBox.ListBoxItem](Jumbee.Console.ListBox.ListBoxItem.md)

## Constructors

### <a id="Jumbee_Console_ListBox_ListBoxItem__ctor_Jumbee_Console_ListBox_System_Int32_Spectre_Console_Rendering_IRenderable_"></a> ListBoxItem\(ListBox, int, IRenderable\)

Initializes an item with the given renderable content at <code class="paramref">index</code> in <code class="paramref">listBox</code>.

```csharp
public ListBoxItem(ListBox listBox, int index, IRenderable content)
```

#### Parameters

`listBox` [ListBox](Jumbee.Console.ListBox.md)

`index` int

`content` IRenderable

### <a id="Jumbee_Console_ListBox_ListBoxItem__ctor_Jumbee_Console_ListBox_System_Int32_System_String_System_Nullable_Jumbee_Console_Color__System_Nullable_Jumbee_Console_Color__"></a> ListBoxItem\(ListBox, int, string, Color?, Color?\)

Initializes a text item with optional foreground/background colours at <code class="paramref">index</code> in <code class="paramref">listBox</code>.

```csharp
public ListBoxItem(ListBox listBox, int index, string text, Color? foreground = null, Color? background = null)
```

#### Parameters

`listBox` [ListBox](Jumbee.Console.ListBox.md)

`index` int

`text` string

`foreground` [Color](Jumbee.Console.Color.md)?

`background` [Color](Jumbee.Console.Color.md)?

## Fields

### <a id="Jumbee_Console_ListBox_ListBoxItem_Index"></a> Index

This item's stable index within its owning list.

```csharp
public readonly int Index
```

#### Field Value

 int

## Properties

### <a id="Jumbee_Console_ListBox_ListBoxItem_BackgroundColor"></a> BackgroundColor

Background colour of a text item.

```csharp
public Color? BackgroundColor { get; set; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)?

### <a id="Jumbee_Console_ListBox_ListBoxItem_Content"></a> Content

The renderable drawn for this item; setting it clears any text and re-measures the list.

```csharp
public IRenderable Content { get; set; }
```

#### Property Value

 IRenderable

### <a id="Jumbee_Console_ListBox_ListBoxItem_ForegroundColor"></a> ForegroundColor

Foreground colour of a text item.

```csharp
public Color? ForegroundColor { get; set; }
```

#### Property Value

 [Color](Jumbee.Console.Color.md)?

### <a id="Jumbee_Console_ListBox_ListBoxItem_IsDetached"></a> IsDetached

Whether the item has been detached from its list.

```csharp
public bool IsDetached { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_ListBox_ListBoxItem_ListBox"></a> ListBox

The list this item belongs to, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> once detached.

```csharp
public ListBox? ListBox { get; }
```

#### Property Value

 [ListBox](Jumbee.Console.ListBox.md)?

### <a id="Jumbee_Console_ListBox_ListBoxItem_Text"></a> Text

The item's plain text, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> if it was created from a renderable.

```csharp
public string? Text { get; set; }
```

#### Property Value

 string?

## Methods

### <a id="Jumbee_Console_ListBox_ListBoxItem_Detach"></a> Detach\(\)

Detaches the item from its owning list.

```csharp
public void Detach()
```

