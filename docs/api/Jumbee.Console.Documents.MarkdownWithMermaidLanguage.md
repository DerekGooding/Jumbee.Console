# <a id="Jumbee_Console_Documents_MarkdownWithMermaidLanguage"></a> Class MarkdownWithMermaidLanguage

Namespace: [Jumbee.Console.Documents](Jumbee.Console.Documents.md)  
Assembly: Jumbee.Console.Documents.dll  

A ColorCode <xref href="ColorCode.ILanguage" data-throw-if-not-resolved="false"></xref> that highlights Markdown <em>and</em> the contents of embedded
<code>```mermaid</code> fenced blocks (using the <xref href="Jumbee.Console.Documents.MermaidLanguage" data-throw-if-not-resolved="false"></xref> grammar) — for editing Markdown that
contains mermaid diagrams in a <xref href="Jumbee.Console.CodeEditor" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class MarkdownWithMermaidLanguage
```

#### Inheritance

object ← 
[MarkdownWithMermaidLanguage](Jumbee.Console.Documents.MarkdownWithMermaidLanguage.md)

## Remarks

It reuses the built-in ColorCode Markdown rules and
prepends a <code>```mermaid</code>-fence rule whose inner content is delegated to the nested "mermaid" grammar via
ColorCode's language-embedding mechanism (a capture scope prefixed with <xref href="ColorCode.Common.ScopeName.LanguagePrefix" data-throw-if-not-resolved="false"></xref>).

## Fields

### <a id="Jumbee_Console_Documents_MarkdownWithMermaidLanguage_Instance"></a> Instance

The shared grammar instance (ColorCode caches the compiled grammar by <xref href="Jumbee.Console.Documents.MarkdownWithMermaidLanguage.Id" data-throw-if-not-resolved="false"></xref>).

```csharp
public static readonly MarkdownWithMermaidLanguage Instance
```

#### Field Value

 [MarkdownWithMermaidLanguage](Jumbee.Console.Documents.MarkdownWithMermaidLanguage.md)

## Properties

### <a id="Jumbee_Console_Documents_MarkdownWithMermaidLanguage_CssClassName"></a> CssClassName

The CSS class name used for HTML output.

```csharp
public string CssClassName { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_MarkdownWithMermaidLanguage_FirstLinePattern"></a> FirstLinePattern

First-line detection pattern (unused; <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>).

```csharp
public string FirstLinePattern { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_MarkdownWithMermaidLanguage_Id"></a> Id

The ColorCode language id (<code>"markdown-mermaid"</code>).

```csharp
public string Id { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_MarkdownWithMermaidLanguage_Name"></a> Name

The display name.

```csharp
public string Name { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_MarkdownWithMermaidLanguage_Rules"></a> Rules

The highlighting rules: the <code>```mermaid</code>-fence rule followed by the standard Markdown rules.

```csharp
public IList<LanguageRule> Rules { get; }
```

#### Property Value

 IList<LanguageRule\>

## Methods

### <a id="Jumbee_Console_Documents_MarkdownWithMermaidLanguage_HasAlias_System_String_"></a> HasAlias\(string\)

Always <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a> — this grammar has no aliases.

```csharp
public bool HasAlias(string lang)
```

#### Parameters

`lang` string

#### Returns

 bool

### <a id="Jumbee_Console_Documents_MarkdownWithMermaidLanguage_ToString"></a> ToString\(\)

Returns a string that represents the current object.

```csharp
public override string ToString()
```

#### Returns

 string

A string that represents the current object.

