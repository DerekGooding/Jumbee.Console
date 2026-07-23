namespace Jumbee.Console;
/// <summary>One keystroke (or chord) and what it does, listed in a control's <see cref="HelpInfo"/>.</summary>
/// <param name="Keys">The key(s) as displayed, e.g. <c>"Ctrl+N"</c> or <c>"↑/↓"</c>.</param>
/// <param name="Description">What the key does.</param>
public sealed record KeyHelp(string Keys, string Description);

/// <summary>
/// A control's entry in the global help dialog (one tab). Mutable so an <see cref="Control.OnHelp"/> handler can
/// tweak it in place.
/// </summary>
/// <remarks>
/// Help is compiled by calling each control's <see cref="Control.GetHelpInfo"/> and deduplicated by
/// <see cref="Name"/> — one tab per distinct name (e.g. all buttons share a "Button" tab), and the focused
/// control's tab is shown first.
/// </remarks>
/// <remarks>Initializes a new <see cref="HelpInfo"/> with the given <paramref name="name"/>, and optional
/// <paramref name="title"/> (defaults to the name) and <paramref name="text"/>.</remarks>
public sealed class HelpInfo(string name, string? title = null, string? text = null)
{


    #region Properties

    /// <summary>Identity for deduplication and for matching the focused control's tab. Required, non-empty.</summary>
    public string Name { get; set; } = name;

    /// <summary>The tab header label. Defaults to <see cref="Name"/>.</summary>
    public string Title { get; set; } = title ?? name;

    /// <summary>Spectre markup shown in the panel (e.g. <c>"[bold]Save[/] the file"</c>).</summary>
    public string Text { get; set; } = text ?? "";

    /// <summary>Key bindings shown either inline in <see cref="Text"/> or as a separate section (see
    /// <see cref="KeysInline"/>). Mutable so handlers can add entries.</summary>
    public IList<KeyHelp> Keys { get; } = [];

    /// <summary>When <see langword="true"/>, the author has already woven the keys into <see cref="Text"/>, so the
    /// dialog does not append a separate "Keys" section. Defaults to <see langword="false"/> (separate section).</summary>
    public bool KeysInline { get; set; }

    #endregion Properties

    #region Methods

    /// <summary>Fluent helper to add a key binding (returns this).</summary>
    public HelpInfo WithKey(string keys, string description)
    {
        Keys.Add(new KeyHelp(keys, description));
        return this;
    }

    #endregion Methods
}