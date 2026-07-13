namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>
/// A VS-Code-style workspace: a tabbed <see cref="MultiTabCodeEditor"/> stacked over a live
/// <see cref="TerminalEmulator"/> in a vertical <see cref="SplitPanel"/> — closable tabs, syntax highlighting, and a
/// draggable divider to the shell below (ported from the IDE demo). Click either pane to direct input to it.
/// </summary>
public sealed class MultiTabEditorExample : CompositeControl, IExample, IActivatableExample
{
    public MultiTabEditorExample()
    {
        editor = new MultiTabCodeEditor(Language.CSharp);
        terminal = new TerminalEmulator(Pty.DefaultShell);   // a real shell, started/stopped by the IActivatable hooks

        editor.OpenDocument("Program.cs",
            "static class Program\n{\n    static void Main()\n    {\n        System.Console.WriteLine(\"Hello, Jumbee!\");\n    }\n}\n");
        editor.OpenDocument("notes.md",
            "# Notes\n\n- Edit any tab; click the ✕ to close one, the + to add one (Alt+←/→ switches tabs)\n" +
            "- Click the terminal below and type — try `dir`, `echo hi`, or `dotnet --version`\n" +
            "- Drag the divider between the editor and terminal, or focus it and press ↑/↓\n",
            Language.Markdown);

        // Editor over terminal, each in its own titled frame. Hosting the split inside a composite (rather than being
        // a bare SplitPanel) gives the terminal a focus root that delegates back to it — so click-to-focus reaches
        // the terminal instead of dead-ending at the example host. See FocusChild below.
        SetContent(new SplitPanel(SplitOrientation.Vertical,
            editor.WithFrame(BorderStyle.Rounded, title: "Editor"),
            terminal.WithFrame(BorderStyle.Rounded, title: "Terminal"),
            splitPosition: 22));
    }

    // The panes scroll inside their own frames, so fill the surrounding (borderless) host frame's viewport rather
    // than balloon to content height (mirrors MultiTabCodeEditor).
    protected override bool FillsFrameViewport => true;

    // When focus resolves up to this workspace (Ctrl-nav into the pane, or a click on the terminal — the one plain
    // control the composite owns), delegate to the terminal. The editor is a self-contained composite that
    // click-to-focus resolves to directly, so it doesn't route through here.
    protected override Control? FocusChild => terminal;

    #region IExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Tabbed Code Editor";
    string IExample.Description =>
        "A VS-Code-style editor group over a live terminal: closable tabs, syntax highlighting, and a draggable divider to the shell below.";

    // Show the two controls this example composes rather than the CompositeControl framework base.
    IReadOnlyList<string> IExample.SourceFiles => ["MultiTabEditorExample.cs", "MultiTabCodeEditor.cs", "TerminalEmulator.cs"];
    #endregion

    #region IActivatableExample
    // Start the shell only while shown and stop it when navigated away, so browsing other examples doesn't leave a
    // background shell running (the standalone IDE demo keeps its terminal for the app's lifetime; a browsable
    // example shouldn't). The terminal's process lifecycle is decoupled from layout, so restarting is a fresh shell.
    void IActivatableExample.OnActivated() => terminal.StartProcess();
    void IActivatableExample.OnDeactivated() => terminal.StopProcess();
    #endregion

    #region Fields
    private readonly MultiTabCodeEditor editor;
    private readonly TerminalEmulator terminal;
    #endregion
}
