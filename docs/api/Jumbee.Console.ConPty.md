# <a id="Jumbee_Console_ConPty"></a> Class ConPty

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A pseudo-console (ConPTY) session: launches a process attached to a Windows pseudo console and exposes its
stdin/stdout as streams.

```csharp
public sealed class ConPty : IPty
```

#### Inheritance

object ← 
[ConPty](Jumbee.Console.ConPty.md)

#### Implements

[IPty](Jumbee.Console.IPty.md)

## Remarks

Pure managed P/Invoke against the OS conhost (Windows 10 1809+) — no shipped native binaries, so it is
trim/single-file/AOT clean (unlike winpty-based wrappers).

## Properties

### <a id="Jumbee_Console_ConPty_Input"></a> Input

Write here to send input (keystrokes/bytes) to the child process.

```csharp
public Stream Input { get; }
```

#### Property Value

 Stream

### <a id="Jumbee_Console_ConPty_Output"></a> Output

Read here to receive the child process's terminal output (the ANSI stream).

```csharp
public Stream Output { get; }
```

#### Property Value

 Stream

## Methods

### <a id="Jumbee_Console_ConPty_Dispose"></a> Dispose\(\)

Closes the pseudo console (signalling the child's input EOF) and disposes its streams and process.

```csharp
public void Dispose()
```

### <a id="Jumbee_Console_ConPty_Resize_System_Int16_System_Int16_"></a> Resize\(short, short\)

Resizes the pseudo console (call when the host control's cell area changes).

```csharp
public void Resize(short columns, short rows)
```

#### Parameters

`columns` short

`rows` short

### <a id="Jumbee_Console_ConPty_Start_System_String_System_Int16_System_Int16_System_String_"></a> Start\(string, short, short, string?\)

Launches <code class="paramref">commandLine</code> in a new pseudo console of the given size, optionally in
    <code class="paramref">workingDirectory</code> (null inherits the host process's directory).

```csharp
public static ConPty Start(string commandLine, short columns, short rows, string? workingDirectory = null)
```

#### Parameters

`commandLine` string

`columns` short

`rows` short

`workingDirectory` string?

#### Returns

 [ConPty](Jumbee.Console.ConPty.md)

### <a id="Jumbee_Console_ConPty_Exited"></a> Exited

Raised (on a thread-pool thread) when the child process exits.

```csharp
public event Action? Exited
```

#### Event Type

 Action?

