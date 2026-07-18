# <a id="Jumbee_Console_DocumentClosingEventArgs"></a> Class DocumentClosingEventArgs

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Arguments for <xref href="Jumbee.Console.MultiTabCodeEditor.DocumentClosing" data-throw-if-not-resolved="false"></xref>. Set <xref href="Jumbee.Console.DocumentClosingEventArgs.Cancel" data-throw-if-not-resolved="false"></xref> to keep the
    document open (e.g. after confirming unsaved changes).

```csharp
public sealed class DocumentClosingEventArgs : EventArgs
```

#### Inheritance

object ← 
EventArgs ← 
[DocumentClosingEventArgs](Jumbee.Console.DocumentClosingEventArgs.md)

## Properties

### <a id="Jumbee_Console_DocumentClosingEventArgs_Cancel"></a> Cancel

Set to <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> to cancel the close and keep the document open.

```csharp
public bool Cancel { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_DocumentClosingEventArgs_Editor"></a> Editor

The document's editor.

```csharp
public CodeEditor Editor { get; }
```

#### Property Value

 [CodeEditor](Jumbee.Console.CodeEditor.md)

