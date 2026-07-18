# <a id="Jumbee_Console_FooterHint"></a> Struct FooterHint

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A single key-binding hint shown in a <xref href="Jumbee.Console.Footer" data-throw-if-not-resolved="false"></xref>: the key chord and what it does.

```csharp
public readonly record struct FooterHint
```

## Constructors

### <a id="Jumbee_Console_FooterHint__ctor_System_String_System_String_"></a> FooterHint\(string, string\)

A single key-binding hint shown in a <xref href="Jumbee.Console.Footer" data-throw-if-not-resolved="false"></xref>: the key chord and what it does.

```csharp
public FooterHint(string Key, string Label)
```

#### Parameters

`Key` string

`Label` string

## Properties

### <a id="Jumbee_Console_FooterHint_Key"></a> Key

```csharp
public string Key { get; init; }
```

#### Property Value

 string

### <a id="Jumbee_Console_FooterHint_Label"></a> Label

```csharp
public string Label { get; init; }
```

#### Property Value

 string

