# <a id="Jumbee_Console_UI_HotKeys"></a> Class UI.HotKeys

Namespace: [Jumbee.Console](Jumbee.Console.md)  
Assembly: Jumbee.Console.dll  

Factory helpers and well-known <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref> constants for registering hotkeys.

```csharp
public static class UI.HotKeys
```

#### Inheritance

object ← 
[UI.HotKeys](Jumbee.Console.UI.HotKeys.md)

## Fields

### <a id="Jumbee_Console_UI_HotKeys_AltDown"></a> AltDown

Alt+Down — Alt-tier layout navigation.

```csharp
public static ConsoleKeyInfo AltDown
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_AltLeft"></a> AltLeft

Alt+Left — Alt-tier layout navigation.

```csharp
public static ConsoleKeyInfo AltLeft
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_AltRight"></a> AltRight

Alt+Right — Alt-tier layout navigation.

```csharp
public static ConsoleKeyInfo AltRight
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_AltUp"></a> AltUp

Alt+Up — Alt-tier layout navigation.

```csharp
public static ConsoleKeyInfo AltUp
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlAltUp"></a> CtrlAltUp

Ctrl+Alt+Up.

```csharp
public static ConsoleKeyInfo CtrlAltUp
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlDown"></a> CtrlDown

Ctrl+Down — moves focus one region down.

```csharp
public static ConsoleKeyInfo CtrlDown
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlF12"></a> CtrlF12

Ctrl+F12.

```csharp
public static ConsoleKeyInfo CtrlF12
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlLeft"></a> CtrlLeft

Ctrl+Left — moves focus one region left.

```csharp
public static ConsoleKeyInfo CtrlLeft
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlN"></a> CtrlN

Ctrl+N — cycles focus to the next control within the region.

```csharp
public static ConsoleKeyInfo CtrlN
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlP"></a> CtrlP

Ctrl+P — cycles focus to the previous control within the region.

```csharp
public static ConsoleKeyInfo CtrlP
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlQ"></a> CtrlQ

Ctrl+Q — the default quit hotkey.

```csharp
public static ConsoleKeyInfo CtrlQ
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlRight"></a> CtrlRight

Ctrl+Right — moves focus one region right.

```csharp
public static ConsoleKeyInfo CtrlRight
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlUp"></a> CtrlUp

Ctrl+Up — moves focus one region up.

```csharp
public static ConsoleKeyInfo CtrlUp
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_Escape"></a> Escape

The Escape key, as produced by the input decoder (KeyChar <code>\x1b</code>, no modifiers).

```csharp
public static ConsoleKeyInfo Escape
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_F1"></a> F1

The F1 key, as produced by the input decoder (SS3 <code>ESC O P</code> → KeyChar <code>\0</code>, no modifiers).

```csharp
public static ConsoleKeyInfo F1
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_ShiftTab"></a> ShiftTab

Shift+Tab (back-tab), as produced by the input decoder from CSI Z (KeyChar <code>\0</code>, Shift).

```csharp
public static ConsoleKeyInfo ShiftTab
```

#### Field Value

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_Tab"></a> Tab

The Tab key, as produced by the input decoder (KeyChar <code>\t</code>, no modifiers).

```csharp
public static ConsoleKeyInfo Tab
```

#### Field Value

 ConsoleKeyInfo

## Methods

### <a id="Jumbee_Console_UI_HotKeys_Alt_System_ConsoleKey_"></a> Alt\(ConsoleKey\)

Builds a <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref> for <code class="paramref">key</code> with the Alt modifier.

```csharp
public static ConsoleKeyInfo Alt(ConsoleKey key)
```

#### Parameters

`key` ConsoleKey

#### Returns

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_Char_System_Char_"></a> Char\(char\)

Builds a <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref> for a bare printable key — a letter, digit, punctuation,
    or space — so it can be registered as a global hotkey (e.g. <code>UI.RegisterHotKey(UI.HotKeys.Char('q'),
    UI.Stop)</code>).

```csharp
public static ConsoleKeyInfo Char(char c)
```

#### Parameters

`c` char

#### Returns

 ConsoleKeyInfo

#### Remarks

Produces exactly what the input decoder emits for that keystroke, so the registered hotkey
    matches a real press: an uppercase letter carries Shift; every non-letter/digit (e.g. <code>'/'</code>) uses
    key code <code>0</code> with the character. The same value drives a headless test — pass
    <code>UI.HotKeys.Char(c)</code> to the Snapshot package's <code>ToTextAfter(..., routeGlobal: true)</code>.

### <a id="Jumbee_Console_UI_HotKeys_Ctrl_System_ConsoleKey_"></a> Ctrl\(ConsoleKey\)

Builds a <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref> for <code class="paramref">key</code> with the Ctrl modifier.

```csharp
public static ConsoleKeyInfo Ctrl(ConsoleKey key)
```

#### Parameters

`key` ConsoleKey

#### Returns

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_CtrlAlt_System_ConsoleKey_"></a> CtrlAlt\(ConsoleKey\)

Builds a <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref> for <code class="paramref">key</code> with both Ctrl and Alt modifiers.

```csharp
public static ConsoleKeyInfo CtrlAlt(ConsoleKey key)
```

#### Parameters

`key` ConsoleKey

#### Returns

 ConsoleKeyInfo

### <a id="Jumbee_Console_UI_HotKeys_Shift_System_ConsoleKey_"></a> Shift\(ConsoleKey\)

Builds a <xref href="System.ConsoleKeyInfo" data-throw-if-not-resolved="false"></xref> for <code class="paramref">key</code> with the Shift modifier — for
    Shift+arrow / Shift+PageUp and similar non-letter combos (letter keys carry their uppercase char, matching
    the input decoder). For a Shift+letter as a printable character, prefer <xref href="Jumbee.Console.UI.HotKeys.Char(System.Char)" data-throw-if-not-resolved="false"></xref>.

```csharp
public static ConsoleKeyInfo Shift(ConsoleKey key)
```

#### Parameters

`key` ConsoleKey

#### Returns

 ConsoleKeyInfo

