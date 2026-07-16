namespace Jumbee.Console.Examples;

using Spectre.Console;

/// <summary>
/// The examples browser landing page.
/// <see cref="SpectreControl{T}"/> Spectre panel.
/// </summary>
public sealed class WelcomeExample : SpectreControl<Panel>, IExample
{
    public WelcomeExample() : base(BuildPanel()) { }

    private static Panel BuildPanel()
    {
        var body = new Markup(
            """                                    
            [skyblue1 bold underline]About[/]
            Jumbee.Console is a .NET library for advanced TUIs that focuses on performance and usability. 
            Inspired by libraries like ratatui and  Textual,  it tries to provide a high-performance TUI framework that is easy-to-use 
            with idiomatic .NET GUI and Task patterns,  while flexible enough to create different types of TUI applications 
            from news readers to animated dashboards to IDEs to drawing apps.

            [skyblue1 bold underline]Features[/]            
            :check_mark: 100% managed AOT-compliant code.
            :check_mark: Retained-mode GUI framework with a modern API designed to be easy to use and extend.
            :rocket: Sub-ms frame rendering times and minimal CPU consumption even with multi-tab document editing and syntax highlighting.
            :check_mark: Supports both fixed-width layouts like `Grid` and flexible, resizable layouts like `DockPanel`, `HorizontalStack`, `VerticalStack`, resizable `SplitPanel`.
            :check_mark: Large set of controls from common GUI elemts:
                [#7CFC00]•[/]  Menus, Buttons, Trees, text inputs with autocomplete...
                [#7CFC00]•[/]  Modal dialogs
                [#7CFC00]•[/]  Control frames support adornments like titles, borders, margins, scrollbars with autoscrolling control content
            :check_mark: Cross-platform 100% managed code terminal-emulator
            :check_mark: Muti-tab editor that supports C#, JavaScript, C++, Markdown + a dozen other languages
            :check_mark: Split-pane interactive editors with preview for Markdown, AsciiDoc, Mermaid documents, Mermaid embedded in Markdown
            :bar_chart: Many different types of plots and graphs with support for animation
            :check_mark: Flexible themes that support styling both colors and glyphs independently

            [skyblue1 bold underline]Internals[/]
            Jumbee.Console combines the windowing and layout features of the ConsoleGUI [link]https://github.com/allisterb/C-sharp-console-gui-framework/tree/jumbee-console[/] retained-mode library with the text markup and rendering and layout capabilities of Spectre.Console[link]https://github.com/allisterb/spectre.console/tree/jumbee-console [/]. Both libraries have been modified to reduce allocations and increase performance along frame-rendering hotpaths. 

            [skyblue1 bold underline]Getting started[/]
            The simplest way to get started is to run the jumbee-cosole. 
            You can install the NuGet package. Jumbee.Console is the main package while Jumbee.Console.Documents contanis support for viewing PDF documents and viewing and editing AsciiDoc and Mermaid documents.
            """
            );
        return new Panel(body)
        {
            Header = new PanelHeader(" ◈ Welcome "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(foreground: Color.SkyBlue1),
            Padding = new Padding(2, 1, 2, 1),
            Expand = true,
        };
    }

    #region IExample
    string IExample.Category => "Welcome";
    string IExample.Title => "Jumbee.Console";
    string IExample.Description => "Jumbee.Console Examples Browser Landing Page";
    #endregion
}
