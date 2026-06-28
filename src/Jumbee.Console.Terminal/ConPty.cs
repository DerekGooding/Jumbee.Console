namespace Jumbee.Console.Terminal;

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

/// <summary>
/// A pseudo-console (ConPTY) session: launches a process attached to a Windows pseudo console and exposes its
/// stdin/stdout as streams. Pure managed P/Invoke against the OS conhost (Windows 10 1809+) — no shipped native
/// binaries, so it is trim/single-file/AOT clean (unlike winpty-based wrappers).
/// </summary>
public sealed class ConPty : IDisposable
{
    #region Constructors
    private ConPty(IntPtr handle, Stream input, Stream output, SafeProcessHandle process)
    {
        _handle = handle;
        Input = input;
        Output = output;
        _process = process;
    }
    #endregion

    #region Properties
    /// <summary>Write here to send input (keystrokes/bytes) to the child process.</summary>
    public Stream Input { get; }

    /// <summary>Read here to receive the child process's terminal output (the ANSI stream).</summary>
    public Stream Output { get; }
    #endregion

    #region Events
    /// <summary>Raised (on a thread-pool thread) when the child process exits.</summary>
    public event Action? Exited;
    #endregion

    #region Methods
    /// <summary>Launches <paramref name="commandLine"/> in a new pseudo console of the given size.</summary>
    public static ConPty Start(string commandLine, short columns, short rows)
    {
        // Two pipes: one feeds the PTY (we write → child reads), one drains it (child writes → we read).
        CreatePipe(out var inputRead, out var inputWrite);
        CreatePipe(out var outputRead, out var outputWrite);

        var size = new COORD { X = Math.Max((short)1, columns), Y = Math.Max((short)1, rows) };
        var hr = CreatePseudoConsole(size, inputRead, outputWrite, 0, out var hPC);
        if (hr != 0) throw new Win32Exception(hr, "CreatePseudoConsole failed");

        var process = StartProcess(commandLine, hPC);

        // Close our copies of the PTY-end pipe handles only AFTER the child is launched (the conhost has dup'd them
        // by now). Closing them before CreateProcess can leave the child attached to the parent console instead.
        inputRead.Dispose();
        outputWrite.Dispose();

        var input = new FileStream(inputWrite, FileAccess.Write);
        var output = new FileStream(outputRead, FileAccess.Read);
        var pty = new ConPty(hPC, input, output, process);
        pty.WatchForExit();
        return pty;
    }

    /// <summary>Resizes the pseudo console (call when the host control's cell area changes).</summary>
    public void Resize(short columns, short rows)
    {
        if (_handle == IntPtr.Zero) return;
        ResizePseudoConsole(_handle, new COORD { X = Math.Max((short)1, columns), Y = Math.Max((short)1, rows) });
    }

    public void Dispose()
    {
        // Closing the pseudo console signals the child's input EOF and lets it exit.
        if (_handle != IntPtr.Zero) { ClosePseudoConsole(_handle); _handle = IntPtr.Zero; }
        Input.Dispose();
        Output.Dispose();
        _process.Dispose();
    }

    private void WatchForExit() => Task.Run(() =>
    {
        WaitForSingleObject(_process, INFINITE);
        Exited?.Invoke();
    });

    private static SafeProcessHandle StartProcess(string commandLine, IntPtr hPC)
    {
        var attrSize = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attrSize);
        var attrList = Marshal.AllocHGlobal(attrSize);
        try
        {
            var startup = new STARTUPINFOEX();
            startup.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
            startup.lpAttributeList = attrList;

            if (!InitializeProcThreadAttributeList(attrList, 1, 0, ref attrSize))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "InitializeProcThreadAttributeList failed");

            if (!UpdateProcThreadAttribute(attrList, 0, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, hPC, IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateProcThreadAttribute failed");

            // CreateProcess may mutate the command line buffer, so hand it a mutable copy.
            var cmd = new string(commandLine.ToCharArray());
            if (!CreateProcess(null, cmd, IntPtr.Zero, IntPtr.Zero, false,
                    EXTENDED_STARTUPINFO_PRESENT, IntPtr.Zero, null, ref startup, out var pi))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateProcess failed");

            CloseHandle(pi.hThread);
            return new SafeProcessHandle(pi.hProcess, ownsHandle: true);
        }
        finally
        {
            DeleteProcThreadAttributeList(attrList);
            Marshal.FreeHGlobal(attrList);
        }
    }

    private static void CreatePipe(out SafeFileHandle read, out SafeFileHandle write)
    {
        if (!CreatePipe(out read, out write, IntPtr.Zero, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreatePipe failed");
    }
    #endregion

    #region Fields
    private IntPtr _handle;
    private readonly SafeProcessHandle _process;

    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
    private const uint INFINITE = 0xFFFFFFFF;
    #endregion

    #region Native
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(COORD size, SafeFileHandle hInput, SafeFileHandle hOutput, uint dwFlags, out IntPtr phPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int ResizePseudoConsole(IntPtr hPC, COORD size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcess(string? lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string? lpCurrentDirectory, ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(SafeProcessHandle hHandle, uint dwMilliseconds);

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD { public short X; public short Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION { public IntPtr hProcess; public IntPtr hThread; public int dwProcessId; public int dwThreadId; }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX { public STARTUPINFO StartupInfo; public IntPtr lpAttributeList; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }
    #endregion
}
