# <a id="Jumbee_Console_Documents_MermaidLanguage"></a> Class MermaidLanguage

Namespace: [Jumbee.Console.Documents](Jumbee.Console.Documents.md)  
Assembly: Jumbee.Console.Documents.dll  

A ColorCode <xref href="ColorCode.ILanguage" data-throw-if-not-resolved="false"></xref> grammar for Mermaid diagram source, for syntax-highlighting a Mermaid document
in a <xref href="Jumbee.Console.CodeEditor" data-throw-if-not-resolved="false"></xref> (<code>new CodeEditor(MermaidLanguage.Instance)</code>).

```csharp
public sealed class MermaidLanguage
```

#### Inheritance

object ← 
[MermaidLanguage](Jumbee.Console.Documents.MermaidLanguage.md)

## Remarks

The token patterns are lifted from
the vendored Mermaider parsers (<code>ext/Mermaider/Parsing/*.cs</code>): the diagram-type and structural keyword lists,
the direction set, and the flowchart/sequence/class/ER arrow and relationship alternations. It classifies tokens
(it does not validate structure), so it highlights every diagram type uniformly.

<p>
Rules are applied in list order and the first to match a position wins, so the order is: comments, then quoted
strings and bracket labels (which must claim their text before keyword rules can colour a keyword sitting inside a
label), then arrows, then keywords, directions and numbers.
</p>

## Fields

### <a id="Jumbee_Console_Documents_MermaidLanguage_Instance"></a> Instance

The shared grammar instance (ColorCode caches the compiled grammar by <xref href="Jumbee.Console.Documents.MermaidLanguage.Id" data-throw-if-not-resolved="false"></xref>).

```csharp
public static readonly MermaidLanguage Instance
```

#### Field Value

 [MermaidLanguage](Jumbee.Console.Documents.MermaidLanguage.md)

## Properties

### <a id="Jumbee_Console_Documents_MermaidLanguage_CssClassName"></a> CssClassName

The CSS class name used for HTML output.

```csharp
public string CssClassName { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_MermaidLanguage_FirstLinePattern"></a> FirstLinePattern

First-line detection pattern (unused; <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>).

```csharp
public string FirstLinePattern { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_MermaidLanguage_Id"></a> Id

The ColorCode language id (<code>"mermaid"</code>).

```csharp
public string Id { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_MermaidLanguage_Name"></a> Name

The display name.

```csharp
public string Name { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_MermaidLanguage_Rules"></a> Rules

The ordered highlighting rules (the first to match a position wins).

```csharp
public IList<LanguageRule> Rules { get; }
```

#### Property Value

 IList<LanguageRule\>

## Methods

### <a id="Jumbee_Console_Documents_MermaidLanguage_HasAlias_System_String_"></a> HasAlias\(string\)

Returns <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> when <code class="paramref">lang</code> is a known alias (<code>mermaid</code>/<code>mmd</code>).

```csharp
public bool HasAlias(string lang)
```

#### Parameters

`lang` string

#### Returns

 bool

### <a id="Jumbee_Console_Documents_MermaidLanguage_ToString"></a> ToString\(\)

Returns a string that represents the current object.

```csharp
public override string ToString()
```

#### Returns

 string

A string that represents the current object.

