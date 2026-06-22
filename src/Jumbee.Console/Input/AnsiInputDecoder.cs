namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// A streaming state machine that turns a raw terminal input char stream into <see cref="TerminalInputEvent"/>s:
/// printable text, control/navigation keys (CSI/SS3), SGR (1006) mouse reports, bracketed paste (DEC 2004), and
/// focus in/out (DEC 1004). Incomplete escape sequences split across reads are buffered until the next
/// <see cref="Feed"/>; <see cref="Flush"/> resolves a dangling <c>ESC</c> as the Escape key (the idle-timeout case).
/// </summary>
/// <remarks>
/// Pure and platform-independent: a raw input source feeds it chars and forwards the events. The classic
/// "ESC alone vs start of a sequence" ambiguity is resolved by the caller flushing after an idle timeout.
/// </remarks>
public sealed class AnsiInputDecoder
{
    #region Methods
    /// <summary>Feeds decoded chars and returns the events that could be fully parsed.</summary>
    public IReadOnlyList<TerminalInputEvent> Feed(ReadOnlySpan<char> chars)
    {
        _buffer.Append(chars);
        return Parse(flush: false);
    }

    /// <summary>Resolves any buffered-but-incomplete input (e.g. a lone ESC becomes <see cref="ConsoleKey.Escape"/>).</summary>
    public IReadOnlyList<TerminalInputEvent> Flush() => Parse(flush: true);

    private IReadOnlyList<TerminalInputEvent> Parse(bool flush)
    {
        var events = new List<TerminalInputEvent>();
        // _buffer holds only the unconsumed non-paste tail (a short incomplete escape sequence or text). A paste
        // body is diverted into _pasteBuf and scanned incrementally, so _buffer never accumulates the whole paste
        // and this per-Feed ToString stays O(new chars) rather than O(total buffered).
        var s = _buffer.ToString();
        int i = 0;
        while (i < s.Length)
        {
            if (_inPaste)
            {
                i = ConsumePaste(s, i, events); // accumulates s[i..] into _pasteBuf; emits on terminator
                continue;
            }

            char c = s[i];
            if (c == Esc)
            {
                int consumed = TryParseEscape(s, i, events);
                if (consumed == 0)
                {
                    if (flush)
                    {
                        // No more input is coming: treat the dangling ESC as the Escape key, reparse the rest.
                        events.Add(Key(ConsoleKey.Escape, Esc));
                        i++;
                        continue;
                    }
                    break; // wait for more chars to complete the sequence
                }
                i += consumed;
            }
            else
            {
                events.Add(DecodeChar(c, TerminalModifiers.None));
                i++;
            }
        }

        _buffer.Clear();
        if (i < s.Length) _buffer.Append(s, i, s.Length - i);
        return events;
    }

    // Returns the number of chars consumed, or 0 when the sequence is incomplete (keep buffering).
    private int TryParseEscape(string s, int start, List<TerminalInputEvent> events)
    {
        if (start + 1 >= s.Length) return 0; // lone ESC; resolved by Flush
        char c1 = s[start + 1];
        if (c1 == '[') return TryParseCsi(s, start, events);
        if (c1 == 'O') return TryParseSs3(s, start, events);
        if (c1 == ']') return TryConsumeOsc(s, start);            // OSC response (e.g. clipboard/colour); swallowed
        if (!char.IsControl(c1)) { events.Add(DecodeChar(c1, TerminalModifiers.Alt)); return 2; } // ESC x = Alt+x
        return 0; // ESC followed by a control char (e.g. ESC ESC); let Flush resolve the ESC
    }

    // CSI: ESC [ <params/intermediates> <final>. final byte is 0x40-0x7E.
    private int TryParseCsi(string s, int start, List<TerminalInputEvent> events)
    {
        int paramsStart = start + 2;
        int i = paramsStart;
        while (i < s.Length && s[i] >= (char)0x20 && s[i] <= (char)0x3F) i++; // param + intermediate bytes
        if (i >= s.Length) return 0; // no final byte yet

        char final = s[i];
        // A valid CSI final byte is 0x40-0x7E. Anything else means the sequence was interrupted or malformed
        // (VT500 ESC/CAN/SUB "abort anywhere" rule): drop it without emitting a bogus key. ESC must be left for
        // the outer loop to reprocess as the start of a fresh sequence rather than be consumed here.
        if (final < (char)0x40 || final > (char)0x7E)
            return final == Esc ? (i - start) : (i - start + 1);

        string p = s.Substring(paramsStart, i - paramsStart);
        int seqLen = (i - start) + 1;

        if (final == '~' && p == "200") { _inPaste = true; _pasteBuf.Clear(); _pasteScanFrom = 0; return seqLen; }
        if (final == 'I') { events.Add(new FocusInputEvent(true)); return seqLen; }
        if (final == 'O') { events.Add(new FocusInputEvent(false)); return seqLen; }
        if (p.StartsWith('<')) { events.Add(DecodeSgrMouse(p, final)); return seqLen; }

        events.Add(DecodeCsiKey(p, final));
        return seqLen;
    }

    // SS3: ESC O <final> (application-mode cursor/function keys).
    private int TryParseSs3(string s, int start, List<TerminalInputEvent> events)
    {
        if (start + 2 >= s.Length) return 0;
        var key = s[start + 2] switch
        {
            'P' => ConsoleKey.F1,
            'Q' => ConsoleKey.F2,
            'R' => ConsoleKey.F3,
            'S' => ConsoleKey.F4,
            'A' => ConsoleKey.UpArrow,
            'B' => ConsoleKey.DownArrow,
            'C' => ConsoleKey.RightArrow,
            'D' => ConsoleKey.LeftArrow,
            'H' => ConsoleKey.Home,
            'F' => ConsoleKey.End,
            _ => (ConsoleKey)0,
        };
        events.Add(Key(key, '\0', TerminalModifiers.None));
        return 3;
    }

    // OSC string: ESC ] ... terminated by BEL (0x07) or ST (ESC \). We don't yet consume OSC responses
    // (clipboard/colour queries), so swallow the whole sequence rather than misdecode ESC ] as Alt+].
    // TODO: when clipboard-get / colour probes land, surface specific OSC responses as events instead.
    private static int TryConsumeOsc(string s, int start)
    {
        for (int i = start + 2; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '\x07') return (i + 1) - start;                      // BEL terminator
            if (c == Esc)
            {
                if (i + 1 >= s.Length) return 0;                          // ESC at tail: wait for the next char
                if (s[i + 1] == '\\') return (i + 2) - start;             // ST terminator (ESC \)
                return i - start;                                         // malformed: end OSC, reparse from this ESC
            }
        }
        return 0; // no terminator yet
    }

    // In paste mode: append s[from..] to _pasteBuf and scan only the new region (plus a small overlap, so a
    // terminator split across Feeds is still found) for ESC[201~. Returns the index in s to resume normal parsing
    // (== s.Length while still pasting). Cumulative cost is O(total paste length), not O(n^2).
    private int ConsumePaste(string s, int from, List<TerminalInputEvent> events)
    {
        _pasteBuf.Append(s, from, s.Length - from);
        int end = IndexOfPasteTerminator(_pasteBuf, _pasteScanFrom);
        if (end < 0)
        {
            // Re-scan only the trailing PasteEnd.Length-1 chars next time (catches a split terminator).
            _pasteScanFrom = Math.Max(_pasteScanFrom, _pasteBuf.Length - (PasteEnd.Length - 1));
            return s.Length; // all of s consumed into the paste body
        }

        events.Add(new PasteInputEvent(_pasteBuf.ToString(0, end)));
        int leftover = _pasteBuf.Length - (end + PasteEnd.Length); // post-terminator chars (all from this Feed)
        _inPaste = false;
        _pasteBuf.Clear();
        _pasteScanFrom = 0;
        return s.Length - leftover;
    }

    private static int IndexOfPasteTerminator(StringBuilder sb, int from)
    {
        for (int i = Math.Max(0, from); i + PasteEnd.Length <= sb.Length; i++)
        {
            if (sb[i] == PasteEnd[0] && sb[i + 1] == PasteEnd[1] && sb[i + 2] == PasteEnd[2]
                && sb[i + 3] == PasteEnd[3] && sb[i + 4] == PasteEnd[4] && sb[i + 5] == PasteEnd[5])
                return i;
        }
        return -1;
    }

    // SGR 1006 mouse: ESC [ < b ; x ; y (M=press/motion, m=release).
    private static MouseInputEvent DecodeSgrMouse(string p, char final)
    {
        var parts = p.Substring(1).Split(';');
        int b = ParseInt(parts[0]);
        int x = ParseInt(parts[1]) - 1;
        int y = ParseInt(parts[2]) - 1;

        var mods = TerminalModifiers.None;
        if ((b & 4) != 0) mods |= TerminalModifiers.Shift;
        if ((b & 8) != 0) mods |= TerminalModifiers.Alt;
        if ((b & 16) != 0) mods |= TerminalModifiers.Control;

        bool wheel = (b & 64) != 0;
        bool motion = (b & 32) != 0;
        int low = b & 3;

        TerminalMouseButton button;
        TerminalMouseKind kind;
        if (wheel)
        {
            button = low == 0 ? TerminalMouseButton.WheelUp : TerminalMouseButton.WheelDown;
            kind = TerminalMouseKind.Wheel;
        }
        else
        {
            button = low switch
            {
                0 => TerminalMouseButton.Left,
                1 => TerminalMouseButton.Middle,
                2 => TerminalMouseButton.Right,
                _ => TerminalMouseButton.None,
            };
            kind = motion
                ? (button == TerminalMouseButton.None ? TerminalMouseKind.Move : TerminalMouseKind.Drag)
                : (final == 'M' ? TerminalMouseKind.Down : TerminalMouseKind.Up);
        }

        return new MouseInputEvent(x, y, button, kind, mods);
    }

    private static KeyInputEvent DecodeCsiKey(string p, char final)
    {
        var parts = p.Split(';');
        var mods = parts.Length > 1 ? DecodeModParam(parts[1]) : TerminalModifiers.None;

        if (final == '~')
        {
            int code = parts.Length > 0 ? ParseInt(parts[0]) : 0;
            var tildeKey = code switch
            {
                1 or 7 => ConsoleKey.Home,
                2 => ConsoleKey.Insert,
                3 => ConsoleKey.Delete,
                4 or 8 => ConsoleKey.End,
                5 => ConsoleKey.PageUp,
                6 => ConsoleKey.PageDown,
                11 => ConsoleKey.F1,
                12 => ConsoleKey.F2,
                13 => ConsoleKey.F3,
                14 => ConsoleKey.F4,
                15 => ConsoleKey.F5,
                17 => ConsoleKey.F6,
                18 => ConsoleKey.F7,
                19 => ConsoleKey.F8,
                20 => ConsoleKey.F9,
                21 => ConsoleKey.F10,
                23 => ConsoleKey.F11,
                24 => ConsoleKey.F12,
                _ => (ConsoleKey)0,
            };
            return Key(tildeKey, '\0', mods);
        }

        var key = final switch
        {
            'A' => ConsoleKey.UpArrow,
            'B' => ConsoleKey.DownArrow,
            'C' => ConsoleKey.RightArrow,
            'D' => ConsoleKey.LeftArrow,
            'H' => ConsoleKey.Home,
            'F' => ConsoleKey.End,
            'P' => ConsoleKey.F1,
            'Q' => ConsoleKey.F2,
            'R' => ConsoleKey.F3,
            'S' => ConsoleKey.F4,
            'Z' => ConsoleKey.Tab,            // CSI Z = back-tab
            _ => (ConsoleKey)0,
        };
        if (final == 'Z') mods |= TerminalModifiers.Shift;
        return Key(key, '\0', mods);
    }

    // Terminal modifier param is (bitmask + 1): bit0 Shift, bit1 Alt, bit2 Control.
    private static TerminalModifiers DecodeModParam(string s)
    {
        int m = ParseInt(s) - 1;
        if (m <= 0) return TerminalModifiers.None;
        var mods = TerminalModifiers.None;
        if ((m & 1) != 0) mods |= TerminalModifiers.Shift;
        if ((m & 2) != 0) mods |= TerminalModifiers.Alt;
        if ((m & 4) != 0) mods |= TerminalModifiers.Control;
        return mods;
    }

    private static KeyInputEvent DecodeChar(char c, TerminalModifiers mods)
    {
        switch (c)
        {
            case '\r':
            case '\n': return Key(ConsoleKey.Enter, c, mods);
            case '\t': return Key(ConsoleKey.Tab, c, mods);
            case '\b':
            case '\x7f': return Key(ConsoleKey.Backspace, c, mods);
            case Esc: return Key(ConsoleKey.Escape, c, mods);
        }

        // C0 control: Ctrl+letter (e.g. 0x03 -> Ctrl+C).
        if (c >= 1 && c <= 26)
            return Key(ConsoleKey.A + (c - 1), c, mods | TerminalModifiers.Control);

        var key = c switch
        {
            >= 'a' and <= 'z' => ConsoleKey.A + (c - 'a'),
            >= 'A' and <= 'Z' => ConsoleKey.A + (c - 'A'),
            >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
            ' ' => ConsoleKey.Spacebar,
            _ => (ConsoleKey)0,
        };
        if (c >= 'A' && c <= 'Z') mods |= TerminalModifiers.Shift;
        return Key(key, c, mods);
    }

    private static KeyInputEvent Key(ConsoleKey key, char keyChar, TerminalModifiers mods = TerminalModifiers.None)
        => new(key, keyChar, mods);

    private static int ParseInt(string s)
        => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
    #endregion

    #region Fields
    private const char Esc = '\x1b';
    private const string PasteEnd = "\x1b[201~";   // bracketed-paste terminator
    private readonly StringBuilder _buffer = new();

    // Bracketed-paste accumulation: the body lives here (not in _buffer) and is scanned incrementally from
    // _pasteScanFrom so a paste arriving in many chunks stays O(n) overall.
    private bool _inPaste;
    private readonly StringBuilder _pasteBuf = new();
    private int _pasteScanFrom;
    #endregion
}
