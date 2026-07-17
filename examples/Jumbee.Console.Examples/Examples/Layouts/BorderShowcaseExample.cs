namespace Jumbee.Console.Examples;

/// <summary>
/// A sampler of frame border options — every <see cref="BorderStyle"/>, a palette of border colours, and the
/// title-placement styles — so you can pick a look for a framed control (e.g. the tabbed editor).
/// </summary>
public sealed class BorderShowcaseExample : CompositeControl, IExample
{
    public BorderShowcaseExample()
    {
        SetContent(new VerticalStackPanel(
            Header("Border styles — Frame.BorderStyle / WithFrame(…)"),
            new Grid([4, 4], [19, 19, 18],
            [
                [Cell("soft corners",  BorderStyle.Rounded, "Rounded"),
                 Cell("sharp corners", BorderStyle.Square,  "Square"),
                 Cell("bold lines",    BorderStyle.Heavy,   "Heavy")],
                [Cell("double lines",  BorderStyle.Double,  "Double"),
                 Cell("portable +--+", BorderStyle.Ascii,   "Ascii"),
                 Bare("None — no border")],
            ]),

            Header("Border sides — Frame.BorderPlacement (draw some edges)"),
            new Grid([4, 4], [19, 19, 18],
            [
                [PlacementCell("Top",    top: true),
                 PlacementCell("Bottom", bottom: true),
                 PlacementCell("Left",   left: true)],
                [PlacementCell("Right",  right: true),
                 PlacementCell("Top+Bottom", top: true, bottom: true),
                 PlacementCell("Sides",  left: true, right: true)],
            ]),

            Header("Border colours — Frame.BorderFgColor"),
            new Grid([4, 4], [19, 19, 18],
            [
                [ColorCell("Blue",   new Color(0x5c, 0x9c, 0xff)),
                 ColorCell("Green",  new Color(0x8f, 0xd0, 0x66)),
                 ColorCell("Orange", new Color(0xe0, 0xa0, 0x50))],
                [ColorCell("Red",    new Color(0xe0, 0x6c, 0x6c)),
                 ColorCell("Purple", new Color(0xc8, 0x92, 0xf0)),
                 ColorCell("Cyan",   new Color(0x5c, 0xc8, 0xc8))],
            ]),

            Header("Title styles — Frame.TitleStyle (pos, banner, reverse)"),
            new Grid([5, 5], [19, 19, 18],
            [
                [TitleCell("Inline · left",   new TitleStyle(TitlePos.TopLeft,   TitleBorderStyle.Inline)),
                 TitleCell("Centered",        new TitleStyle(TitlePos.TopCenter, TitleBorderStyle.Inline)),
                 TitleCell("Right",           new TitleStyle(TitlePos.TopRight,  TitleBorderStyle.Inline))],
                [TitleCell("Banner",          new TitleStyle(TitlePos.TopLeft,   TitleBorderStyle.Double)),
                 TitleCell("Reverse",         new TitleStyle(TitlePos.TopCenter, TitleBorderStyle.Inline, TitleColorStyle.Reverse)),
                 TitleCell("Bottom",          new TitleStyle(TitlePos.BottomCenter, TitleBorderStyle.Inline))],
            ])
        ));
    }

    // A framed sample cell: `sample` text inside a `style` border, labelled with an inline `title`.
    private static IFocusable Cell(string sample, BorderStyle style, string title) =>
        Sample("  " + sample).WithFrame(borderStyle: style).WithTitle(title, InlineTitle);

    // A Rounded frame in a specific border colour, labelled with the colour name; content shows its RGB.
    private static IFocusable ColorCell(string name, Color color) =>
        Sample($"  {color.R},{color.G},{color.B}").WithFrame(borderStyle: BorderStyle.Rounded, borderFgColor: color)
            .WithTitle(name, InlineTitle);

    // A Rounded frame demonstrating a title-placement style (the title itself names the style).
    private static IFocusable TitleCell(string title, TitleStyle titleStyle) =>
        Sample("").WithFrame(borderStyle: BorderStyle.Rounded).WithTitle(title, titleStyle);

    // A Rounded frame drawing only the requested edges (Frame.BorderPlacement is a [Flags] set of sides). Labelled by
    // its content, since a missing top/bottom edge leaves nowhere for an inline title.
    private static IFocusable PlacementCell(string label, bool top = false, bool bottom = false, bool left = false, bool right = false)
    {
        var placement = BorderPlacement.None;
        if (top) placement |= BorderPlacement.Top;
        if (bottom) placement |= BorderPlacement.Bottom;
        if (left) placement |= BorderPlacement.Left;
        if (right) placement |= BorderPlacement.Right;

        var cell = Sample("  " + label).WithFrame(borderStyle: BorderStyle.Rounded);
        cell.Frame!.BorderPlacement = placement;
        return cell;
    }

    // An unframed cell (demonstrates BorderStyle.None: just content, no border).
    private static IFocusable Bare(string text) => Sample("  " + text);

    private static TextLabel Sample(string text) =>
        new TextLabel(TextLabelOrientation.Horizontal, text, ContentColor) { Focusable = false };

    private static TextLabel Header(string text) =>
        new TextLabel(TextLabelOrientation.Horizontal, text, HeaderColor) { Focusable = false };

    #region IExample
    string IExample.Category => "Layouts";
    string IExample.Title => "Frame Borders";
    string IExample.Description =>
        "Every frame BorderStyle, a palette of border colours, and the title-placement styles — a sampler for choosing a framed-control look.";
    #endregion

    #region Fields
    private static readonly TitleStyle InlineTitle = new(TitlePos.TopLeft, TitleBorderStyle.Inline);
    private static readonly Color HeaderColor = new(0x9a, 0xc8, 0xff);    // soft blue section headers
    private static readonly Color ContentColor = new(0x9a, 0xa6, 0xc0);   // muted body text
    #endregion
}
