# <a id="Jumbee_Console_UnixPty"></a> Class UnixPty

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A Unix pseudo terminal (Linux/macOS) session.

```csharp
public sealed class UnixPty : IPty
```

#### Inheritance

object ← 
[UnixPty](Jumbee.Console.UnixPty.md)

#### Implements

[IPty](Jumbee.Console.IPty.md)

## Remarks

Opens a pty (<code>posix_openpt</code>) and launches the child with <code>posix_spawn</code> — which fork+execs atomically in
native code, so NO managed code runs in a forked child (a raw <code>fork()</code>/<code>forkpty()</code> segfaults the .NET
runtime). Pure managed P/Invoke against libc — no shipped native binaries, mirroring <xref href="Jumbee.Console.ConPty" data-throw-if-not-resolved="false"></xref> on Windows.

<p>Compiles on any OS (P/Invoke declarations are metadata) but only runs on Linux/macOS.</p>

## Properties

### <a id="Jumbee_Console_UnixPty_Input"></a> Input

Write here to send input (keystrokes/bytes) to the child process.

```csharp
public Stream Input { get; }
```

#### Property Value

 Stream

### <a id="Jumbee_Console_UnixPty_Output"></a> Output

Read here to receive the child process's terminal output (the ANSI stream).

```csharp
public Stream Output { get; }
```

#### Property Value

 Stream

## Methods

### <a id="Jumbee_Console_UnixPty_Dispose"></a> Dispose\(\)

Sends SIGHUP to the child process and disposes the pty streams.

```csharp
public void Dispose()
```

### <a id="Jumbee_Console_UnixPty_Resize_System_Int16_System_Int16_"></a> Resize\(short, short\)

Resizes the pseudo terminal (call when the host control's cell area changes).

```csharp
public void Resize(short columns, short rows)
```

#### Parameters

`columns` short

`rows` short

### <a id="Jumbee_Console_UnixPty_Start_System_String_System_Int16_System_Int16_System_String_"></a> Start\(string, short, short, string?\)

Launches <code class="paramref">commandLine</code> in a new pty of the given size, optionally starting the
    child in <code class="paramref">workingDirectory</code> (null inherits the host process's directory).

```csharp
public static UnixPty Start(string commandLine, short columns, short rows, string? workingDirectory = null)
```

#### Parameters

`commandLine` string

`columns` short

`rows` short

`workingDirectory` string?

#### Returns

 [UnixPty](Jumbee.Console.UnixPty.md)

### <a id="Jumbee_Console_UnixPty_Exited"></a> Exited

Raised (off the UI thread) when the child process exits.

```csharp
public event Action? Exited
```

#### Event Type

 Action?

