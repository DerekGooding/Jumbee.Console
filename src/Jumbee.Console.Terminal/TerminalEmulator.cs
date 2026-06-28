namespace Jumbee.Console.Terminal;

using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using VtNetCore.VirtualTerminal;
using VtNetCore.XTermParser;

/// <summary>
/// A control that runs a child process in a pseudo-console (<see cref="ConPty"/>), parses its ANSI output with
/// VtNetCore, and paints the emulated screen into the control's cell area. Input routed to the focused control is
/// translated to terminal bytes and sent to the process.
/// </summary>
public class TerminalEmulator : Control
{
    #region Constructors
    public TerminalEmulator(string commandLine = "cmd.exe")
    {
        _commandLine = commandLine;
        _terminal = new VirtualTerminalController();
        _consumer = new DataConsumer(_terminal);
        _terminal.SendData += OnTerminalSendData;   // terminal responses (DSR/DA/…) → back to the process
    }
    #endregion

    #region Properties
    public override bool HandlesInput => true;
    #endregion

    #region Methods
    // Start (or re-size) the PTY once the control has a real cell area. Runs on the UI thread.
    protected override void Control_OnInitialization()
    {
        var cols = (short)Math.Max(1, ActualWidth);
        var rows = (short)Math.Max(1, ActualHeight);
        if (cols <= 1 && rows <= 1) return;

        _terminal.ResizeView(ActualWidth, ActualHeight);

        if (_pty is null)
        {
            _pty = ConPty.Start(_commandLine, cols, rows);
            _pty.Exited += () => UI.Post(Invalidate);
            _cts = new CancellationTokenSource();
            _ = ReadLoopAsync(_pty, _cts.Token);
        }
        else
        {
            _pty.Resize(cols, rows);
        }
        Invalidate();
    }

    // Drains the child's output on a background thread and marshals each chunk onto the UI thread, where the
    // emulator is mutated and the control repainted (VtNetCore, like all UI state, is single-threaded here).
    private async Task ReadLoopAsync(ConPty pty, CancellationToken ct)
    {
        var buffer = new byte[4096];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var n = await pty.Output.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false);
                if (n <= 0) break;
                var chunk = buffer[..n];
                UI.Post(() =>
                {
                    _consumer.Push(chunk);
                    Invalidate();
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* pipe closed on exit */ }
    }

    private void OnTerminalSendData(object? sender, SendDataEventArgs e) => WriteToProcess(e.Data);

    private void WriteToProcess(byte[] data)
    {
        var pty = _pty;
        if (pty is null || data is null || data.Length == 0) return;
        try { pty.Input.Write(data, 0, data.Length); pty.Input.Flush(); }
        catch (Exception) { }
    }

    protected override void OnInput(InputEvent inputEvent)
    {
        var bytes = KeyToBytes(inputEvent.Key);
        if (bytes is { Length: > 0 })
        {
            WriteToProcess(bytes);
            inputEvent.Handled = true;
        }
    }

    public override void OnPaste(string text) => WriteToProcess(Encoding.UTF8.GetBytes(text));

    // Map a key event to the bytes a terminal application expects. A first-pass subset — printable text, the common
    // editing keys, arrows, and Ctrl+letter — enough to drive a shell; app-cursor-mode/keypad/F-keys come later.
    private static byte[]? KeyToBytes(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter: return [(byte)'\r'];
            case ConsoleKey.Backspace: return [0x7f];
            case ConsoleKey.Tab: return [(byte)'\t'];
            case ConsoleKey.Escape: return [0x1b];
            case ConsoleKey.UpArrow: return Esc("[A");
            case ConsoleKey.DownArrow: return Esc("[B");
            case ConsoleKey.RightArrow: return Esc("[C");
            case ConsoleKey.LeftArrow: return Esc("[D");
            case ConsoleKey.Home: return Esc("[H");
            case ConsoleKey.End: return Esc("[F");
            case ConsoleKey.Delete: return Esc("[3~");
        }

        if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.KeyChar == '\0'
            && key.Key is >= ConsoleKey.A and <= ConsoleKey.Z)
            return [(byte)(key.Key - ConsoleKey.A + 1)];   // Ctrl+A..Z → 0x01..0x1A

        return key.KeyChar != '\0' ? Encoding.UTF8.GetBytes([key.KeyChar]) : null;
    }

    private static byte[] Esc(string tail)
    {
        var bytes = new byte[tail.Length + 1];
        bytes[0] = 0x1b;
        for (var i = 0; i < tail.Length; i++) bytes[i + 1] = (byte)tail[i];
        return bytes;
    }

    protected override void Render()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        // Blank the area first (the emulator only describes occupied spans).
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                consoleBuffer.Write(new Position(x, y), Blank);

        try
        {
            // GetPageSpans takes an absolute buffer line; ViewPort.TopRow is the top of the visible page.
            var rows = _terminal.ViewPort.GetPageSpans(_terminal.ViewPort.TopRow, height);
            for (var y = 0; y < rows.Count && y < height; y++)
            {
                var x = 0;
                foreach (var span in rows[y].Spans)
                {
                    var fg = ParseWebColor(span.ForgroundColor);
                    var bg = ParseWebColor(span.BackgroundColor);
                    var deco = Decoration.None;
                    if (span.Bold) deco |= Decoration.Bold;
                    if (span.Underline) deco |= Decoration.Underline;
                    if (span.Blink) deco |= Decoration.SlowBlink;
                    if (span.Hidden) deco |= Decoration.Conceal;

                    foreach (var ch in span.Text)
                    {
                        if (x >= width) break;
                        consoleBuffer.Write(new Position(x, y), new Character(ch, fg, bg, deco));
                        x++;
                    }
                }
            }

            // The terminal cursor (CursorState row/col are already viewport-relative); only the focused control
            // owns the real cursor (Character.IsCursor).
            if (IsFocused)
            {
                var cx = _terminal.CursorState.CurrentColumn;
                var cy = _terminal.CursorState.CurrentRow;
                if (cx >= 0 && cx < width && cy >= 0 && cy < height)
                {
                    var cell = consoleBuffer[cx, cy].Character;
                    consoleBuffer.Write(new Position(cx, cy),
                        new Character(cell.Content ?? ' ', cell.Foreground, cell.Background, cell.Decoration, isCursor: true));
                }
            }
        }
        catch (Exception) { /* tolerate transient emulator state during resize */ }
    }

    private static Color? ParseWebColor(string? web)
    {
        if (string.IsNullOrEmpty(web) || web[0] != '#' || web.Length < 7) return null;
        return byte.TryParse(web.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
            && byte.TryParse(web.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
            && byte.TryParse(web.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)
            ? new Color(r, g, b)
            : null;
    }

    public override void Dispose()
    {
        _cts?.Cancel();
        _pty?.Dispose();
        _pty = null;
        base.Dispose();
    }
    #endregion

    #region Fields
    private static readonly Character Blank = new(' ');
    private readonly string _commandLine;
    private readonly VirtualTerminalController _terminal;
    private readonly DataConsumer _consumer;
    private ConPty? _pty;
    private CancellationTokenSource? _cts;
    #endregion
}
