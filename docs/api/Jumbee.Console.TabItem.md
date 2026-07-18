# <a id="Jumbee_Console_TabItem"></a> Class TabItem

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A handle to a tab in a <xref href="Jumbee.Console.TabPanel" data-throw-if-not-resolved="false"></xref> — stable across add/remove/reorder (unlike a positional index).

```csharp
public sealed class TabItem
```

#### Inheritance

object ← 
[TabItem](Jumbee.Console.TabItem.md)

## Remarks

Use it to relabel, hide/show, disable/enable, or query whether the tab is selected; pass it to
<xref href="Jumbee.Console.TabPanel.RemoveTab(Jumbee.Console.TabItem)" data-throw-if-not-resolved="false"></xref> to remove it.

## Properties

### <a id="Jumbee_Console_TabItem_Closable"></a> Closable

Per-tab override of whether this tab shows a close (✕) glyph.

```csharp
public bool Closable { get; set; }
```

#### Property Value

 bool

#### Remarks

Independent of the panel-wide <xref href="Jumbee.Console.TabPanel.ClosableTabs" data-throw-if-not-resolved="false"></xref> default (though setting that
    re-applies to every tab). Use to keep a specific tab non-closable (e.g. a pinned document).

### <a id="Jumbee_Console_TabItem_Content"></a> Content

The tab's content control.

```csharp
public IFocusable Content { get; }
```

#### Property Value

 [IFocusable](Jumbee.Console.IFocusable.md)

### <a id="Jumbee_Console_TabItem_IsDisabled"></a> IsDisabled

When <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> the tab is shown greyed-out and can't be selected or focused.

```csharp
public bool IsDisabled { get; set; }
```

#### Property Value

 bool

#### Remarks

Disabling the selected tab moves selection to the nearest selectable tab.

### <a id="Jumbee_Console_TabItem_IsHidden"></a> IsHidden

When <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> the tab is removed from the bar but kept in the model (can be shown
    again).

```csharp
public bool IsHidden { get; set; }
```

#### Property Value

 bool

#### Remarks

Hiding the selected tab moves selection to the nearest visible, enabled tab.

### <a id="Jumbee_Console_TabItem_IsSelected"></a> IsSelected

Whether this is the currently selected tab.

```csharp
public bool IsSelected { get; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_TabItem_Name"></a> Name

The tab label. Setting it relabels the header.

```csharp
public string Name { get; set; }
```

#### Property Value

 string

