# <a id="Jumbee_Console_Documents_AsciiDocStyles"></a> Class AsciiDocStyles

Namespace: [Jumbee.Console.Documents](Jumbee.Console.Documents.md)  
Assembly: Jumbee.Console.Documents.dll  

Visual styling for <xref href="Jumbee.Console.Documents.AsciiDocViewer" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class AsciiDocStyles
```

#### Inheritance

object ← 
[AsciiDocStyles](Jumbee.Console.Documents.AsciiDocStyles.md)

## Remarks

Each value is a Spectre.Console markup style string
(e.g. <code>"bold blue"</code>) applied to the console renderables emitted while traversing the AsciiDoc AST.

## Properties

### <a id="Jumbee_Console_Documents_AsciiDocStyles_AdmonitionCaution"></a> AdmonitionCaution

Accent (border) colour of CAUTION admonitions.

```csharp
public string AdmonitionCaution { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_AdmonitionImportant"></a> AdmonitionImportant

Accent (border) colour of IMPORTANT admonitions.

```csharp
public string AdmonitionImportant { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_AdmonitionNote"></a> AdmonitionNote

Accent (border) colours for the five admonition kinds.

```csharp
public string AdmonitionNote { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_AdmonitionTip"></a> AdmonitionTip

Accent (border) colour of TIP admonitions.

```csharp
public string AdmonitionTip { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_AdmonitionWarning"></a> AdmonitionWarning

Accent (border) colour of WARNING admonitions.

```csharp
public string AdmonitionWarning { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Attribution"></a> Attribution

Quote attribution (author / source) line.

```csharp
public string Attribution { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Code"></a> Code

Foreground of verbatim source / listing / literal block text.

```csharp
public string Code { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_CrossReference"></a> CrossReference

Cross-reference (<code>&lt;&lt;ref&gt;&gt;</code>) text.

```csharp
public string CrossReference { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Default"></a> Default

A shared default style set.

```csharp
public static AsciiDocStyles Default { get; }
```

#### Property Value

 [AsciiDocStyles](Jumbee.Console.Documents.AsciiDocStyles.md)

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Emphasis"></a> Emphasis

Emphasised (<code>_italic_</code>) inline text.

```csharp
public string Emphasis { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Headings"></a> Headings

Section heading styles indexed by level (1 = <code>==</code>, 2 = <code>===</code>, …). Index 0 is unused.

```csharp
public string[] Headings { get; init; }
```

#### Property Value

 string\[\]

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Highlight"></a> Highlight

Highlighted (<code>#mark#</code>) inline text.

```csharp
public string Highlight { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Link"></a> Link

Hyperlink text.

```csharp
public string Link { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_ListMarker"></a> ListMarker

List bullet / ordered-list markers.

```csharp
public string ListMarker { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Mermaid"></a> Mermaid

Colours/scale for embedded <code>[source,mermaid]</code> diagram blocks.

```csharp
public MermaidStyles Mermaid { get; init; }
```

#### Property Value

 [MermaidStyles](Jumbee.Console.Documents.MermaidStyles.md)

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Monospace"></a> Monospace

Inline monospace (<code>`code`</code>) text.

```csharp
public string Monospace { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_PanelBorder"></a> PanelBorder

Border colour of code / example / sidebar panels and thematic breaks.

```csharp
public string PanelBorder { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Quote"></a> Quote

Quote / verse block body text.

```csharp
public string Quote { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Strong"></a> Strong

Bold (<code>*strong*</code>) inline text.

```csharp
public string Strong { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_Documents_AsciiDocStyles_Title"></a> Title

The document title (<code>= Title</code> header), rendered as a horizontal rule.

```csharp
public string Title { get; init; }
```

#### Property Value

 string

