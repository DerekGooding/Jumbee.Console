namespace Jumbee.Console.Examples;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The everyday form controls in one pane — text fields, a type-ahead field, a drop-down, radio and check lists, and
/// both list-box highlight modes — each wired to report its value. Click or Tab between them; the status line shows
/// the last change.
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
        notifications.SelectionChanged += (_, index) => Report($"notification: {Cards[index].Title}");

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

            Header("Lists — ListBox highlighting: item-width (left) vs full-width cards (right)"),
            new Grid([10], [30, 30],
            [
                [Framed(files, "Text · item-width", Green), Framed(notifications, "Markup cards · full-width", Green)],
            ]),

            status));
    }

    // A form is several fields the user moves between, so Tab belongs to the form rather than to the focused field
    // (the default, which suits a composite built around one editor). Clicking a field focuses it either way.
    protected override bool TabNavigatesChildren => true;

    private void Report(string what) => status.Text = "▸ " + what;

    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "none" : string.Join(", ", values);

    // A list row can be any Spectre renderable, not just text — here an icon and a bold title over a grey detail
    // line. Two rows per card, so the full-width highlight has something to span.
    private static Spectre.Console.Rendering.IRenderable Card((string Icon, string Title, string Detail) card) =>
        new Spectre.Console.Markup(
            $"{card.Icon}  [bold]{Spectre.Console.Markup.Escape(card.Title)}[/]\n" +
            $"    [grey]{Spectre.Console.Markup.Escape(card.Detail)}[/]");

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
        "The everyday desktop controls — text fields, type-ahead, a drop-down, radio and check lists, and both list highlight modes — each reporting its value.";
    IReadOnlyList<string> IExample.SourceFiles =>
        ["FormControlsExample.cs", "TextInput.cs", "Autocomplete.cs", "Select.cs", "ListBox.cs"];
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

    // HighlightFullWidth makes the selected card a bar across every row it occupies; the default (left) highlights
    // only the item's own text.
    private readonly ListBox notifications = new ListBox(Cards.Select(Card).ToArray()) { HighlightFullWidth = true };

    private readonly TextLabel status = new TextLabel(TextLabelOrientation.Horizontal, "▸ change something…", StatusColor);

    private static readonly string[] Files =
        ["Program.cs", "Control.cs", "ControlFrame.cs", "Layout.cs", "UI.cs", "Dispatcher.cs", "Theme.cs"];

    private static readonly (string Icon, string Title, string Detail)[] Cards =
    [
        ("✉", "New message",  "from Ada · 2m ago"),
        ("⚠", "Build failed", "3 tests · main"),
        ("★", "Starred",      "Plot.cs"),
        ("✔", "Deployed",     "v1.4.2 · prod"),
    ];

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
