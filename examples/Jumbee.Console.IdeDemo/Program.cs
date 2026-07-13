namespace Jumbee.Console.IdeDemo;

using Jumbee.Console.Snapshot;

/// <summary>
/// Entry point for the Jumbee.Console IDE demo — a minimal VS Code–style shell (file explorer, tabbed C# editor, and
/// an embedded terminal running in the project directory). Pass a directory path to open it; with no argument the
/// bundled sample project is opened. Pass <c>--verify</c> for a headless smoke check (renders the layout offscreen).
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Contains("--verify"))
            return Verify(ResolveProjectDir([.. args.Where(a => a != "--verify")]));
        if (args.Contains("--dump"))   // dev aid: print the offscreen-rendered layout and exit
        {
            System.Console.WriteLine(ConsoleSnapshot.ToText(
                new IdeApp(ResolveProjectDir([.. args.Where(a => a != "--dump")]), headless: true).Root, 150, 44));
            return 0;
        }

        new IdeApp(ResolveProjectDir(args)).Run();
        return 0;
    }

    // Builds the app headlessly and renders its layout offscreen, asserting the shell's landmarks are present. A CI
    // guard that the whole thing composes and paints without a terminal — no TTY required.
    private static int Verify(string projectDir)
    {
        var app = new IdeApp(projectDir, headless: true);
        var text = ConsoleSnapshot.ToText(app.Root, 150, 44);
        string[] expected = ["Explorer", "Editor", "Terminal", "File", "Build", "View", "SampleApp.csproj", "Calculator.cs"];
        var missing = expected.Where(e => !text.Contains(e)).ToArray();
        if (missing.Length > 0)
        {
            System.Console.WriteLine("FAIL  IdeDemo verify — missing: " + string.Join(", ", missing));
            System.Console.WriteLine(text);
            return 1;
        }
        System.Console.WriteLine("PASS  IdeDemo verify — layout renders (explorer, editor, terminal, menu, sample files).");
        return 0;
    }

    // The directory the IDE opens: an explicit path argument, else a writable copy of the bundled sample project.
    private static string ResolveProjectDir(string[] args)
    {
        if (args.Length > 0 && Directory.Exists(args[0]))
            return Path.GetFullPath(args[0]);
        return MaterializeSample();
    }

    // Copies the bundled sample project (shipped next to the app) into a writable temp directory so the user can edit
    // and `dotnet build`/`dotnet run` a real project without touching the app's own output. Copied once, then reused
    // (edits persist across runs until the temp directory is cleared).
    private static string MaterializeSample()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "SampleProject");
        var dest = Path.Combine(Path.GetTempPath(), "JumbeeIdeDemo", "SampleProject");
        if (!Directory.Exists(dest) && Directory.Exists(source))
            CopyDirectory(source, dest);
        return Directory.Exists(dest) ? dest
             : Directory.Exists(source) ? source
             : AppContext.BaseDirectory;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        foreach (var sub in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(sub);
            if (name is "bin" or "obj") continue;   // don't ship a stale build tree
            CopyDirectory(sub, Path.Combine(dest, name));
        }
    }
}
