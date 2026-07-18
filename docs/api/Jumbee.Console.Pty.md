# <a id="Jumbee_Console_Pty"></a> Class Pty

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Factory that opens an <xref href="Jumbee.Console.IPty" data-throw-if-not-resolved="false"></xref> using the right backend for the current OS: the Windows pseudo console
(<xref href="Jumbee.Console.ConPty" data-throw-if-not-resolved="false"></xref>) or a Unix pseudo terminal (<xref href="Jumbee.Console.UnixPty" data-throw-if-not-resolved="false"></xref>).

```csharp
public static class Pty
```

#### Inheritance

object ← 
[Pty](Jumbee.Console.Pty.md)

## Properties

### <a id="Jumbee_Console_Pty_DefaultShell"></a> DefaultShell

The OS default interactive shell: <code>cmd.exe</code> on Windows, <code>$SHELL</code> (or <code>/bin/bash</code>)
    on Unix. Handy as the command line for <xref href="Jumbee.Console.TerminalEmulator" data-throw-if-not-resolved="false"></xref> on a non-Windows host.

```csharp
public static string DefaultShell { get; }
```

#### Property Value

 string

## Methods

### <a id="Jumbee_Console_Pty_Start_System_String_System_Int16_System_Int16_System_String_"></a> Start\(string, short, short, string?\)

Launches <code class="paramref">commandLine</code> in a new pseudo terminal of the given size. When
    <code class="paramref">workingDirectory</code> is non-null the child starts in that directory; otherwise it inherits the
    host process's current directory.

```csharp
public static IPty Start(string commandLine, short columns, short rows, string? workingDirectory = null)
```

#### Parameters

`commandLine` string

`columns` short

`rows` short

`workingDirectory` string?

#### Returns

 [IPty](Jumbee.Console.IPty.md)

