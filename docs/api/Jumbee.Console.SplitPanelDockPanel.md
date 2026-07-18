# <a id="Jumbee_Console_SplitPanelDockPanel"></a> Class SplitPanelDockPanel

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

The visual scaffold behind <xref href="Jumbee.Console.SplitPanel" data-throw-if-not-resolved="false"></xref>: two nested ConsoleGUI <xref href="Jumbee.Console.DockPanel" data-throw-if-not-resolved="false"></xref>s laying out
<code>[first | divider | second]</code> along the split axis.

```csharp
public sealed class SplitPanelDockPanel : DockPanel
```

#### Inheritance

object ← 
Control ← 
DockPanel ← 
[SplitPanelDockPanel](Jumbee.Console.SplitPanelDockPanel.md)

## Remarks

The first pane is docked with a fixed extent (a <xref href="Jumbee.Console.Boundary" data-throw-if-not-resolved="false"></xref>), the 1-cell divider is docked next, and the
second pane fills the rest — so on container resize the first pane keeps its size and the second pane absorbs the
change (the sidebar-stays-put behaviour). <xref href="Jumbee.Console.SplitPanel" data-throw-if-not-resolved="false"></xref> owns the model and drives this through
<xref href="Jumbee.Console.SplitPanelDockPanel.SetFirstExtent(System.Int32)" data-throw-if-not-resolved="false"></xref>.

