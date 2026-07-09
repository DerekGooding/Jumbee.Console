namespace Jumbee.Console.Benchmarks;

using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using ConsoleGUI;
using ConsoleGUI.Api;
using ConsoleGUI.Space;

using CColor = ConsoleGUI.Data.Color;
using CCharacter = ConsoleGUI.Data.Character;
using Rect = ConsoleGUI.Space.Rect;

/// <summary>
/// The per-frame cost of the opt-in partial-redraw path (<see cref="Control.TracksDamage"/>) driven through the
/// real <see cref="ConsoleManager"/> compositor: each iteration advances one frame (paint + composite) with damage
/// tracking on vs off. <c>Track=false</c> reports the whole control (the old behaviour), so the delta is the
/// compositor scan the opt-in skips. Two workloads: a spinning <see cref="Globe"/> (disc inscribed in a wide pane —
/// only the blank margins are skipped) and a mostly-static scene with a small moving block (nearly all skipped).
/// </summary>
[MemoryDiagnoser]
public class PartialRedrawBenchmarks
{
    private const int W = 120, H = 40;

    private Globe _globe = null!;
    private MovingBlock _static = null!;

    [Params(true, false)]
    public bool Track;

    [GlobalSetup(Target = nameof(GlobeFrame))]
    public void SetupGlobe()
    {
        _globe = new Globe { DamageTracking = Track };
        StartSession(_globe);
    }

    [GlobalSetup(Target = nameof(StaticSceneFrame))]
    public void SetupStatic()
    {
        _static = new MovingBlock { Track = Track };
        StartSession(_static);
    }

    [Benchmark]
    public void GlobeFrame()
    {
        _globe.Spin(0.02);
        UI.PaintFrame();
        ConsoleManager.Draw();
    }

    [Benchmark]
    public void StaticSceneFrame()
    {
        _static.Advance();
        UI.PaintFrame();
        ConsoleManager.Draw();
    }

    // Sets up the real compositor headlessly: a no-op console + a no-op ANSI sink, so we measure paint + composite
    // (scan/diff/encode) without any terminal I/O. Primes one full-redraw frame so later frames are incremental.
    private static void StartSession(IControl content)
    {
        ConsoleManager.AnsiEnabled = true;
        ConsoleManager.AnsiOutput = _ => Task.CompletedTask;
        ConsoleManager.Console = new NullConsole { Size = new Size(W, H) };
        ConsoleManager.Setup();
        ConsoleManager.Content = content;
        UI.PaintFrame();
        ConsoleManager.Draw();
    }

    private sealed class NullConsole : IConsole
    {
        public Size Size { get; set; }
        public bool KeyAvailable => false;
        public void Initialize() { }
        public void OnRefresh() { }
        public void Write(Position position, in CCharacter character) { }
        public System.ConsoleKeyInfo ReadKey() => throw new System.NotSupportedException();
    }

    // A screen-filling static background with a 3×3 block that steps right each frame. Track on → reports only the
    // vacated + new block (≤18 cells); off → the whole W×H rect.
    private sealed class MovingBlock : Control
    {
        public bool Track = true;
        protected override bool TracksDamage => Track;

        public void Advance() { _x++; Invalidate(); }

        protected override void Render()
        {
            int w = Size.Width, h = Size.Height;
            if (w <= 0 || h <= 0) return;

            consoleBuffer.Initialize();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    consoleBuffer.Write(new Position(x, y), new CCharacter('.', new CColor(40, 40, 40)));

            int bx = _x % (w - 3), by = h / 2;
            for (int dy = 0; dy < 3; dy++)
                for (int dx = 0; dx < 3; dx++)
                    consoleBuffer.Write(new Position(bx + dx, by + dy), new CCharacter('#', new CColor(230, 200, 90)));

            var cur = new Rect(bx, by, 3, 3);
            Damage(Rect.Surround(_prev, cur));
            _prev = cur;
        }

        private int _x;
        private Rect _prev = Rect.Empty;
    }
}
