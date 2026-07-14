namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>
/// A VS-Code-style workspace: a tabbed <see cref="MultiTabCodeEditor"/> stacked over a live
/// <see cref="TerminalEmulator"/> in a vertical <see cref="SplitPanel"/> — closable tabs, syntax highlighting, and a
/// draggable divider to the shell below (ported from the IDE demo). Click either pane to direct input to it.
/// </summary>
public sealed class MultiTabEditorExample : CompositeControl, IActivatableExample
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

        // Neither pane has a frame — focus is shown the way VS Code does it, via the blinking cursor: the editor's
        // caret when it's focused, the terminal's cursor when it is. (The editor also scrolls in its own per-tab
        // viewports; the terminal owns its scrollback + scrollbar.) Hosting the split inside a composite — not a bare
        // SplitPanel — gives the terminal a focus root that delegates back to it, so click-to-focus reaches it instead
        // of dead-ending at the host (see FocusChild); the editor is a nested composite, so clicks resolve to it directly.
        SetContent(new SplitPanel(SplitOrientation.Vertical, editor, terminal, splitPosition: 22));

        // The example host wraps a Control example in a borderless frame that lights up (the theme's
        // FocusedFrameBorder) when the example contains focus — an outer box around the whole workspace we don't want
        // (the panes show focus via their cursors). Claim that frame up front with BorderPlacement.None so it draws
        // nothing, focused or not; the host's WithFrame reuses this frame rather than adding its own.
        this.WithFrame(borderStyle: BorderStyle.None, borderPlacement: BorderPlacement.None);        
    }

    // The panes scroll inside their own frames, so fill the surrounding (borderless) host frame's viewport rather
    // than balloon to content height (mirrors MultiTabCodeEditor).
    protected override bool FillsFrameViewport => true;

    // When focus resolves up to this workspace (Ctrl-nav into the pane, or a click on the terminal — the one plain
    // control the composite owns), delegate to the terminal. The editor is a self-contained composite that
    // click-to-focus resolves to directly, so it doesn't route through here.
    protected override Control? FocusChild => terminal;

    #region IActivatableExample
    string IExample.Category => "Editors and Viewers";
    string IExample.Title => "Tabbed Code Editor";
    string IExample.Description =>
        "A VS-Code-style editor group over a live terminal: closable tabs, syntax highlighting, and a draggable divider to the shell below.";

    // Show the two controls this example composes rather than the CompositeControl framework base.
    IReadOnlyList<string> IExample.SourceFiles => ["MultiTabEditorExample.cs", "MultiTabCodeEditor.cs", "TerminalEmulator.cs"];
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
