namespace Jumbee.Console.Examples;

using System;
using System.Linq;

internal static class Program
{
    static void Main(string[] args)
    {
        if (args.Contains("--verify")) { Environment.Exit(Verify.Run()); return; }

        var browser = new ExampleBrowser();
        var root = browser.Build();

        var hud = new PerfHud();
        var run = UI.Start(root, width: 150, height: 42, isAnsiTerminal: true,
            input: new VtInputSource(anyMotion: true));

        hud.RegisterToggle();                                                    // Ctrl+G: the glass perf HUD
        UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.B), browser.ToggleTree);    // collapse/restore the tree
        UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.E), browser.ToggleEditor);  // collapse/restore the source pane
        UI.RegisterHotKey(new ConsoleKeyInfo('\0', ConsoleKey.F6, false, false, false), () => browser.CyclePane(+1));
        UI.RegisterHotKey(new ConsoleKeyInfo('\0', ConsoleKey.F6, true, false, false), () => browser.CyclePane(-1));
        UI.RegisterHotKey(UI.HotKeys.Ctrl(ConsoleKey.Q), UI.Stop);

        UI.SetFocus(browser.Tree);
        run.Wait();
    }
}
