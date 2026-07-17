namespace Jumbee.Console.AgentHarnessDemo;

using Jumbee.Console;

/// <summary>
/// The warm, near-monochrome dark palette of the Claude desktop app — a soft charcoal ground with a coral accent,
/// distinct from Jumbee's default blue-grey theme. Colours are shared by the theme and by the demo's controls that
/// paint their own surfaces (sidebar, panel headers, chips).
/// </summary>
internal static class Palette
{
    // Grounds — the app is layered charcoals, warmest in the centre chat column.
    public static readonly Color WindowBg = new(0x1c, 0x1b, 0x1a);   // outermost frame / gutters
    public static readonly Color SidebarBg = new(0x21, 0x20, 0x1e);  // left rail
    public static readonly Color ChatBg = new(0x26, 0x26, 0x24);     // centre transcript column
    public static readonly Color PanelBg = new(0x2b, 0x2a, 0x28);    // raised panels (right rail, input)
    public static readonly Color RaisedBg = new(0x33, 0x32, 0x2f);   // chips, selected rows, buttons
    public static readonly Color Border = new(0x3a, 0x39, 0x36);     // panel borders / dividers

    // Text.
    public static readonly Color Text = new(0xe6, 0xe3, 0xd9);       // primary, warm off-white
    public static readonly Color TextMuted = new(0x9a, 0x97, 0x8c);  // secondary
    public static readonly Color TextFaint = new(0x6c, 0x69, 0x60);  // tertiary / disabled

    // Accents.
    public static readonly Color Coral = new(0xd9, 0x77, 0x57);      // Claude's signature accent (prompt, links, active)
    public static readonly Color CoralDim = new(0xb0, 0x5f, 0x44);
    public static readonly Color Green = new(0x7c, 0xb3, 0x77);      // additions / success
    public static readonly Color Red = new(0xd0, 0x6a, 0x5a);        // deletions / errors
    public static readonly Color Blue = new(0x7f, 0xa6, 0xd6);       // secondary info
    public static readonly Color Yellow = new(0xd6, 0xb0, 0x6a);     // warnings
}

/// <summary>Applies the <see cref="Palette"/> to Jumbee's semantic style tokens. Set as the active theme
/// (<c>UI.StyleTheme = new ClaudeDarkTheme()</c>) before any control is constructed so each captures it.</summary>
internal sealed class ClaudeDarkTheme : IStyleTheme
{
    public Style Text => Palette.Text;
    public Style TextMuted => (Style)Palette.TextMuted | Style.Dim;
    public Style TextAccent => Palette.Coral;
    public Style TextDisabled => (Style)Palette.TextFaint | Style.Dim;

    public Style Surface => Style.Bg(Palette.PanelBg);
    public Style BorderText => Palette.Border;
    public Style BorderFocusedText => Palette.Coral;
    public Style TitleText => (Style)Palette.TextMuted;

    public Style Selection => (Style)Palette.Text | Style.Bg(Palette.RaisedBg);
    public Style Hover => Style.Bg(Palette.RaisedBg);
    public Style Focus => Style.Bg(new Color(0x35, 0x33, 0x30));

    public Style Primary => (Style)Color.White | Style.Bg(Palette.CoralDim);
    public Style PrimaryHover => (Style)Color.White | Style.Bg(Palette.Coral);
    public Style PrimaryActive => (Style)Color.White | Style.Bg(Palette.Coral);
    public Style Secondary => (Style)Palette.Text | Style.Bg(Palette.RaisedBg);
    public Style SecondaryHover => (Style)Color.White | Style.Bg(new Color(0x45, 0x43, 0x3f));

    public Style Success => Palette.Green;
    public Style Warning => Palette.Yellow;
    public Style Error => Palette.Red;
    public Style Info => Palette.Blue;

    public BorderStyle FrameBorder => BorderStyle.Rounded;
}
