# <a id="Jumbee_Console_Snapshot_AnsiConsoleSession"></a> Class AnsiConsoleSession

Namespace: [Jumbee.Console.Snapshot](Jumbee.Console.Snapshot.md)  
Assembly: Jumbee.Console.Snapshot.dll  

A stateful counterpart to <xref href="Jumbee.Console.Snapshot.AnsiConsoleSnapshot" data-throw-if-not-resolved="false"></xref> for testing the <em>live</em> render — used to
reproduce diff/cursor or async-ordering bugs that only appear across frames (e.g. press → release).

```csharp
public sealed class AnsiConsoleSession
```

#### Inheritance

object ← 
[AnsiConsoleSession](Jumbee.Console.Snapshot.AnsiConsoleSession.md)

## Remarks

It keeps <xref href="ConsoleGUI.ConsoleManager" data-throw-if-not-resolved="false"></xref> set up across multiple frames so each <xref href="Jumbee.Console.Snapshot.AnsiConsoleSession.FrameAsync" data-throw-if-not-resolved="false"></xref> emits an
incremental diff against the persistent buffer (not a fresh full frame), and folds those bytes into a
persistent <xref href="Jumbee.Console.Snapshot.AnsiConsoleSession.Screen" data-throw-if-not-resolved="false"></xref> — exactly as a real terminal accumulates them. Dispose to restore the sink.

## Properties

### <a id="Jumbee_Console_Snapshot_AnsiConsoleSession_Screen"></a> Screen

The accumulated screen after every frame folded in so far.

```csharp
public AnsiScreen Screen { get; }
```

#### Property Value

 [AnsiScreen](Jumbee.Console.Snapshot.AnsiScreen.md)

## Methods

### <a id="Jumbee_Console_Snapshot_AnsiConsoleSession_Dispose"></a> Dispose\(\)

Restores the previous <xref href="ConsoleGUI.ConsoleManager" data-throw-if-not-resolved="false"></xref> output sink and ANSI mode.

```csharp
public void Dispose()
```

### <a id="Jumbee_Console_Snapshot_AnsiConsoleSession_FrameAsync"></a> FrameAsync\(\)

Paints pending control state, draws one frame (an incremental diff), waits for the serialized
    output, and folds the emitted bytes into <xref href="Jumbee.Console.Snapshot.AnsiConsoleSession.Screen" data-throw-if-not-resolved="false"></xref>.

```csharp
public Task FrameAsync()
```

#### Returns

 Task

### <a id="Jumbee_Console_Snapshot_AnsiConsoleSession_StartAsync_ConsoleGUI_IControl_System_Int32_System_Int32_"></a> StartAsync\(IControl, int, int\)

Sets up the console for <code class="paramref">content</code> and folds in the initial frame.

```csharp
public static Task<AnsiConsoleSession> StartAsync(IControl content, int width, int height)
```

#### Parameters

`content` IControl

`width` int

`height` int

#### Returns

 Task<[AnsiConsoleSession](Jumbee.Console.Snapshot.AnsiConsoleSession.md)\>

