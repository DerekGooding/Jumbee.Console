namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

/// <summary>
/// An <see cref="IInputSource"/> that puts the terminal into VT input mode and enables mouse, bracketed-paste, and
/// focus reporting, then reads the raw stdin byte stream, decodes it (UTF-8) and runs it through
/// <see cref="AnsiInputDecoder"/> to produce <see cref="TerminalInputEvent"/>s. Restores all terminal state on
/// <see cref="Dispose"/> — pass one to <see cref="UI.Start"/> and it is disposed by <see cref="UI.Stop"/>.
/// </summary>
/// <remarks>
/// Reading runs on a dedicated background thread holding a single outstanding read; an idle timeout flushes the
/// decoder so a lone ESC keypress resolves to <see cref="ConsoleKey.Escape"/> instead of waiting for the next byte.
/// Requires a real interactive terminal; not used by the headless/test paths (which inject their own source).
/// </remarks>
public sealed class VtInputSource : IInputSource, IDisposable
{
    #region Constructors
    public VtInputSource(int idleFlushMs = 40)
    {
        _idleFlushMs = idleFlushMs;
        _mode = TerminalInputMode.Enable();
        // When stdin is redirected, the mode opened /dev/tty for raw input — read from that fd so keyboard input
        // still flows (the `myapp < file` / `echo x | myapp` case). Otherwise read stdin as usual. ownsHandle:false
        // because TerminalInputMode owns the fd and closes it on dispose.
        _stdin = _mode.OwnsInputFd
            ? new FileStream(new SafeFileHandle(new IntPtr(_mode.InputFd), ownsHandle: false), FileAccess.Read)
            : Console.OpenStandardInput();
        _reader = new Thread(ReaderLoop) { IsBackground = true, Name = "Jumbee.VtInput" };
        _reader.Start();
    }
    #endregion

    #region Methods
    /// <inheritdoc/>
    public bool TryRead(out TerminalInputEvent? evt)
    {
        if (_queue.TryDequeue(out var e)) { evt = e; return true; }
        evt = null;
        return false;
    }

    // Single dedicated reader: keep exactly one outstanding ReadAsync; on its idle timeout (no new bytes) flush
    // the decoder once so a dangling ESC resolves. The decoder is touched only on this thread; the queue is the
    // only cross-thread handoff.
    private void ReaderLoop()
    {
        var bytes = new byte[1024];
        var chars = new char[1024];
        var utf8 = Encoding.UTF8.GetDecoder();
        Task<int>? read = null;
        var flushedWhileIdle = false;

        while (_running)
        {
            read ??= _stdin.ReadAsync(bytes, 0, bytes.Length);
            if (read.Wait(_idleFlushMs))
            {
                int n;
                try { n = read.Result; }
                catch { break; } // stream closed (dispose)
                read = null;
                if (n <= 0) { Thread.Sleep(_idleFlushMs); continue; } // EOF / closed handle

                int charCount = utf8.GetChars(bytes, 0, n, chars, 0);
                if (charCount > 0)
                {
                    Enqueue(_decoder.Feed(chars.AsSpan(0, charCount)));
                    flushedWhileIdle = false;
                }
            }
            else if (!flushedWhileIdle)
            {
                Enqueue(_decoder.Flush());
                flushedWhileIdle = true;
            }
        }
    }

    private void Enqueue(IReadOnlyList<TerminalInputEvent> events)
    {
        foreach (var e in events) _queue.Enqueue(e);
    }

    public void Dispose()
    {
        if (!_running) return;
        _running = false;     // reader exits on its next idle timeout (it is a background thread)
        _mode.Dispose();      // disable reporting + restore the original console mode
    }
    #endregion

    #region Fields
    private readonly AnsiInputDecoder _decoder = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<TerminalInputEvent> _queue = new();
    private readonly Stream _stdin;
    private readonly Thread _reader;
    private readonly TerminalInputMode _mode;
    private readonly int _idleFlushMs;
    private volatile bool _running = true;
    #endregion
}

/// <summary>
/// Enables VT input mode + mouse/bracketed-paste/focus reporting and restores the prior state on dispose.
/// On Windows this adjusts the console input mode via the Win32 API; on Unix it puts the tty into raw mode via
/// libc <c>cfmakeraw</c> (so reads are byte-at-a-time, unbuffered, and not echoed). The DEC private-mode toggles
/// are emitted as ANSI on all platforms.
/// </summary>
/// <remarks>
/// Unix raw mode disables <c>ISIG</c>, so Ctrl+C arrives as the byte <c>0x03</c> (a key event) rather than raising
/// SIGINT — the application is responsible for its own quit affordance on Unix (this differs from Windows, which
/// leaves Ctrl+C signalling intact).
/// </remarks>
internal sealed class TerminalInputMode : IDisposable
{
    // SGR mouse (1006) + button/drag tracking (1002) + bracketed paste (2004) + focus (1004).
    private const string EnableSeq = "\x1b[?1002h\x1b[?1006h\x1b[?2004h\x1b[?1004h";
    private const string DisableSeq = "\x1b[?1004l\x1b[?2004l\x1b[?1006l\x1b[?1002l";

    public static TerminalInputMode Enable() => new();

    private TerminalInputMode()
    {
        if (OperatingSystem.IsWindows())
        {
            _stdinHandle = GetStdHandle(STD_INPUT_HANDLE);
            if (GetConsoleMode(_stdinHandle, out _originalMode))
            {
                var mode = _originalMode;
                mode &= ~(ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT | ENABLE_QUICK_EDIT_MODE);
                mode |= ENABLE_VIRTUAL_TERMINAL_INPUT | ENABLE_EXTENDED_FLAGS;
                _modeChanged = SetConsoleMode(_stdinHandle, mode);
            }
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            // Resolve the controlling terminal: use stdin when it's a tty, else open /dev/tty so the app still
            // gets raw keyboard input when stdin is redirected (`myapp < file`, `echo x | myapp`) — the same trick
            // fzf/gum use to keep working in a pipeline. _inputFd < 0 (no controlling terminal, e.g. CI) → skip.
            _inputFd = isatty(STDIN_FILENO) == 1 ? STDIN_FILENO : open(DevTty, O_RDWR);
            if (_inputFd >= 0)
            {
                _ownsInputFd = _inputFd != STDIN_FILENO; // opened /dev/tty ourselves → must close it on dispose
                // Save current settings, then switch to raw mode. cfmakeraw also sets VMIN=1/VTIME=0, which is
                // exactly what the byte-at-a-time ReaderLoop wants.
                _savedTermios = new byte[TermiosSize];
                if (tcgetattr(_inputFd, _savedTermios) == 0)
                {
                    var raw = (byte[])_savedTermios.Clone();
                    cfmakeraw(raw);
                    _termiosChanged = tcsetattr(_inputFd, TCSANOW, raw) == 0;
                }
            }
        }

        Console.Out.Write(EnableSeq);
        Console.Out.Flush();

        // Safety net: restore the terminal even on an abrupt exit (e.g. Ctrl+C ends the process before Stop runs),
        // otherwise it is left in mouse-reporting mode.
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnProcessExit(object? sender, EventArgs e) => Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

        try { Console.Out.Write(DisableSeq); Console.Out.Flush(); }
        catch { /* best effort */ }

        if (_modeChanged) SetConsoleMode(_stdinHandle, _originalMode);
        if (_termiosChanged) tcsetattr(_inputFd, TCSANOW, _savedTermios!);
        if (_ownsInputFd) close(_inputFd);
    }

    /// <summary>The terminal input fd raw mode was applied to — stdin, or a /dev/tty we opened; -1 if none.</summary>
    public int InputFd => _inputFd;

    /// <summary><see langword="true"/> when <see cref="InputFd"/> is a /dev/tty we opened (read from it, not stdin).</summary>
    public bool OwnsInputFd => _ownsInputFd;

    #region Win32
    private const int STD_INPUT_HANDLE = -10;
    private const uint ENABLE_LINE_INPUT = 0x0002;
    private const uint ENABLE_ECHO_INPUT = 0x0004;
    private const uint ENABLE_EXTENDED_FLAGS = 0x0080;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;       // intercepts mouse; must be off for mouse reporting
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200; // deliver input as VT sequences

    private readonly IntPtr _stdinHandle;
    private readonly uint _originalMode;
    private readonly bool _modeChanged;
    private bool _disposed;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    #endregion

    #region Posix
    private const int STDIN_FILENO = 0;
    private const int TCSANOW = 0;          // apply termios change immediately (same value on Linux/macOS/BSD)
    private const int O_RDWR = 2;           // open() flags — value is identical on Linux/macOS/BSD
    private const string DevTty = "/dev/tty";
    // struct termios is at most ~72 bytes (macOS); over-allocate so tcgetattr's write always fits. The blob is
    // opaque — cfmakeraw does all the field/flag manipulation, so we never depend on the per-platform layout.
    private const int TermiosSize = 128;

    private readonly byte[]? _savedTermios;
    private readonly bool _termiosChanged;
    private readonly int _inputFd = -1;     // stdin or an opened /dev/tty; -1 = not resolved (Windows / no tty)
    private readonly bool _ownsInputFd;     // we opened /dev/tty and must close it

    [DllImport("libc", SetLastError = true)]
    private static extern int isatty(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcgetattr(int fd, [Out] byte[] termios);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcsetattr(int fd, int optionalActions, [In] byte[] termios);

    [DllImport("libc")]
    private static extern void cfmakeraw([In, Out] byte[] termios);
    #endregion
}
