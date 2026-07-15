namespace Jumbee.Console.Examples;

using System.Collections.Generic;

/// <summary>
/// A sampler of button looks — the two shapes, the primary/secondary roles, colour fills, fixed widths, and the
/// toggle buttons. Every one is wired to report what you pressed in the status line at the bottom.
/// </summary>
public sealed class ButtonExample : CompositeControl, IExample
{
    public ButtonExample()
    {
        // Radio buttons latch on rather than toggling, so the group has to turn the others off. (A RadioSet does
        // this for you — see the Form Controls example.)
        foreach (var radio in radios)
            radio.Changed += (sender, isChecked) =>
            {
                if (!isChecked || sender is not RadioButton chosen) return;
                foreach (var other in radios)
                    if (!ReferenceEquals(other, chosen)) other.IsChecked = false;
                Report($"density: {chosen.Text}");
            };

        SetContent(new VerticalStackPanel(
            Header("Roles — Button.Primary / Button.Secondary, in the default flat shape"),
            new Grid([2], [24, 24],
            [
                [Wire(Button.Primary("Save")), Wire(Button.Secondary("Cancel"))],
            ]),

            Header("Shapes — ButtonStyle.WithShape(Modern): a raised, bevelled 3-row tile"),
            new Grid([4], [24, 24],
            [
                [Wire(Modern(Button.Primary("Save"))), Wire(Modern(Button.Secondary("Cancel")))],
            ]),

            Header("Colours — ButtonStyle.WithColors(normal, hover, press): hover and press one"),
            new Grid([4, 4], [18, 18, 18],
            [
                [Wire(Tinted("Publish", Green)), Wire(Tinted("Delete", Red)),  Wire(Tinted("Retry", Orange))],
                [Wire(Tinted("Merge", Purple)),  Wire(Tinted("Sync", Cyan)),   Wire(Tinted("Details", Grey))],
            ]),

            Header("Sizes — ButtonStyle.WithWidth(cells); MinWidth pads out a short label"),
            new Grid([2], [12, 20, 26],
            [
                [Wire(Sized("OK", 8)), Wire(Sized("Apply", 16)), Wire(Sized("Save and close", 22))],
            ]),

            Header("Toggle buttons — Checkbox and Switch flip; RadioButton latches on"),
            new Grid([1, 1, 2], [26, 22],
            [
                [Wire(new Checkbox("Word wrap", true)), radios[0]],
                [Wire(new Checkbox("Line numbers")),    radios[1]],
                [Wire(new Switch("Dark theme", true)),  radios[2]],
            ]),

            status));
    }

    // Buttons activate on a click OR on Enter/Space while focused — Activated covers both (Control.Clicked is
    // mouse-only). Tab moves focus between them.
    private Button Wire(Button button)
    {
        button.Activated += (_, _) => Report($"{button.Text} pressed");
        return button;
    }

    private T Wire<T>(T toggle) where T : ToggleButton
    {
        toggle.Changed += (_, isOn) => Report($"{toggle.Text}: {(isOn ? "on" : "off")}");
        return toggle;
    }

    private void Report(string what) => status.Text = "▸ " + what;

    // Opting a themed button into the raised tile: the shape is one field of its ButtonStyle, so the theme's
    // colours are kept.
    private static Button Modern(Button button)
    {
        button.Style = button.Style.WithShape(ButtonShape.Modern);
        return button;
    }

    // A tile in an arbitrary accent colour. Only the fills differ per state — the bevel edges are derived from the
    // fill background, so one colour is enough to describe the whole button.
    private static Button Tinted(string text, Color color) => new Button(text)
    {
        Style = new ButtonStyle(
            normal: Style.White | Style.Bg(color.Darken(0.25)),
            hover:  Style.White | Style.Bg(color),
            press:  Style.White | Style.Bg(color.Lighten(0.25)),
            shape: ButtonShape.Modern,
            minWidth: 14),
    };

    // A fixed outer width, overriding the label-sized default.
    private static Button Sized(string text, int width)
    {
        var button = Button.Primary(text);
        button.Style = button.Style.WithWidth(width);
        return button;
    }

    private static TextLabel Header(string text) =>
        new TextLabel(TextLabelOrientation.Horizontal, text, HeaderColor) { Focusable = false };

    #region IExample
    string IExample.Category => "Controls";
    string IExample.Title => "Buttons";
    string IExample.Description =>
        "Button shapes, roles, colours and sizes, plus the toggle buttons — click one or focus it with Tab and press Enter.";
    IReadOnlyList<string> IExample.SourceFiles => ["ButtonExample.cs", "Button.cs", "ButtonStyle.cs"];
    #endregion

    #region Fields
    private readonly TextLabel status = new TextLabel(TextLabelOrientation.Horizontal, "▸ press a button…", StatusColor);

    private readonly RadioButton[] radios =
    [
        new RadioButton("Compact", true),
        new RadioButton("Comfortable"),
        new RadioButton("Spacious"),
    ];

    private static readonly Color HeaderColor = new(0x9a, 0xc8, 0xff);   // soft blue section headers
    private static readonly Color StatusColor = new(0x8f, 0xd0, 0x66);
    private static readonly Color Green = new(0x8f, 0xd0, 0x66);
    private static readonly Color Red = new(0xe0, 0x6c, 0x6c);
    private static readonly Color Orange = new(0xe0, 0xa0, 0x50);
    private static readonly Color Purple = new(0xc8, 0x92, 0xf0);
    private static readonly Color Cyan = new(0x5c, 0xc8, 0xc8);
    private static readonly Color Grey = new(0x6a, 0x70, 0x84);
    #endregion
}
