namespace Jumbee.Console.Examples;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The everyday form controls in one pane — text fields, a type-ahead field, a drop-down, radio and check lists,
/// and a list box — each wired to report its value. Tab moves between them; the status line shows the last change.
/// </summary>
public sealed class FormControlsExample : CompositeControl, IExample
{
    public FormControlsExample()
    {
        // Type-ahead: matches float in a popup below the caret while the field keeps focus and keeps editing.
        // Up/Down highlight, Enter or Tab accepts, Escape dismisses. Attaching it is the whole wiring.
        new Autocomplete(language, Languages);

        name.Changed += (_, _) => Report($"name: {name.Text}");
        secret.Changed += (_, _) => Report($"password: {new string('•', secret.Text.Length)}");
        language.Changed += (_, _) => Report($"language: {language.Text}");
        theme.SelectionChanged += (_, value) => Report($"theme: {value}");
        density.SelectionChanged += (_, _) => Report($"density: {density.SelectedValue}");
        features.SelectionChanged += (_, _) => Report($"features: {Join(features.SelectedValues)}");
        files.SelectionChanged += (_, index) => Report($"file: {Files[index]}");

        SetContent(new VerticalStackPanel(
            Header("Text entry — TextInput: a placeholder hint, and PasswordChar to mask"),
            new Grid([4], [30, 30],
            [
                [Framed(name, "Name", Blue), Framed(secret, "Password", Blue)],
            ]),

            Header("Type-ahead & drop-down — Autocomplete over a TextInput · Select"),
            new Grid([4], [30, 30],
            [
                [Framed(language, "Language · type 'a'", Purple), Framed(theme, "Theme", Purple)],
            ]),

            Header("Choices — RadioSet picks one; SelectionList checks many (Space toggles)"),
            new Grid([7], [30, 30],
            [
                [Framed(density, "Density", Orange), Framed(features, "Features", Orange)],
            ]),

            Header("Lists — ListBox: click it to focus, then move the selection with the arrows"),
            Framed(files, "Recent files", Green).FocusableControl,

            status));
    }

    private void Report(string what) => status.Text = "▸ " + what;

    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "none" : string.Join(", ", values);

    // Every field is framed so its extent reads on screen — a TextInput fills whatever width it is given, and the
    // frame is what bounds it.
    private static T Framed<T>(T control, string title, Color color) where T : Control =>
        control.WithFrame(borderStyle: BorderStyle.Rounded, borderFgColor: color).WithTitle(title, InlineTitle);

    private static TextLabel Header(string text) =>
        new TextLabel(TextLabelOrientation.Horizontal, text, HeaderColor) { Focusable = false };

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "Form Controls";
    string IExample.Description =>
        "The everyday desktop controls — text fields, type-ahead, a drop-down, radio and check lists, and a list box — each reporting its value.";
    IReadOnlyList<string> IExample.SourceFiles =>
        ["FormControlsExample.cs", "TextInput.cs", "Autocomplete.cs", "Select.cs", "RadioSet.cs"];
    #endregion

    #region Fields
    private readonly TextInput name = new TextInput(placeholder: "Ada Lovelace");
    private readonly TextInput secret = new TextInput("hunter2") { PasswordChar = '•' };
    private readonly TextInput language = new TextInput(placeholder: "start typing…");

    // A Select shows its value with a ▼ marker and drops its options into the ambient overlay when opened.
    private readonly Select theme = new Select("Dark", "Light", "Solarized", "High contrast")
    {
        SelectedIndex = 0,
        Foreground = Color.White,
        Background = new Color(0x3a, 0x30, 0x52),
    };

    private readonly RadioSet density = new RadioSet("Compact", "Comfortable", "Spacious") { SelectedIndex = 1 };

    private readonly SelectionList features = new SelectionList("Word wrap", "Line numbers", "Minimap", "Autosave");

    private readonly ListBox files = new ListBox(Files);

    private readonly TextLabel status = new TextLabel(TextLabelOrientation.Horizontal, "▸ change something…", StatusColor);

    private static readonly string[] Files =
        ["Program.cs", "Control.cs", "ControlFrame.cs", "Layout.cs", "UI.cs", "Dispatcher.cs", "Theme.cs"];

    private static readonly string[] Languages =
        ["C#", "F#", "Ada", "Assembly", "Basic", "Haskell", "JavaScript", "Java", "Python", "Rust", "Scala", "TypeScript"];

    private static readonly TitleStyle InlineTitle = new(TitlePos.TopLeft, TitleBorderStyle.Inline);
    private static readonly Color HeaderColor = new(0x9a, 0xc8, 0xff);
    private static readonly Color StatusColor = new(0x8f, 0xd0, 0x66);
    private static readonly Color Blue = new(0x5c, 0x9c, 0xff);
    private static readonly Color Purple = new(0xc8, 0x92, 0xf0);
    private static readonly Color Orange = new(0xe0, 0xa0, 0x50);
    private static readonly Color Green = new(0x8f, 0xd0, 0x66);
    #endregion
}
