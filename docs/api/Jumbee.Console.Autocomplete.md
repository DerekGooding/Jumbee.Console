# <a id="Jumbee_Console_Autocomplete"></a> Class Autocomplete

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Attaches type-ahead suggestions to a <xref href="Jumbee.Console.TextInput" data-throw-if-not-resolved="false"></xref>.

```csharp
public sealed class Autocomplete
```

#### Inheritance

object ← 
[Autocomplete](Jumbee.Console.Autocomplete.md)

## Remarks

As the user types, matching candidates are shown in a <em>passive</em> popup just below the caret (via
<xref href="Jumbee.Console.Overlay.ShowPassive(Jumbee.Console.Control%2cSystem.Int32%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref>) — the field keeps focus and keeps editing. Up/Down move the highlight,
Enter/Tab accept it into the field, Escape dismisses, and the popup also closes when the field loses focus or
there are no matches. A suggestion can also be chosen by clicking.

## Constructors

### <a id="Jumbee_Console_Autocomplete__ctor_Jumbee_Console_TextInput_System_Func_System_String_System_Collections_Generic_IEnumerable_System_String___"></a> Autocomplete\(TextInput, Func<string, IEnumerable<string\>\>\)

Attaches type-ahead to <code class="paramref">input</code>, floating suggestions in the ambient
    <xref href="Jumbee.Console.UI.Overlay" data-throw-if-not-resolved="false"></xref> just below the caret.

```csharp
public Autocomplete(TextInput input, Func<string, IEnumerable<string>> suggest)
```

#### Parameters

`input` [TextInput](Jumbee.Console.TextInput.md)

`suggest` Func<string, IEnumerable<string\>\>

### <a id="Jumbee_Console_Autocomplete__ctor_Jumbee_Console_TextInput_System_String___"></a> Autocomplete\(TextInput, params string\[\]\)

Convenience: suggests from a fixed candidate list (case-insensitive substring match, prefix matches first).

```csharp
public Autocomplete(TextInput input, params string[] candidates)
```

#### Parameters

`input` [TextInput](Jumbee.Console.TextInput.md)

`candidates` string\[\]

## Properties

### <a id="Jumbee_Console_Autocomplete_MaxRows"></a> MaxRows

Maximum suggestions shown at once. Defaults to 8.

```csharp
public int MaxRows { get; set; }
```

#### Property Value

 int

## Methods

### <a id="Jumbee_Console_Autocomplete_Close"></a> Close\(\)

Closes the suggestion popup if open.

```csharp
public void Close()
```

