# <a id="Jumbee_Console_KeyHelp"></a> Class KeyHelp

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

One keystroke (or chord) and what it does, listed in a control's <xref href="Jumbee.Console.HelpInfo" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed record KeyHelp
```

#### Inheritance

object ← 
[KeyHelp](Jumbee.Console.KeyHelp.md)

## Constructors

### <a id="Jumbee_Console_KeyHelp__ctor_System_String_System_String_"></a> KeyHelp\(string, string\)

One keystroke (or chord) and what it does, listed in a control's <xref href="Jumbee.Console.HelpInfo" data-throw-if-not-resolved="false"></xref>.

```csharp
public KeyHelp(string Keys, string Description)
```

#### Parameters

`Keys` string

The key(s) as displayed, e.g. <code>"Ctrl+N"</code> or <code>"↑/↓"</code>.

`Description` string

What the key does.

## Properties

### <a id="Jumbee_Console_KeyHelp_Description"></a> Description

What the key does.

```csharp
public string Description { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_KeyHelp_Keys"></a> Keys

The key(s) as displayed, e.g. <code>"Ctrl+N"</code> or <code>"↑/↓"</code>.

```csharp
public string Keys { get; init; }
```

#### Property Value

 string

