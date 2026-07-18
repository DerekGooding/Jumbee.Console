# <a id="Jumbee_Console_IPty"></a> Interface IPty

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A pseudo-terminal session: a child process attached to a PTY, exposing its stdin/stdout as streams.

```csharp
public interface IPty
```

## Remarks

Implemented per OS — <xref href="Jumbee.Console.ConPty" data-throw-if-not-resolved="false"></xref> (Windows ConPTY) and <xref href="Jumbee.Console.UnixPty" data-throw-if-not-resolved="false"></xref> (Linux/macOS) — and created
through the <xref href="Jumbee.Console.Pty.Start(System.String%2cSystem.Int16%2cSystem.Int16%2cSystem.String)" data-throw-if-not-resolved="false"></xref> factory. All implementations are pure managed P/Invoke (no shipped native
binaries).

## Properties

### <a id="Jumbee_Console_IPty_Input"></a> Input

Write here to send input (keystrokes/bytes) to the child process.

```csharp
Stream Input { get; }
```

#### Property Value

 Stream

### <a id="Jumbee_Console_IPty_Output"></a> Output

Read here to receive the child process's terminal output (the ANSI stream).

```csharp
Stream Output { get; }
```

#### Property Value

 Stream

## Methods

### <a id="Jumbee_Console_IPty_Resize_System_Int16_System_Int16_"></a> Resize\(short, short\)

Resizes the pseudo terminal (call when the host control's cell area changes).

```csharp
void Resize(short columns, short rows)
```

#### Parameters

`columns` short

`rows` short

### <a id="Jumbee_Console_IPty_Exited"></a> Exited

Raised (off the UI thread) when the child process exits.

```csharp
event Action? Exited
```

#### Event Type

 Action?

