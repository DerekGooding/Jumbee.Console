# <a id="Jumbee_Console_HelpInfo"></a> Class HelpInfo

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A control's entry in the global help dialog (one tab). Mutable so an <xref href="Jumbee.Console.Control.OnHelp" data-throw-if-not-resolved="false"></xref> handler can
tweak it in place.

```csharp
public sealed class HelpInfo
```

#### Inheritance

object ← 
[HelpInfo](Jumbee.Console.HelpInfo.md)

## Remarks

Help is compiled by calling each control's <xref href="Jumbee.Console.Control.GetHelpInfo" data-throw-if-not-resolved="false"></xref> and deduplicated by
<xref href="Jumbee.Console.HelpInfo.Name" data-throw-if-not-resolved="false"></xref> — one tab per distinct name (e.g. all buttons share a "Button" tab), and the focused
control's tab is shown first.

## Constructors

### <a id="Jumbee_Console_HelpInfo__ctor_System_String_System_String_System_String_"></a> HelpInfo\(string, string?, string?\)

Initializes a new <xref href="Jumbee.Console.HelpInfo" data-throw-if-not-resolved="false"></xref> with the given <code class="paramref">name</code>, and optional
    <code class="paramref">title</code> (defaults to the name) and <code class="paramref">text</code>.

```csharp
public HelpInfo(string name, string? title = null, string? text = null)
```

#### Parameters

`name` string

`title` string?

`text` string?

## Properties

### <a id="Jumbee_Console_HelpInfo_Keys"></a> Keys

Key bindings shown either inline in <xref href="Jumbee.Console.HelpInfo.Text" data-throw-if-not-resolved="false"></xref> or as a separate section (see
    <xref href="Jumbee.Console.HelpInfo.KeysInline" data-throw-if-not-resolved="false"></xref>). Mutable so handlers can add entries.

```csharp
public IList<KeyHelp> Keys { get; }
```

#### Property Value

 IList<[KeyHelp](Jumbee.Console.KeyHelp.md)\>

### <a id="Jumbee_Console_HelpInfo_KeysInline"></a> KeysInline

When <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a>, the author has already woven the keys into <xref href="Jumbee.Console.HelpInfo.Text" data-throw-if-not-resolved="false"></xref>, so the
    dialog does not append a separate "Keys" section. Defaults to <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a> (separate section).

```csharp
public bool KeysInline { get; set; }
```

#### Property Value

 bool

### <a id="Jumbee_Console_HelpInfo_Name"></a> Name

Identity for deduplication and for matching the focused control's tab. Required, non-empty.

```csharp
public string Name { get; set; }
```

#### Property Value

 string

### <a id="Jumbee_Console_HelpInfo_Text"></a> Text

Spectre markup shown in the panel (e.g. <code>"[bold]Save[/] the file"</code>).

```csharp
public string Text { get; set; }
```

#### Property Value

 string

### <a id="Jumbee_Console_HelpInfo_Title"></a> Title

The tab header label. Defaults to <xref href="Jumbee.Console.HelpInfo.Name" data-throw-if-not-resolved="false"></xref>.

```csharp
public string Title { get; set; }
```

#### Property Value

 string

## Methods

### <a id="Jumbee_Console_HelpInfo_WithKey_System_String_System_String_"></a> WithKey\(string, string\)

Fluent helper to add a key binding (returns this).

```csharp
public HelpInfo WithKey(string keys, string description)
```

#### Parameters

`keys` string

`description` string

#### Returns

 [HelpInfo](Jumbee.Console.HelpInfo.md)

