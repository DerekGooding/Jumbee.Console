# <a id="Jumbee_Console_TabPanelDockPanel"></a> Class TabPanelDockPanel

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

The visual scaffold behind <xref href="Jumbee.Console.TabPanel" data-throw-if-not-resolved="false"></xref>: a ConsoleGUI <xref href="ConsoleGUI.Controls.DockPanel" data-throw-if-not-resolved="false"></xref> that
docks a thin tab bar (a horizontal or vertical stack of <xref href="Jumbee.Console.TabHeader" data-throw-if-not-resolved="false"></xref> cells) on one edge and fills the
rest with the active tab's content. It does no selection bookkeeping — <xref href="Jumbee.Console.TabPanel" data-throw-if-not-resolved="false"></xref> owns the model and
drives this through <xref href="Jumbee.Console.TabPanelDockPanel.SetHeaders(System.Collections.Generic.IEnumerable%7bConsoleGUI.IControl%7d)" data-throw-if-not-resolved="false"></xref> / <xref href="Jumbee.Console.TabPanelDockPanel.SetFill(ConsoleGUI.IControl)" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class TabPanelDockPanel : DockPanel
```

#### Inheritance

object ← 
Control ← 
DockPanel ← 
[TabPanelDockPanel](Jumbee.Console.TabPanelDockPanel.md)

## Properties

### <a id="Jumbee_Console_TabPanelDockPanel_IsHorizontalTabBar"></a> IsHorizontalTabBar

<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> when the tab bar runs horizontally (docked top or bottom).

```csharp
public bool IsHorizontalTabBar { get; }
```

#### Property Value

 bool

## Methods

### <a id="Jumbee_Console_TabPanelDockPanel_SetBarThickness_System_Int32_"></a> SetBarThickness\(int\)

Sets a vertical bar's cross-axis width to <code class="paramref">thickness</code> (no-op for a horizontal bar).

```csharp
public void SetBarThickness(int thickness)
```

#### Parameters

`thickness` int

### <a id="Jumbee_Console_TabPanelDockPanel_SetFill_ConsoleGUI_IControl_"></a> SetFill\(IControl?\)

Sets the fill region to the active tab's <code class="paramref">content</code>, or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> to clear it.

```csharp
public void SetFill(IControl? content)
```

#### Parameters

`content` IControl?

### <a id="Jumbee_Console_TabPanelDockPanel_SetHeaders_System_Collections_Generic_IEnumerable_ConsoleGUI_IControl__"></a> SetHeaders\(IEnumerable<IControl\>\)

Replaces the entire tab bar with <code class="paramref">headers</code>, in order.

```csharp
public void SetHeaders(IEnumerable<IControl> headers)
```

#### Parameters

`headers` IEnumerable<IControl\>

