# <a id="Jumbee_Console_Documents_AsciiDocLanguage"></a> Class AsciiDocLanguage

Namespace: [Jumbee.Console.Documents](Jumbee.Console.Documents.md)  
Assembly: Jumbee.Console.Documents.dll  

A ColorCode <xref href="ColorCode.ILanguage" data-throw-if-not-resolved="false"></xref> grammar for AsciiDoc source, for syntax-highlighting an AsciiDoc document in a
<xref href="Jumbee.Console.CodeEditor" data-throw-if-not-resolved="false"></xref> (<code>new CodeEditor(AsciiDocLanguage.Instance)</code>).

```csharp
public sealed class AsciiDocLanguage
```

#### Inheritance

object ← 
[AsciiDocLanguage](Jumbee.Console.Documents.AsciiDocLanguage.md)

## Remarks

AsciiDoc's parser (AdocNet) is
structure-based rather than regex-based, so — as with the Ace editor's AsciiDoc mode — the highlighter uses its own
token regexes (adapted from espadrine's Ace-mode gist), mapping AsciiDoc constructs onto the Markdown-family scopes
that the default syntax theme colours.

<p>
Rules are applied in list order and the first to match a position wins, so line-level constructs (comments,
headings, block titles/attributes, admonitions, attribute entries, list markers) come before the inline formatting
(links, monospace, bold, italic) — a heading line claims its whole row before an inline rule can recolour part of it.
</p>

## Fields

### <a id="Jumbee_Console_Documents_AsciiDocLanguage_Instance"></a> Instance

The shared grammar instance (ColorCode caches the compiled grammar by <xref href="Jumbee.Console.Documents.AsciiDocLanguage.Id" data-throw-if-not-resolved="false"></xref>).

```csharp
public static readonly AsciiDocLanguage Instance
```

#### Field Value

 [AsciiDocLanguage](Jumbee.Console.Documents.AsciiDocLanguage.md)

## Properties

### <a id="Jumbee_Console_Documents_AsciiDocLanguage_CssClassName"></a> CssClassName

The CSS class name used for HTML output.

```csharp
public string CssClassName { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocLanguage_FirstLinePattern"></a> FirstLinePattern

First-line detection pattern (unused; <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>).

```csharp
public string FirstLinePattern { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocLanguage_Id"></a> Id

The ColorCode language id (<code>"asciidoc"</code>).

```csharp
public string Id { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocLanguage_Name"></a> Name

The display name.

```csharp
public string Name { get; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocLanguage_Rules"></a> Rules

The ordered highlighting rules (the first to match a position wins).

```csharp
public IList<LanguageRule> Rules { get; }
```

#### Property Value

 IList<LanguageRule\>

## Methods

### <a id="Jumbee_Console_Documents_AsciiDocLanguage_HasAlias_System_String_"></a> HasAlias\(string\)

Returns <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> when <code class="paramref">lang</code> is a known alias (<code>asciidoc</code>/<code>adoc</code>/<code>asc</code>).

```csharp
public bool HasAlias(string lang)
```

#### Parameters

`lang` string

#### Returns

 bool

### <a id="Jumbee_Console_Documents_AsciiDocLanguage_ToString"></a> ToString\(\)

Returns a string that represents the current object.

```csharp
public override string ToString()
```

#### Returns

 string

A string that represents the current object.

