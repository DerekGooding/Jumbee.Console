using NewsReaderDemo.Core;
using Jumbee.Console;

namespace NewsReaderDemo.App.Controls;

/// <summary>
/// The dominant bottom-right reading pane: a structured metadata header (title / date+feed+author / tags /
/// image placeholder) stacked above the article body.
/// </summary>
/// <remarks>
/// <para>
/// Each header line is a real <see cref="TextLabel"/> — literal text, not markup, per its XML doc ("Renders each
/// character into the label's cell buffer") — so nothing is markdown-parsed and nothing can leak. Only the
/// article BODY, which is genuinely free text, goes through <see cref="MarkdownViewer"/>.
/// </para>
/// <para>
/// <b>Round 5:</b> as of Jumbee.Console 0.1.4, <see cref="MarkdownViewer"/> word-wraps plain paragraph text to
/// its own width on its own, so the round-4 consumer-level pre-wrap workaround (and the resize re-wrap it
/// needed) is gone — <c>article.Body</c> is fed straight into <c>_body.Markdown</c> below.
/// </para>
/// <para>
/// This is also round 1's proof for the "single-child CompositeControl" gap noted then: a real multi-child
/// composite, built the documented way (child controls + an <see cref="ILayout"/>, then <c>SetContent</c>) per
/// the <see cref="CompositeControl"/> XML remarks.
/// </para>
/// </remarks>
public class ReaderPane : CompositeControl
{
    private readonly TextLabel _title = new(TextLabelOrientation.Horizontal, "Select an article to read it here.", Color.White);
    private readonly TextLabel _meta = new(TextLabelOrientation.Horizontal, "", Accent);
    private readonly TextLabel _tags = new(TextLabelOrientation.Horizontal, "", Color.Teal);
    private readonly TextLabel _image = new(TextLabelOrientation.Horizontal, "", Color.Grey);
    private readonly MarkdownViewer _body = new("*(no article selected)*");
    private Article? _current;

    private static Color Accent => Spectre.Console.Color.FromHex("#9370db");

    public ReaderPane()
    {
        var withImage = new DockPanel(DockedControlPlacement.Top, _image.WithHeight(2), _body);
        var withTags = new DockPanel(DockedControlPlacement.Top, _tags.WithHeight(1), withImage);
        var withMeta = new DockPanel(DockedControlPlacement.Top, _meta.WithHeight(1), withTags);
        var withTitle = new DockPanel(DockedControlPlacement.Top, _title.WithHeight(2), withMeta);
        SetContent(withTitle);
    }

    public void SetArticle(Article? article)
    {
        _current = article;
        if (article is null)
        {
            _title.Text = "Select an article to read it here.";
            _meta.Text = "";
            _tags.Text = "";
            _image.Text = "";
            RenderBody();
            return;
        }

        _title.Text = article.Title;
        var dateStr = article.Published?.ToString("yyyy-MM-dd HH:mm") ?? "unknown date";
        var author = string.IsNullOrWhiteSpace(article.Author) ? "" : $"  by {article.Author}";
        _meta.Text = $"{dateStr}  ·  {article.Feed.Title}{author}  ·  {article.Link}";
        _tags.Text = article.Tags.Count > 0 ? string.Join("   ", article.Tags.Select(t => $"#{t}")) : "";
        _image.Text = article.ImageUrl is { Length: > 0 } url ? $"[image]  {url}" : "(no image)";
        RenderBody();
    }

    private void RenderBody()
    {
        if (_current is not { } article)
        {
            _body.Markdown = "*(no article selected)*";
            return;
        }

        _body.Markdown = string.IsNullOrWhiteSpace(article.Body) ? "*(no content)*" : article.Body;
    }
}
