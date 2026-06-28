// AOT probe: forces NativeAOT to compile the full VtNetCore + ConPTY + TerminalEmulator closure so any trim/AOT
// warnings surface at publish. `live` runs a real ConPTY smoke test (JIT; no AOT linker needed).

using System.Text;

using Jumbee.Console;

using VtNetCore.VirtualTerminal;
using VtNetCore.XTermParser;

var vt = new VirtualTerminalController();
vt.ResizeView(80, 25);
var consumer = new DataConsumer(vt);
consumer.Push(Encoding.UTF8.GetBytes("hello world\r\n"));
var spans = vt.ViewPort.GetPageSpans(vt.ViewPort.TopRow, 25);
Console.WriteLine($"VtNetCore AOT ok: parsed {spans.Count} rows");

if (args.Contains("live"))
{
    using var pty = ConPty.Start("cmd.exe", 80, 25);   // interactive shell: emits a banner immediately
    var total = 0;
    var sb = new StringBuilder();
    var reader = Task.Run(async () =>
    {
        var buf = new byte[4096];
        int n;
        while ((n = await pty.Output.ReadAsync(buf.AsMemory())) > 0)
        {
            Interlocked.Add(ref total, n);
            lock (sb) sb.Append(Encoding.UTF8.GetString(buf, 0, n));
            if (total > 64) break;
        }
    });
    reader.Wait(TimeSpan.FromSeconds(3));

    Console.WriteLine($"ConPTY live smoke: {(total > 0 ? "PASS - PTY pipe delivered child output" : "FAIL - no data on the PTY pipe")}");
    string captured;
    lock (sb) captured = sb.ToString();
    var esc = ((char)27).ToString();
    var sample = captured.Replace(esc, "<ESC>").Replace("\r", "").Replace("\n", "|");
    Console.WriteLine($"bytes={total}; sample={(sample.Length > 90 ? sample[..90] : sample)}");
}

if (args.Length > 1_000_000)   // never true - keeps TerminalEmulator reachable for the AOT compiler
{
    using var term = new TerminalEmulator("cmd.exe");
    GC.KeepAlive(term);
}
