# <a id="Jumbee_Console_Snapshot"></a> Namespace Jumbee.Console.Snapshot

### Classes

 [AnsiConsoleSession](Jumbee.Console.Snapshot.AnsiConsoleSession.md)

A stateful counterpart to <xref href="Jumbee.Console.Snapshot.AnsiConsoleSnapshot" data-throw-if-not-resolved="false"></xref> for testing the <em>live</em> render — used to
reproduce diff/cursor or async-ordering bugs that only appear across frames (e.g. press → release).

 [AnsiConsoleSnapshot](Jumbee.Console.Snapshot.AnsiConsoleSnapshot.md)

Drives the <em>real</em> <xref href="ConsoleGUI.ConsoleManager" data-throw-if-not-resolved="false"></xref> ANSI render path headlessly, captures the emitted escape
sequences via <xref href="ConsoleGUI.ConsoleManager.AnsiOutput" data-throw-if-not-resolved="false"></xref>, and parses them back into an <xref href="Jumbee.Console.Snapshot.AnsiScreen" data-throw-if-not-resolved="false"></xref>.

 [AnsiScreen](Jumbee.Console.Snapshot.AnsiScreen.md)

A small VT/ANSI screen model that parses the subset of escape sequences <code>ConsoleManager</code> emits and
maintains a cell grid, exactly as a terminal would.

 [ConsoleSnapshot](Jumbee.Console.Snapshot.ConsoleSnapshot.md)

Renders Jumbee.Console controls headlessly (without a real terminal) to a <xref href="Jumbee.Console.ConsoleBuffer" data-throw-if-not-resolved="false"></xref>,
and converts that buffer to a text or PNG snapshot. Intended for tests and visual verification.

 [SnapshotImageOptions](Jumbee.Console.Snapshot.SnapshotImageOptions.md)

Options controlling how a <xref href="Jumbee.Console.ConsoleBuffer" data-throw-if-not-resolved="false"></xref> is rendered to a PNG image.

