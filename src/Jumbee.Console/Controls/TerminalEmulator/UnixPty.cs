namespace Jumbee.Console;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Win32.SafeHandles;

/// <summary>
/// A Unix pseudo terminal (Linux/macOS) session: <c>forkpty</c> launches the child attached to a new pty, and the
/// controller (master) fd is exposed as input/output streams. Pure managed P/Invoke against libc/libutil — no
/// shipped native binaries, mirroring <see cref="ConPty"/> on Windows.
/// </summary>
/// <remarks>
/// NOTE: this compiles on any OS (P/Invoke declarations are just metadata) but only runs on Linux/macOS, and must
/// be validated on a real Linux/macOS host (the same way ConPTY needs a real Windows console). Modeled on
/// Microsoft's vs-pty.net <c>Linux/</c>+<c>Mac/</c> providers.
/// </remarks>
public sealed class UnixPty : IPty
{
    #region Constructors
    private UnixPty(int controller, int pid, Stream input, Stream output)
    {
        _controller = controller;
        _pid = pid;
        Input = input;
        Output = output;
    }
    #endregion

    #region Properties
    public Stream Input { get; }
    public Stream Output { get; }
    #endregion

    #region Events
    public event Action? Exited;
    #endregion

    #region Methods
    /// <summary>Launches <paramref name="commandLine"/> in a new pty of the given size.</summary>
    public static UnixPty Start(string commandLine, short columns, short rows)
    {
        // Split the command line into program + args (simple whitespace split — adequate for a shell path).
        var parts = commandLine.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var app = parts.Length > 0 ? parts[0] : "/bin/sh";

        // Build argv (argv[0] = app) and the TERM env pair as native memory BEFORE the fork, so the child only
        // makes blittable libc calls (no managed allocation/GC between fork and exec — the classic fork hazard).
        var (argvElems, argvPtr) = BuildArgv(app, parts);
        var termName = Marshal.StringToCoTaskMemUTF8("TERM");
        var termValue = Marshal.StringToCoTaskMemUTF8("xterm-256color");

        var winSize = new WinSize((ushort)Math.Max((short)1, rows), (ushort)Math.Max((short)1, columns));
        var controller = 0;
        var pid = ForkPty(ref controller, ref winSize);

        if (pid == 0)
        {
            // CHILD: only async-signal-safe-ish blittable calls until exec. Give the shell a sane TERM, exec it,
            // and bail if exec fails. (No managed allocation here on purpose.)
            setenv(termName, termValue, 1);
            execvp(argvElems[0], argvPtr);
            _exit(127);   // unreachable unless exec failed
        }

        // PARENT: the child has its own (copy-on-write) copy of the marshalled memory, so free ours now.
        FreeNative(argvElems, argvPtr, termName, termValue);

        if (pid == -1)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "forkpty failed");

        // The controller fd is bidirectional; use a duplicate so concurrent read (loop) and write (input) don't
        // contend on one FileStream. Each FileStream owns its fd and closes it on Dispose.
        var writeFd = dup(controller);
        var output = new FileStream(new SafeFileHandle((nint)controller, ownsHandle: true), FileAccess.Read);
        var input = new FileStream(new SafeFileHandle((nint)writeFd, ownsHandle: true), FileAccess.Write);

        var pty = new UnixPty(controller, pid, input, output);
        pty.WatchForExit();
        return pty;
    }

    public void Resize(short columns, short rows)
    {
        if (_controller < 0) return;
        var winSize = new WinSize((ushort)Math.Max((short)1, rows), (ushort)Math.Max((short)1, columns));
        ioctl(_controller, TIOCSWINSZ, ref winSize);
    }

    public void Dispose()
    {
        if (_pid > 0) kill(_pid, SIGHUP);   // ask the child to exit
        Input.Dispose();                    // closes the controller + its dup
        Output.Dispose();
        _controller = -1;
    }

    // Blocks a background thread on the child until it exits, then raises Exited.
    private void WatchForExit()
    {
        var t = new Thread(() =>
        {
            var status = 0;
            while (waitpid(_pid, ref status, 0) == -1 && Marshal.GetLastWin32Error() == EINTR) { }
            Exited?.Invoke();
        })
        { IsBackground = true, Name = $"UnixPty waitpid {_pid}" };
        t.Start();
    }

    private static int ForkPty(ref int controller, ref WinSize winSize) =>
        OperatingSystem.IsMacOS()
            ? forkpty_mac(ref controller, IntPtr.Zero, IntPtr.Zero, ref winSize)
            : forkpty_linux(ref controller, IntPtr.Zero, IntPtr.Zero, ref winSize);

    private static (IntPtr[] elems, IntPtr argv) BuildArgv(string app, string[] parts)
    {
        // elems: app, parts[1..], null terminator.
        var elems = new IntPtr[parts.Length + 1];
        elems[0] = Marshal.StringToCoTaskMemUTF8(app);
        for (var i = 1; i < parts.Length; i++) elems[i] = Marshal.StringToCoTaskMemUTF8(parts[i]);
        elems[^1] = IntPtr.Zero;

        var argv = Marshal.AllocCoTaskMem(IntPtr.Size * elems.Length);
        Marshal.Copy(elems, 0, argv, elems.Length);
        return (elems, argv);
    }

    private static void FreeNative(IntPtr[] argvElems, IntPtr argv, IntPtr termName, IntPtr termValue)
    {
        foreach (var p in argvElems) if (p != IntPtr.Zero) Marshal.FreeCoTaskMem(p);
        Marshal.FreeCoTaskMem(argv);
        Marshal.FreeCoTaskMem(termName);
        Marshal.FreeCoTaskMem(termValue);
    }
    #endregion

    #region Fields
    private int _controller;
    private readonly int _pid;

    private const int EINTR = 4;
    private const int SIGHUP = 1;
    // ioctl request to set the window size; the encoding differs between macOS and Linux.
    private static ulong TIOCSWINSZ => OperatingSystem.IsMacOS() ? 0x80087467UL : 0x5414UL;
    #endregion

    #region Native
    // forkpty lives in libutil on Linux but in libc (libSystem) on macOS.
    [DllImport("libutil.so.1", EntryPoint = "forkpty", SetLastError = true)]
    private static extern int forkpty_linux(ref int amaster, IntPtr name, IntPtr termp, ref WinSize winp);

    [DllImport("libc", EntryPoint = "forkpty", SetLastError = true)]
    private static extern int forkpty_mac(ref int amaster, IntPtr name, IntPtr termp, ref WinSize winp);

    // The rest are in libc on both (on macOS "libc" resolves to libSystem).
    [DllImport("libc", SetLastError = true)]
    private static extern int execvp(IntPtr file, IntPtr argv);

    [DllImport("libc", SetLastError = true)]
    private static extern int setenv(IntPtr name, IntPtr value, int overwrite);

    [DllImport("libc", EntryPoint = "_exit")]
    private static extern void _exit(int status);

    [DllImport("libc", SetLastError = true)]
    private static extern int ioctl(int fd, ulong request, ref WinSize winSize);

    [DllImport("libc", SetLastError = true)]
    private static extern int dup(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern int waitpid(int pid, ref int status, int options);

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    [StructLayout(LayoutKind.Sequential)]
    private struct WinSize
    {
        public ushort Row, Col, XPixel, YPixel;
        public WinSize(ushort row, ushort col) { Row = row; Col = col; XPixel = 0; YPixel = 0; }
    }
    #endregion
}
