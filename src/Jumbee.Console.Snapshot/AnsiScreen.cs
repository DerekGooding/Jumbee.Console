namespace Jumbee.Console.Snapshot;

using System;

using ConsoleGUI.Data;
using ConsoleGUI.Space;

using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// A small VT/ANSI screen model that parses the subset of escape sequences <c>ConsoleManager</c> emits and
/// maintains a cell grid, exactly as a terminal would.
/// </summary>
/// <remarks>
/// <para>
/// The parsed subset covers CUP cursor moves, 24-bit SGR foreground/background (and default 39/49), the SGR
/// decorations, cursor visibility (DECTCEM <c>?25</c>), and OSC strings. Feed it the bytes captured from
/// <c>ConsoleManager.AnsiOutput</c> and read the resulting <see cref="Buffer"/>.
/// </para>
/// <para>
/// It is deliberately a <em>faithful</em> emulator of the emitted subset (SGR attributes accumulate; <c>ESC[m</c>
/// / <c>ESC[0m</c> resets foreground, background, and decorations), so an encoding bug in the render path — a
/// colour that should have been re-emitted but wasn't, a cell that wasn't erased — shows up as a wrong cell here.
/// </para>
/// </remarks>
public sealed class AnsiScreen
{
    #region Constructors
    /// <summary>Initializes a new <see cref="AnsiScreen"/> with a blank <paramref name="width"/>×<paramref name="height"/> cell grid.</summary>
    public AnsiScreen(int width, int height)
    {
        _width = width;
        _height = height;
        Buffer = new ConsoleBuffer { Size = new Size(width, height) };
        Buffer.Initialize();   // blank (space) cells
    }
    #endregion

    #region Properties
    /// <summary>The parsed screen image. Reuse <see cref="ConsoleSnapshot.ToText(ConsoleBuffer)"/> to read it.</summary>
    public ConsoleBuffer Buffer { get; }

    /// <summary>Whether the terminal cursor is currently shown (last DECTCEM <c>?25h</c>/<c>?25l</c>).</summary>
    public bool CursorVisible { get; private set; } = true;

    /// <summary>The cursor position (0-based) as last set by a CUP move.</summary>
    public Position CursorPosition => new(_col, _row);
    #endregion

    #region Methods
    /// <summary>Parses <paramref name="ansi"/> and applies it to the screen (cumulative across calls).</summary>
    public void Feed(string ansi)
    {
        var i = 0;
        while (i < ansi.Length)
        {
            var c = ansi[i];
            switch (c)
            {
                case Esc: i = ParseEscape(ansi, i); break;
                case '\n': _row++; _col = 0; i++; break;
                case '\r': _col = 0; i++; break;
                default: Put(c); i++; break;
            }
        }
    }

    private int ParseEscape(string s, int i)
    {
        // s[i] == ESC
        if (i + 1 >= s.Length) return i + 1;
        return s[i + 1] switch
        {
            '[' => ParseCsi(s, i + 2),
            ']' => SkipOsc(s, i + 2),
            _ => i + 2,   // ESC\ (ST) or another two-byte escape we don't model
        };
    }

    private int ParseCsi(string s, int i)
    {
        // Collect parameter + intermediate bytes up to the final byte (0x40..0x7E).
        var start = i;
        while (i < s.Length && (s[i] < '\x40' || s[i] > '\x7e')) i++;
        if (i >= s.Length) return i;
        ApplyCsi(s.Substring(start, i - start), s[i]);
        return i + 1;
    }

    private static int SkipOsc(string s, int i)
    {
        // OSC terminated by BEL or ST (ESC\).
        while (i < s.Length)
        {
            if (s[i] == '\x07') return i + 1;
            if (s[i] == Esc && i + 1 < s.Length && s[i + 1] == '\\') return i + 2;
            i++;
        }
        return i;
    }

    private void ApplyCsi(string param, char final)
    {
        switch (final)
        {
            case 'H' or 'f':   // CUP: line;column, 1-based
                var semi = param.IndexOf(';');
                var line = semi < 0 ? ParseInt(param, 1) : ParseInt(param[..semi], 1);
                var column = semi < 0 ? 1 : ParseInt(param[(semi + 1)..], 1);
                _row = Math.Max(0, line - 1);
                _col = Math.Max(0, column - 1);
                break;
            case 'm':
                ApplySgr(param);
                break;
            case 'h' or 'l':
                if (param == "?25") CursorVisible = final == 'h';
                break;
            // DECSCUSR ('q') and anything else: not modelled.
        }
    }

    private void ApplySgr(string param)
    {
        if (param.Length == 0) { ResetPen(); return; }   // ESC[m == ESC[0m

        var parts = param.Split(';');
        for (var k = 0; k < parts.Length; k++)
        {
            switch (ParseInt(parts[k], 0))
            {
                case 0: ResetPen(); break;
                case 1: _deco |= Decoration.Bold; break;
                case 2: _deco |= Decoration.Dim; break;
                case 3: _deco |= Decoration.Italic; break;
                case 4: _deco |= Decoration.Underline; break;
                case 5: _deco |= Decoration.SlowBlink; break;
                case 6: _deco |= Decoration.RapidBlink; break;
                case 7: _deco |= Decoration.Invert; break;
                case 8: _deco |= Decoration.Conceal; break;
                case 9: _deco |= Decoration.Strikethrough; break;
                case 22: _deco &= ~(Decoration.Bold | Decoration.Dim); break;
                case 23: _deco &= ~Decoration.Italic; break;
                case 24: _deco &= ~Decoration.Underline; break;
                case 25: _deco &= ~(Decoration.SlowBlink | Decoration.RapidBlink); break;
                case 27: _deco &= ~Decoration.Invert; break;
                case 28: _deco &= ~Decoration.Conceal; break;
                case 29: _deco &= ~Decoration.Strikethrough; break;
                case 39: _fg = null; break;
                case 49: _bg = null; break;
                case 38 when k + 4 < parts.Length && ParseInt(parts[k + 1], -1) == 2:
                    _fg = new CColor((byte)ParseInt(parts[k + 2], 0), (byte)ParseInt(parts[k + 3], 0), (byte)ParseInt(parts[k + 4], 0));
                    k += 4;
                    break;
                case 48 when k + 4 < parts.Length && ParseInt(parts[k + 1], -1) == 2:
                    _bg = new CColor((byte)ParseInt(parts[k + 2], 0), (byte)ParseInt(parts[k + 3], 0), (byte)ParseInt(parts[k + 4], 0));
                    k += 4;
                    break;
            }
        }
    }

    private void Put(char c)
    {
        if (_row >= 0 && _row < _height && _col >= 0 && _col < _width)
            Buffer.Write(new Position(_col, _row), new Character(c, _fg, _bg, _deco));
        _col++;
    }

    private void ResetPen()
    {
        _fg = null;
        _bg = null;
        _deco = Decoration.None;
    }

    private static int ParseInt(string s, int fallback) =>
        int.TryParse(s, out var v) ? v : fallback;
    #endregion

    #region Fields
    private const char Esc = '\x1b';
    private readonly int _width;
    private readonly int _height;
    private int _row;
    private int _col;
    private CColor? _fg;
    private CColor? _bg;
    private Decoration _deco = Decoration.None;
    #endregion
}
