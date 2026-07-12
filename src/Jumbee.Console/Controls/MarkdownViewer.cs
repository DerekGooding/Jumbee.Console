namespace Jumbee.Console;

using System;
using System.Threading.Tasks;

using ConsoleGUI.Input;
using ConsoleGUI.Space;

using NTokenizers.Extensions.Spectre.Console;
using NTokenizers.Extensions.Spectre.Console.Styles;

/// <summary>
/// A read-only, scrollable Markdown viewer. Renders CommonMark — headings, bold/italic, block-quotes, ordered and
/// unordered lists, links, syntax-highlighted fenced code blocks, and box-drawn tables — via NTokenizers'
/// Spectre.Console markdown writer. Wrap it in a <see cref="ControlFrame"/> (e.g. <c>viewer.WithFrame()</c>) to get a
/// border, title and scrollbar; ↑/↓, PgUp/PgDn, Home/End and the mouse wheel scroll it.
/// <para>
/// The markdown parse/render is comparatively slow, so it runs on a background thread: setting <see cref="Markdown"/>
/// or resizing never blocks the UI thread, and the view fills in when the render completes. Content is re-rendered
/// only when the text/styles change or the width changes (it reflows to the control width).
/// </para>
/// </summary>
public class MarkdownViewer : Control
{
    #region Constructors
    public MarkdownViewer(string markdown = "") => _markdown = markdown ?? "";
    #endregion

    #region Properties
    /// <summary>The Markdown source. Setting it re-renders (off the UI thread) and re-lays-out.</summary>
    public string Markdown
    {
        get => _markdown;
        set => UI.Invoke(() =>
        {
            var v = value ?? "";
            if (v == _markdown) return;
            _markdown = v;
            _version++;
            Initialize();
        });
    }

    /// <summary>The render styles (heading / code / table colours, …). Defaults to <see cref="MarkdownStyles.Default"/>.</summary>
    public MarkdownStyles? Styles
    {
        get => _styles;
        set => UI.Invoke(() => { _styles = value; _version++; Initialize(); });
    }

    public override bool HandlesInput => true;
    #endregion

    #region Methods
    // A viewer paints its own content; don't overlay the themed focus tint over the whole document.
    protected override bool RendersOwnFocus => true;

    protected internal override HelpInfo? GetHelpInfo() => new HelpInfo("Markdown", "Markdown Viewer",
        "A read-only, scrollable Markdown viewer.")
        .WithKey("↑ / ↓", "Scroll a line")
        .WithKey("PgUp / PgDn", "Scroll a page")
        .WithKey("Home / End", "Top / bottom");

    // Our content height at this width. Rendering (once per width/text) yields the true height; until the render for
    // this width is ready, report a rough estimate so a surrounding frame allocates a sensible height (replaced when
    // the render completes and re-lays-out). Consulted only when the parent leaves the height unbounded (scrolling).
    protected override int MeasureHeight(int width)
    {
        var w = Math.Max(1, width);
        EnsureRender(w);
        return _renderedWidth == w && _renderedVersion == _version
            ? Math.Max(1, _contentHeight)
            : EstimateHeight();
    }

    protected override void Render()
    {
        EnsureRender(Math.Max(1, Size.Width));
        consoleBuffer.Initialize();
        Blit();
    }

    protected override void OnInput(InputEvent inputEvent)
    {
        if (Frame is not { } frame) return;
        var page = Math.Max(1, frame.ViewportSize.Height - 1);
        switch (inputEvent.Key.Key)
        {
            case ConsoleKey.DownArrow: frame.Scroll(1); inputEvent.Handled = true; break;
            case ConsoleKey.UpArrow: frame.Scroll(-1); inputEvent.Handled = true; break;
            case ConsoleKey.PageDown: frame.Scroll(page); inputEvent.Handled = true; break;
            case ConsoleKey.PageUp: frame.Scroll(-page); inputEvent.Handled = true; break;
            case ConsoleKey.Home: frame.Top = 0; inputEvent.Handled = true; break;
            case ConsoleKey.End: frame.Top = int.MaxValue / 2; inputEvent.Handled = true; break;   // clamps to the bottom
        }
    }

    // Kicks off (or reuses) a render at `width` for the current text/styles. Runs on a background thread while the UI
    // is live so a slow parse never stalls a frame; runs synchronously when headless (tests / first layout) so the
    // result is available immediately to the MeasureHeight that requested it.
    private void EnsureRender(int width)
    {
        if (width <= 0) return;
        if (_renderedWidth == width && _renderedVersion == _version) return;   // already up to date
        if (_renderingWidth == width && _renderingVersion == _version) return; // already in flight for this

        var version = _version;
        var text = _markdown;
        var styles = _styles ?? MarkdownStyles.Default;
        _renderingWidth = width;
        _renderingVersion = version;

        if (!UI.IsRunning)
        {
            var (buffer, height) = RenderMarkdown(text, styles, width);
            Apply(buffer, height, width, version, relayout: false);   // caller reads _contentHeight straight back
            return;
        }

        Task.Run(() =>
        {
            var (buffer, height) = RenderMarkdown(text, styles, width);
            UI.Post(() => Apply(buffer, height, width, version, relayout: true));
        });
    }

    private void Apply(ConsoleBuffer buffer, int height, int width, int version, bool relayout)
    {
        _renderingWidth = -1;
        _renderingVersion = -1;
        // Discard a render superseded by a newer text/style (a fresh one is kicked off by the relayout / next measure).
        if (version == _version && width > 0)
        {
            _content = buffer;
            _contentHeight = Math.Max(1, height);
            _renderedWidth = width;
            _renderedVersion = version;
        }
        if (relayout) { Initialize(); Invalidate(); }
    }

    /// <summary>Discards the cached render so the next layout re-renders — for a subclass that adds render inputs
    /// (e.g. diagram styles) beyond <see cref="Markdown"/>/<see cref="Styles"/>. Call on the UI thread.</summary>
    protected void InvalidateContent() { _version++; Initialize(); }

    // Renders the markdown into a fresh offscreen buffer at `width` and returns it with its measured content height.
    // Resilient: any failure in the third-party writer yields a blank buffer rather than throwing. Instance-virtual so
    // a subclass can post-process the document (e.g. rasterize embedded diagrams); the base body uses only its
    // arguments (no instance state), so it stays safe to call on the background render thread.
    protected virtual (ConsoleBuffer buffer, int height) RenderMarkdown(string text, MarkdownStyles styles, int width)
    {
        var cap = Math.Clamp(LineCount(text) * 3 + 40, 8, MaxRows);
        var buffer = new ConsoleBuffer { Size = new Size(width, cap) };
        buffer.Initialize();
        try
        {
            var console = new AnsiConsoleBuffer(buffer);
            console.WriteMarkdown(text, styles);
            return (buffer, MeasureRenderedHeight(buffer));
        }
        catch
        {
            return (buffer, 1);
        }
    }

    // The last non-blank row + 1 — the true rendered height (the writer's cursor isn't a reliable end marker across
    // every renderable, so scan the buffer).
    private static int MeasureRenderedHeight(ConsoleBuffer buffer)
    {
        for (var y = buffer.Size.Height - 1; y >= 0; y--)
            for (var x = 0; x < buffer.Size.Width; x++)
            {
                var c = buffer[x, y].Character.Content;
                if (c is not null && c != ' ' && c != '\0') return y + 1;
            }
        return 1;
    }

    private void Blit()
    {
        var src = _content;
        var h = Math.Min(consoleBuffer.Size.Height, src.Size.Height);
        var w = Math.Min(consoleBuffer.Size.Width, src.Size.Width);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                consoleBuffer.Write(new Position(x, y), src[x, y]);
    }

    private int EstimateHeight() => Math.Clamp(LineCount(_markdown), 1, MaxRows);

    private static int LineCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 1;
        var n = 1;
        foreach (var c in text) if (c == '\n') n++;
        return n;
    }
    #endregion

    #region Fields
    // The rendered content is capped at this many rows — beyond the control's own ~1000-row size clamp nothing is
    // reachable anyway, so a taller document simply clips at the bottom.
    protected const int MaxRows = 1024;

    private string _markdown;
    private MarkdownStyles? _styles;
    private int _version;                       // bumped when the text/styles change, invalidating a cached render

    private ConsoleBuffer _content = new();     // the last completed render, blitted into consoleBuffer each paint
    private int _contentHeight;
    private int _renderedWidth = -1;            // (width, version) the cached _content was rendered for
    private int _renderedVersion = -1;
    private int _renderingWidth = -1;           // (width, version) a render is currently in flight for, or -1
    private int _renderingVersion = -1;
    #endregion
}
