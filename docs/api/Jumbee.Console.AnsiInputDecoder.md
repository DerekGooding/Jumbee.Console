# <a id="Jumbee_Console_AnsiInputDecoder"></a> Class AnsiInputDecoder

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

A streaming state machine that turns a raw terminal input char stream into <xref href="Jumbee.Console.TerminalInputEvent" data-throw-if-not-resolved="false"></xref>s:
printable text, control/navigation keys (CSI/SS3), SGR (1006) mouse reports, bracketed paste (DEC 2004), and
focus in/out (DEC 1004).

```csharp
public sealed class AnsiInputDecoder
```

#### Inheritance

object ← 
[AnsiInputDecoder](Jumbee.Console.AnsiInputDecoder.md)

## Remarks

<p>
Incomplete escape sequences split across reads are buffered until the next <xref href="Jumbee.Console.AnsiInputDecoder.Feed(System.ReadOnlySpan%7bSystem.Char%7d)" data-throw-if-not-resolved="false"></xref>;
<xref href="Jumbee.Console.AnsiInputDecoder.Flush" data-throw-if-not-resolved="false"></xref> resolves a dangling <code>ESC</code> as the Escape key (the idle-timeout case).
</p>
<p>
Pure and platform-independent: a raw input source feeds it chars and forwards the events. The classic
"ESC alone vs start of a sequence" ambiguity is resolved by the caller flushing after an idle timeout.
</p>

## Methods

### <a id="Jumbee_Console_AnsiInputDecoder_Feed_System_ReadOnlySpan_System_Char__"></a> Feed\(ReadOnlySpan<char\>\)

Feeds decoded chars and returns the events that could be fully parsed.

```csharp
public IReadOnlyList<TerminalInputEvent> Feed(ReadOnlySpan<char> chars)
```

#### Parameters

`chars` ReadOnlySpan<char\>

#### Returns

 IReadOnlyList<[TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md)\>

### <a id="Jumbee_Console_AnsiInputDecoder_Flush"></a> Flush\(\)

Resolves any buffered-but-incomplete input (e.g. a lone ESC becomes <xref href="System.ConsoleKey.Escape" data-throw-if-not-resolved="false"></xref>).

```csharp
public IReadOnlyList<TerminalInputEvent> Flush()
```

#### Returns

 IReadOnlyList<[TerminalInputEvent](Jumbee.Console.TerminalInputEvent.md)\>

