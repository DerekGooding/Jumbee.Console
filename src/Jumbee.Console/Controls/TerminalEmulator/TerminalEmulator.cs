namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ConsoleGUI.Data;
using ConsoleGUI.Input;
using ConsoleGUI.Space;

using VtNetCore.VirtualTerminal;
using VtNetCore.VirtualTerminal.Enums;
using VtNetCore.XTermParser;

// In the Jumbee.Console namespace, unqualified Color is the Styles Color struct; the emulator paints ConsoleGUI
// cells, so alias the cell colour type (the alias can't be named Color — that clashes with the namespace member).
using CColor = ConsoleGUI.Data.Color;

/// <summary>
/// A control that runs a child process in a pseudo-console (<see cref="ConPty"/>), parses its ANSI output with
/// VtNetCore, and paints the emulated screen into the control's cell area. Input routed to the focused control is
/// translated to terminal bytes and sent to the process.
/// </summary>
public class TerminalEmulator : Control
{
    #region Constructors
    /// <summary>
    /// Creates a terminal. With a <paramref name="commandLine"/> the control launches that process in a pseudo
    /// console once it is sized. Pass <see langword="null"/> (or whitespace) to skip spawning and drive the
    /// emulator manually by pushing bytes through <see cref="Feed"/> — useful for embedding an existing stream
    /// (e.g. an SSH channel) or for headless tests.
    /// <para><paramref name="workingDirectory"/> sets the child process's initial directory (e.g. a project folder
    /// so <c>dotnet build</c> resolves there); <see langword="null"/> inherits the host process's directory.</para>
    /// </summary>
    public TerminalEmulator(string? commandLine = "cmd.exe", string? workingDirectory = null)
    {
        _commandLine = commandLine;
        _workingDirectory = workingDirectory;
        _terminal = new VirtualTerminalController();
        _consumer = new DataConsumer(_terminal);
        _terminal.SendData += OnTerminalSendData;   // terminal responses (DSR/DA/…) → back to the process
        _terminal.WindowTitleChanged += OnWindowTitleChanged;   // OSC 0/2 title set by the running program
    }
    #endregion

    #region Properties
    public override bool HandlesInput => true;
    protected override bool RendersOwnFocus => true;   // the terminal cursor shows focus

    /// <summary>The window title the running program set via OSC 0/2, or <see langword="null"/> if none. Hosts can
    /// bind this (or <see cref="TitleChanged"/>) to a frame title.</summary>
    public string? WindowTitle { get; private set; }
    #endregion

    #region Events
    /// <summary>Raised on the UI thread after the child process exits (never for a manually-driven terminal).</summary>
    public event Action? Exited;

    /// <summary>Raised on the UI thread when the running program changes the window title (OSC 0/2).</summary>
    public event Action<string>? TitleChanged;
    #endregion

    #region Methods
    // A terminal owns its own scrollback, so it must fill the framing viewport rather than be given a frame's
    // unbounded scroll height — otherwise it balloons to ~1000 rows, oversizing the PTY and pushing live output
    // off-screen (no auto-scroll). Opting out makes the frame offer the bounded viewport height instead.
    protected internal override bool FillsFrameViewport => true;

    // The cell columns the shell draws into: the control width minus the column reserved for the scrollbar.
    private int ContentWidth => Math.Max(1, ActualWidth - ScrollbarWidth);

    // Start (or re-size) the PTY once the control has a real cell area. Runs on the UI thread.
    protected override void Control_OnInitialization()
    {
        var cols = (short)Math.Max(1, ContentWidth);
        var rows = (short)Math.Max(1, ActualHeight);
        if (cols <= 1 && rows <= 1) return;

        _terminal.ResizeView(ContentWidth, ActualHeight);

        // No command line → manual-drive mode (bytes arrive via Feed); nothing to spawn or resize.
        if (string.IsNullOrWhiteSpace(_commandLine)) { Invalidate(); return; }

        if (_pty is null)
        {
            _pty = Pty.Start(_commandLine, cols, rows, _workingDirectory);
            _pty.Exited += () => UI.Post(() => { Invalidate(); Exited?.Invoke(); });
            _cts = new CancellationTokenSource();
            _ = ReadLoopAsync(_pty, _cts.Token);
        }
        else
        {
            _pty.Resize(cols, rows);
        }
        Invalidate();
    }

    /// <summary>
    /// Pushes raw terminal output bytes into the emulator and repaints. Available for manually-driven terminals
    /// (see the <see langword="null"/>-command-line constructor). Safe to call from any thread — the work is
    /// marshaled onto the UI thread. (The PTY read loop uses the flow-controlled path, not this.)
    /// </summary>
    public void Feed(byte[] data)
    {
        if (data is null || data.Length == 0) return;
        UI.Invoke(() =>
        {
            _consumer.Push(data);
            Invalidate();
        });
    }

    // Drains the child's output on a background thread. To stay responsive under a flood (e.g. `yes`, which emits
    // megabytes/sec), output is FLOW-CONTROLLED: chunks are queued and applied to the emulator in a single
    // coalesced UI-thread drain (not one post per chunk, which would bury input in the dispatcher queue), and the
    // loop stops reading once too far ahead so the PTY buffer fills and the producer blocks on write.
    private async Task ReadLoopAsync(IPty pty, CancellationToken ct)
    {
        var buffer = new byte[16384];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var n = await pty.Output.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false);
                if (n <= 0) break;
                EnqueueOutput(buffer[..n]);
                while (!ct.IsCancellationRequested && PendingOutputBytes >= OutputHighWater)
                    await Task.Delay(2, ct).ConfigureAwait(false);   // backpressure: let rendering catch up
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* pipe closed on exit */ }
    }

    private int PendingOutputBytes { get { lock (_outLock) return _outBytes; } }

    // Queue a chunk for the UI thread, scheduling a single coalesced drain regardless of how many chunks pile up.
    private void EnqueueOutput(byte[] chunk)
    {
        bool schedule;
        lock (_outLock)
        {
            _outChunks.Enqueue(chunk);
            _outBytes += chunk.Length;
            schedule = !_drainScheduled;
            _drainScheduled = true;
        }
        if (schedule) UI.Post(DrainOutput);
    }

    // Apply a BOUNDED batch of queued output to the emulator, then repaint. Parsing a flood (e.g. `yes`) is itself
    // expensive — each "y\n" scrolls VtNetCore's history — so we cap the bytes parsed per call and, if more remains,
    // re-post ourselves. Each re-post goes to the dispatcher queue's tail, so pending input interleaves between
    // batches and the UI stays responsive (Ctrl+C still gets through to kill the flood). Runs on the UI thread.
    private void DrainOutput()
    {
        var batch = new List<byte[]>();
        var pushed = 0;
        bool more;
        lock (_outLock)
        {
            while (_outChunks.Count > 0 && pushed < OutputDrainCap)
            {
                var c = _outChunks.Dequeue();
                _outBytes -= c.Length;
                pushed += c.Length;
                batch.Add(c);
            }
            more = _outChunks.Count > 0;
            _drainScheduled = more;   // keep the chain alive while data remains; else let EnqueueOutput re-arm
        }
        foreach (var c in batch) _consumer.Push(c);
        if (batch.Count > 0) Invalidate();
        if (more) UI.Post(DrainOutput);
    }

    private void OnTerminalSendData(object? sender, SendDataEventArgs e) => WriteToProcess(e.Data);

    // Fired during _consumer.Push (already on the UI thread). Surface the new title to hosts.
    private void OnWindowTitleChanged(object? sender, TextEventArgs e)
    {
        WindowTitle = e.Text;
        TitleChanged?.Invoke(e.Text ?? string.Empty);
    }

    private void WriteToProcess(byte[] data)
    {
        var pty = _pty;
        if (pty is null || data is null || data.Length == 0) return;
        try { pty.Input.Write(data, 0, data.Length); pty.Input.Flush(); }
        catch (Exception) { }
    }

    protected override void OnInput(InputEvent inputEvent)
    {
        var key = inputEvent.Key;
        // Shift+PageUp/PageDown scroll the scrollback (the Shift tier = view/scroll, per the framework's nav
        // convention) instead of reaching the shell.
        if ((key.Modifiers & ConsoleModifiers.Shift) != 0 && key.Key is ConsoleKey.PageUp or ConsoleKey.PageDown)
        {
            ScrollByLines(key.Key == ConsoleKey.PageUp ? -(ActualHeight - 1) : ActualHeight - 1);
            inputEvent.Handled = true;
            return;
        }

        var bytes = TranslateKey(key);
        if (bytes is { Length: > 0 })
        {
            SnapToBottom();   // typing returns the view to the live prompt, as terminals do
            WriteToProcess(bytes);
            inputEvent.Handled = true;
        }
    }

    // Tag cells for mouse so motion/hover (not just clicks) reach the control — needed to forward the mouse to a
    // program that requested tracking.
    protected override bool WantsMouse => true;

    // Forward a left-button press/release to the program when it is tracking the mouse and we're at the live view;
    // otherwise let the base behavior stand (the press already focused us via click-to-focus). The framework's
    // mouse events carry no button/modifier, so only the left button (0) with no modifiers is reported.
    protected override void OnMousePress(Position position) => ForwardMouse(position, button: 0, press: true);
    protected override void OnMouseRelease(Position position) => ForwardMouse(position, button: 0, press: false);

    // When the program is tracking the mouse and we're following the live screen, the wheel belongs to it (e.g.
    // scrolling inside less/vim); otherwise it scrolls our own scrollback.
    protected override void OnMouseWheel(Position position, int delta)
    {
        if (_terminal.MouseTrackingEnabled && _follow && InContent(position))
            SendMouseReport(button: delta < 0 ? 64 : 65, position.X, position.Y, press: true);   // wheel up=64, down=65
        else
            ScrollByLines(delta);
    }

    private void ForwardMouse(Position position, int button, bool press)
    {
        if (_terminal.MouseTrackingEnabled && _follow && InContent(position))
            SendMouseReport(button, position.X, position.Y, press);
    }

    // Inside the cell area the shell draws into (excludes the reserved scrollbar column).
    private bool InContent(Position p) => p.X >= 0 && p.X < ContentWidth && p.Y >= 0 && p.Y < ActualHeight;

    // Emit a mouse report in the encoding the program negotiated (SGR 1006, else X10/X11). VtNetCore's own
    // MousePress can't encode the wheel (it masks the button to 2 bits), so we build the sequence directly.
    private void SendMouseReport(int button, int col, int row, bool press)
    {
        byte[]? data = null;
        if (_terminal.SgrMouseMode)
        {
            data = Encoding.UTF8.GetBytes($"\x1b[<{button};{col + 1};{row + 1}{(press ? 'M' : 'm')}");
        }
        else if (_terminal.X10SendMouseXYOnButton || _terminal.X11SendMouseXYOnButton
            || _terminal.CellMotionMouseTracking || _terminal.UseAllMouseTracking)
        {
            var cb = press ? button : 3;   // legacy encoding has no per-button release: button 3 = "released"
            data = [0x1b, (byte)'[', (byte)'M',
                (byte)Math.Min(255, 32 + cb), (byte)Math.Min(255, 32 + col + 1), (byte)Math.Min(255, 32 + row + 1)];
        }
        if (data is { Length: > 0 }) WriteToProcess(data);
    }

    private void ScrollByLines(int lines)
    {
        var liveTop = _terminal.ViewPort.TopRow;
        var floor = Math.Max(0, liveTop - _terminal.MaximumHistoryLines);
        var current = _follow ? liveTop : Math.Clamp(_viewTop, floor, liveTop);
        var next = Math.Clamp(current + lines, floor, liveTop);
        _follow = next >= liveTop;
        _viewTop = next;
        Invalidate();
    }

    private void SnapToBottom()
    {
        if (!_follow) { _follow = true; Invalidate(); }
    }

    public override void OnPaste(string text)
    {
        var body = Encoding.UTF8.GetBytes(text);
        SnapToBottom();
        // Bracketed paste (DECSET 2004): wrap the payload so the application can tell pasted text from typing.
        WriteToProcess(_terminal.BracketedPasteMode ? [.. Esc("[200~"), .. body, .. Esc("[201~")] : body);
    }

    /// <summary>Sends text to the process as if typed (UTF-8, no bracketed-paste wrapping). Include a trailing
    /// <c>"\r"</c> to submit a line. Snaps the view back to the live bottom.</summary>
    public void SendText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        SnapToBottom();
        WriteToProcess(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Translates a key event into the bytes to send the process, honoring the emulator's current modes
    /// (application-cursor mode, keypad, …). Navigation/function keys are mapped by VtNetCore so the sequences
    /// track those modes; Enter/Escape/printables are handled here. Returns <see langword="null"/> for a key that
    /// produces no input.
    /// </summary>
    public byte[]? TranslateKey(ConsoleKeyInfo key)
    {
        // Keys VtNetCore's table gets wrong: it emits LF for Enter (a shell wants CR) and a doubled ESC for Escape.
        switch (key.Key)
        {
            case ConsoleKey.Enter: return [(byte)'\r'];
            case ConsoleKey.Escape: return [0x1b];
        }

        var control = (key.Modifiers & ConsoleModifiers.Control) != 0;
        var shift = (key.Modifiers & ConsoleModifiers.Shift) != 0;

        // Named special keys → mode-aware sequence (arrows flip to SS3 under app-cursor mode, F-keys, Shift+Tab, …).
        if (KeyName(key.Key) is { } name)
        {
            var seq = _terminal.GetKeySequence(name, control, shift);
            if (seq is { Length: > 0 }) return seq;
        }

        // Ctrl+A..Z → 0x01..0x1A (when it didn't arrive as a control char in KeyChar).
        if (control && key.KeyChar == '\0' && key.Key is >= ConsoleKey.A and <= ConsoleKey.Z)
            return [(byte)(key.Key - ConsoleKey.A + 1)];

        return key.KeyChar != '\0' ? Encoding.UTF8.GetBytes([key.KeyChar]) : null;
    }

    // ConsoleKey → the key name VtNetCore's translation table understands (null = not a special key).
    private static string? KeyName(ConsoleKey key) => key switch
    {
        ConsoleKey.UpArrow => "Up",
        ConsoleKey.DownArrow => "Down",
        ConsoleKey.LeftArrow => "Left",
        ConsoleKey.RightArrow => "Right",
        ConsoleKey.Home => "Home",
        ConsoleKey.End => "End",
        ConsoleKey.PageUp => "PageUp",
        ConsoleKey.PageDown => "PageDown",
        ConsoleKey.Insert => "Insert",
        ConsoleKey.Delete => "Delete",
        ConsoleKey.Backspace => "Back",
        ConsoleKey.Tab => "Tab",
        ConsoleKey.F1 => "F1",
        ConsoleKey.F2 => "F2",
        ConsoleKey.F3 => "F3",
        ConsoleKey.F4 => "F4",
        ConsoleKey.F5 => "F5",
        ConsoleKey.F6 => "F6",
        ConsoleKey.F7 => "F7",
        ConsoleKey.F8 => "F8",
        ConsoleKey.F9 => "F9",
        ConsoleKey.F10 => "F10",
        ConsoleKey.F11 => "F11",
        ConsoleKey.F12 => "F12",
        _ => null,
    };

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
        var contentWidth = ContentWidth;

        // Blank the area first (the emulator only describes occupied spans).
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                consoleBuffer.Write(new Position(x, y), Blank);

        try
        {
            // Resolve the page to show: follow the live bottom, or a fixed absolute line when scrolled into history.
            var liveTop = _terminal.ViewPort.TopRow;
            var floor = Math.Max(0, liveTop - _terminal.MaximumHistoryLines);
            var top = _follow ? liveTop : Math.Clamp(_viewTop, floor, liveTop);

            // GetPageSpans takes an absolute buffer line.
            var rows = _terminal.ViewPort.GetPageSpans(top, height);
            for (var y = 0; y < rows.Count && y < height; y++)
            {
                var x = 0;
                foreach (var span in rows[y].Spans)
                {
                    var fg = ParseWebColor(span.ForgroundColor);
                    var bg = ParseWebColor(span.BackgroundColor);
                    var deco = Decoration.None;
                    if (span.Bold) deco |= Decoration.Bold;
                    if (span.Italic) deco |= Decoration.Italic;
                    if (span.Underline) deco |= Decoration.Underline;
                    if (span.Blink) deco |= Decoration.SlowBlink;
                    if (span.Hidden) deco |= Decoration.Conceal;

                    foreach (var ch in span.Text)
                    {
                        if (x >= contentWidth) break;
                        consoleBuffer.Write(new Position(x, y), new Character(ch, fg, bg, deco));
                        x++;
                    }
                }
            }

            DrawScrollBar(width - 1, height, top, floor, liveTop);

            // The terminal cursor (CursorState row/col are viewport-relative to the live page). Show it only when
            // focused, following the live bottom, and the program hasn't hidden it (DECTCEM). Only the focused
            // control owns the real cursor (Character.IsCursor); its DECSCUSR shape/blink rides the cell decoration.
            var cursor = _terminal.CursorState;
            if (IsFocused && _follow && cursor.ShowCursor)
            {
                var cx = cursor.CurrentColumn;
                var cy = cursor.CurrentRow;
                if (cx >= 0 && cx < contentWidth && cy >= 0 && cy < height)
                {
                    var cell = consoleBuffer[cx, cy].Character;
                    var deco = CursorEncoding.EncodeStyle(cell.Decoration ?? Decoration.None, CursorStyleValue(cursor));
                    consoleBuffer.Write(new Position(cx, cy),
                        new Character(cell.Content ?? ' ', cell.Foreground, cell.Background, deco, isCursor: true));
                }
            }
        }
        catch (Exception) { /* tolerate transient emulator state during resize */ }
    }

    // Draws the scrollback indicator in column <paramref name="col"/>: a thumb sized/positioned to the visible
    // window within the (history + screen) content. Drawn only when there is scrollback to show.
    private void DrawScrollBar(int col, int height, int top, int floor, int liveTop)
    {
        var available = liveTop - floor;          // history rows reachable above the live page
        if (available <= 0 || col < 0) return;    // nothing scrolled off yet → leave the gutter blank

        var total = available + height;           // full scrollable content height
        var thumbSize = Math.Clamp((int)((long)height * height / total), 1, height);
        var viewOffset = Math.Clamp(top - floor, 0, available);
        var thumbStart = (int)((long)(height - thumbSize) * viewOffset / available);

        for (var y = 0; y < height; y++)
        {
            var isThumb = y >= thumbStart && y < thumbStart + thumbSize;
            consoleBuffer.Write(new Position(col, y), isThumb ? ScrollThumb : ScrollTrack);
        }
    }

    // Map VtNetCore's cursor shape + blink to a DECSCUSR style value (the CursorStyle enum mirrors DECSCUSR 0-6).
    private static int CursorStyleValue(TerminalCursorState s) => s.CursorShape switch
    {
        ECursorShape.Underline => (int)(s.BlinkingCursor ? CursorStyle.BlinkingUnderline : CursorStyle.SteadyUnderline),
        ECursorShape.Bar       => (int)(s.BlinkingCursor ? CursorStyle.BlinkingBar : CursorStyle.SteadyBar),
        _                      => (int)(s.BlinkingCursor ? CursorStyle.BlinkingBlock : CursorStyle.SteadyBlock),
    };

    private static CColor? ParseWebColor(string? web)
    {
        if (string.IsNullOrEmpty(web) || web[0] != '#' || web.Length < 7) return null;
        return byte.TryParse(web.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
            && byte.TryParse(web.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
            && byte.TryParse(web.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)
            ? new CColor(r, g, b)
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
    private const int ScrollbarWidth = 1;
    private static readonly Character Blank = new(' ');
    private static readonly Character ScrollThumb = new('█', new CColor(0x9e, 0x9e, 0x9e), null, Decoration.None);
    private static readonly Character ScrollTrack = new('░', new CColor(0x44, 0x44, 0x44), null, Decoration.None);
    private readonly string? _commandLine;
    private readonly string? _workingDirectory;
    private readonly VirtualTerminalController _terminal;
    private readonly DataConsumer _consumer;
    private IPty? _pty;
    private CancellationTokenSource? _cts;
    // Scrollback view state: _follow pins the view to the live bottom; when false, _viewTop is the absolute top line.
    private bool _follow = true;
    private int _viewTop;
    // Flow-controlled output: chunks from the read loop, applied to the emulator on the UI thread in bounded batches.
    private const int OutputHighWater = 256 * 1024;   // pending bytes above which the read loop backs off
    private const int OutputDrainCap = 16 * 1024;     // max bytes parsed per UI-thread batch (keeps input responsive)
    private readonly object _outLock = new();
    private readonly Queue<byte[]> _outChunks = new();
    private int _outBytes;
    private bool _drainScheduled;
    #endregion
}
