namespace Jumbee.Console;

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// A Unix pseudo terminal (Linux/macOS) session.
/// </summary>
/// <remarks>
/// Opens a pty (<c>posix_openpt</c>) and launches the child with <c>posix_spawn</c> — which fork+execs atomically in
/// native code, so NO managed code runs in a forked child (a raw <c>fork()</c>/<c>forkpty()</c> segfaults the .NET
/// runtime). Pure managed P/Invoke against libc — no shipped native binaries, mirroring <see cref="ConPty"/> on Windows.
/// <para>Compiles on any OS (P/Invoke declarations are metadata) but only runs on Linux/macOS.</para>
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

    #endregion Constructors

    #region Properties

    /// <inheritdoc/>
    public Stream Input { get; }

    /// <inheritdoc/>
    public Stream Output { get; }

    #endregion Properties

    #region Events

    /// <inheritdoc/>
    public event Action? Exited;

    #endregion Events

    #region Methods

    /// <summary>Launches <paramref name="commandLine"/> in a new pty of the given size, optionally starting the
    /// child in <paramref name="workingDirectory"/> (null inherits the host process's directory).</summary>
    public static UnixPty Start(string commandLine, short columns, short rows, string? workingDirectory = null)
    {
        // Open the pty controller (master) and unlock + name the subordinate (slave) side.
        var controller = posix_openpt(O_RDWR);
        if (controller < 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "posix_openpt failed");
        if (grantpt(controller) != 0 || unlockpt(controller) != 0)
        {
            close(controller);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "grantpt/unlockpt failed");
        }

        var subName = Marshal.PtrToStringUTF8(ptsname(controller))
            ?? throw new InvalidOperationException("ptsname returned null");

        // Size the pty before the child starts so it lays out correctly.
        var winSize = new WinSize((ushort)Math.Max((short)1, rows), (ushort)Math.Max((short)1, columns));
        ioctl(controller, TIOCSWINSZ, ref winSize);

        var parts = commandLine.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var app = parts.Length > 0 ? parts[0] : "/bin/sh";

        var (argvElems, argvPtr) = BuildStringArray(parts.Length > 0 ? parts : [app]);
        var (envElems, envPtr) = BuildStringArray(BuildEnvironment());
        var appPtr = Marshal.StringToCoTaskMemUTF8(app);
        var subPtr = Marshal.StringToCoTaskMemUTF8(subName);
        var cwdPtr = string.IsNullOrEmpty(workingDirectory) ? IntPtr.Zero : Marshal.StringToCoTaskMemUTF8(workingDirectory);

        // File actions: the child opens the subordinate as stdin (acquiring it as its controlling terminal once it
        // is a session leader — see SETSID below), dups it to stdout/stderr, and closes the inherited controller.
        // The opaque structs are zeroed native blobs sized for glibc (macOS uses far less).
        var fa = Marshal.AllocHGlobal(FileActionsSize);
        var at = Marshal.AllocHGlobal(SpawnAttrSize);
        Marshal.Copy(new byte[FileActionsSize], 0, fa, FileActionsSize);
        Marshal.Copy(new byte[SpawnAttrSize], 0, at, SpawnAttrSize);
        var pid = 0;
        try
        {
            posix_spawn_file_actions_init(fa);
            posix_spawn_file_actions_addopen(fa, 0, subPtr, O_RDWR, 0);
            posix_spawn_file_actions_adddup2(fa, 0, 1);
            posix_spawn_file_actions_adddup2(fa, 0, 2);
            posix_spawn_file_actions_addclose(fa, controller);

            // Start the child in workingDirectory by adding a chdir to the file actions — it runs inside the spawned
            // child (post-fork, pre-exec) in native code, so it's fork-safe like the rest (a managed chdir would be
            // process-global). addchdir_np is glibc 2.29+ / macOS 10.15+; on an older libc it's absent, so degrade to
            // the inherited directory rather than failing the whole launch.
            if (cwdPtr != IntPtr.Zero)
            {
                try { posix_spawn_file_actions_addchdir_np(fa, cwdPtr); }
                catch (EntryPointNotFoundException) { }
            }

            posix_spawnattr_init(at);
            posix_spawnattr_setflags(at, POSIX_SPAWN_SETSID);   // new session → the opened tty becomes controlling

            var rc = posix_spawnp(out pid, appPtr, fa, at, argvPtr, envPtr);
            if (rc != 0) throw new Win32Exception(rc, $"posix_spawnp('{app}') failed");
        }
        finally
        {
            posix_spawn_file_actions_destroy(fa);
            posix_spawnattr_destroy(at);
            Marshal.FreeHGlobal(fa);
            Marshal.FreeHGlobal(at);
            FreeAll(argvElems); Marshal.FreeCoTaskMem(argvPtr);
            FreeAll(envElems); Marshal.FreeCoTaskMem(envPtr);
            Marshal.FreeCoTaskMem(appPtr);
            Marshal.FreeCoTaskMem(subPtr);
            if (cwdPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(cwdPtr);
        }

        // The controller fd is bidirectional; use a duplicate so the read loop and input writes don't contend on
        // one FileStream. Each FileStream owns its fd and closes it on Dispose.
        var writeFd = dup(controller);
        var output = new FileStream(new SafeFileHandle((nint)controller, ownsHandle: true), FileAccess.Read);
        var input = new FileStream(new SafeFileHandle((nint)writeFd, ownsHandle: true), FileAccess.Write);

        var pty = new UnixPty(controller, pid, input, output);
        pty.WatchForExit();
        return pty;
    }

    /// <inheritdoc/>
    public void Resize(short columns, short rows)
    {
        if (_controller < 0) return;
        var winSize = new WinSize((ushort)Math.Max((short)1, rows), (ushort)Math.Max((short)1, columns));
        ioctl(_controller, TIOCSWINSZ, ref winSize);
    }

    /// <summary>Sends SIGHUP to the child process and disposes the pty streams.</summary>
    public void Dispose()
    {
        if (_pid > 0) kill(_pid, SIGHUP);
        Input.Dispose();    // closes the controller + its dup
        Output.Dispose();
        _controller = -1;
    }

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

    // The child's environment: the current process env with TERM forced to a sane value.
    private static string[] BuildEnvironment()
    {
        var env = new List<string>();
        foreach (DictionaryEntry e in Environment.GetEnvironmentVariables())
            if (!string.Equals(e.Key as string, "TERM", StringComparison.Ordinal))
                env.Add($"{e.Key}={e.Value}");
        env.Add("TERM=xterm-256color");
        return [.. env];
    }

    // Marshals a managed string[] to a null-terminated native char** (each element UTF-8).
    private static (IntPtr[] elems, IntPtr array) BuildStringArray(string[] items)
    {
        var elems = new IntPtr[items.Length + 1];
        for (var i = 0; i < items.Length; i++) elems[i] = Marshal.StringToCoTaskMemUTF8(items[i]);
        elems[^1] = IntPtr.Zero;
        var array = Marshal.AllocCoTaskMem(IntPtr.Size * elems.Length);
        Marshal.Copy(elems, 0, array, elems.Length);
        return (elems, array);
    }

    private static void FreeAll(IntPtr[] elems)
    {
        foreach (var p in elems) if (p != IntPtr.Zero) Marshal.FreeCoTaskMem(p);
    }

    #endregion Methods

    #region Fields

    private int _controller;
    private readonly int _pid;

    private const int EINTR = 4;
    private const int SIGHUP = 1;
    private const int O_RDWR = 2;

    // posix_spawn opaque structs: allocate the (larger) glibc sizes with headroom; macOS uses far less.
    private const int FileActionsSize = 128;   // glibc posix_spawn_file_actions_t ≈ 80

    private const int SpawnAttrSize = 512;      // glibc posix_spawnattr_t ≈ 336

    // POSIX_SPAWN_SETSID differs by libc; TIOCSWINSZ ioctl request likewise.
    private static short POSIX_SPAWN_SETSID => (short)(OperatingSystem.IsMacOS() ? 0x0400 : 0x80);

    private static ulong TIOCSWINSZ => OperatingSystem.IsMacOS() ? 0x80087467UL : 0x5414UL;

    #endregion Fields

    #region Native  (all in libc; on macOS "libc" resolves to libSystem)

    [DllImport("libc", SetLastError = true)] private static extern int posix_openpt(int flags);

    [DllImport("libc", SetLastError = true)] private static extern int grantpt(int fd);

    [DllImport("libc", SetLastError = true)] private static extern int unlockpt(int fd);

    [DllImport("libc", SetLastError = true)] private static extern IntPtr ptsname(int fd);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawn_file_actions_init(IntPtr fileActions);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawn_file_actions_addopen(IntPtr fileActions, int fd, IntPtr path, int oflag, uint mode);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawn_file_actions_adddup2(IntPtr fileActions, int fd, int newFd);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawn_file_actions_addclose(IntPtr fileActions, int fd);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawn_file_actions_addchdir_np(IntPtr fileActions, IntPtr path);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawn_file_actions_destroy(IntPtr fileActions);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawnattr_init(IntPtr attr);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawnattr_setflags(IntPtr attr, short flags);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawnattr_destroy(IntPtr attr);

    [DllImport("libc", SetLastError = true)] private static extern int posix_spawnp(out int pid, IntPtr file, IntPtr fileActions, IntPtr attr, IntPtr argv, IntPtr envp);

    [DllImport("libc", SetLastError = true)] private static extern int ioctl(int fd, ulong request, ref WinSize winSize);

    [DllImport("libc", SetLastError = true)] private static extern int dup(int fd);

    [DllImport("libc", SetLastError = true)] private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)] private static extern int waitpid(int pid, ref int status, int options);

    [DllImport("libc", SetLastError = true)] private static extern int kill(int pid, int sig);

    [StructLayout(LayoutKind.Sequential)]
    private struct WinSize
    {
        public ushort Row, Col, XPixel, YPixel;

        public WinSize(ushort row, ushort col)
        { Row = row; Col = col; XPixel = 0; YPixel = 0; }
    }

    #endregion Native  (all in libc; on macOS "libc" resolves to libSystem)
}