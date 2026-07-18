# <a id="Jumbee_Console_TabCloseEventArgs"></a> Class TabCloseEventArgs

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Arguments for <xref href="Jumbee.Console.TabPanel.TabCloseRequested" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class TabCloseEventArgs : EventArgs
```

#### Inheritance

object ← 
EventArgs ← 
[TabCloseEventArgs](Jumbee.Console.TabCloseEventArgs.md)

## Remarks

Set <xref href="Jumbee.Console.TabCloseEventArgs.Cancel" data-throw-if-not-resolved="false"></xref> to keep the tab open (e.g. after confirming unsaved changes); otherwise the
    panel removes it.

## Properties

### <a id="Jumbee_Console_TabCloseEventArgs_Cancel"></a> Cancel

Set to <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> to cancel the close and keep the tab.

```csharp
public bool Cancel { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_TabCloseEventArgs_Tab"></a> Tab

The tab whose ✕ was clicked.

```csharp
public TabItem Tab { get; }
```

#### Property Value

 [TabItem](Jumbee.Console.TabItem.md)

