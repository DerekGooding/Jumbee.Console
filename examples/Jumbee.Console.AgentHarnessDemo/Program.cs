namespace Jumbee.Console.AgentHarnessDemo;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

/// <summary>
/// Entry point for the Jumbee.Console agent-harness demo — a high-fidelity mock of the Claude desktop agent UI
/// (session rail, chat transcript, live task list, document pane, and a chat prompt with model/effort/permission
/// selectors) built entirely from Jumbee.Console controls. The agent is simulated: typing a prompt plays a scripted
/// streaming response. Pass <c>--verify</c> for a headless smoke check (renders the layout offscreen).
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Contains("--verify"))
            return Verify();
        if (args.Contains("--dump"))
        {
            System.Console.WriteLine(ConsoleSnapshot.ToText(new AgentHarnessApp().Root, 150, 44));
            return 0;
        }

        new AgentHarnessApp().Run();
        return 0;
    }

    // Builds the app headlessly, renders the layout offscreen, and asserts the landmarks are present — a CI guard that
    // the whole thing composes and paints without a terminal.
    private static int Verify()
    {
        var app = new AgentHarnessApp();
        var text = ConsoleSnapshot.ToText(app.Root, 150, 44);
        string[] expected = ["Harbor API", "Recents", "Robin Hale", "cursor pagination", "Pagination"];
        var missing = expected.Where(e => !text.Contains(e)).ToArray();
        if (missing.Length > 0)
        {
            System.Console.WriteLine("FAIL  AgentHarnessDemo verify — missing: " + string.Join(", ", missing));
            System.Console.WriteLine(text);
            return 1;
        }
        System.Console.WriteLine("PASS  AgentHarnessDemo verify — layout renders (rail, transcript, tasks, document).");
        return 0;
    }
}
