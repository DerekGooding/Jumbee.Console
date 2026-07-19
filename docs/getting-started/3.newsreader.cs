#:package Jumbee.Console@*

using System.Xml.Linq;

using Jumbee.Console;

// --- Fetch a few headlines from a public RSS feed ---
var items = new List<(string Title, string Summary)>();
try
{
    using var http = new HttpClient();
    var xml = await http.GetStringAsync("https://feeds.bbci.co.uk/news/rss.xml");
    foreach (var item in XDocument.Parse(xml).Descendants("item").Take(20))
        items.Add((item.Element("title")?.Value ?? "(no title)",
                   item.Element("description")?.Value ?? ""));
}
catch (Exception ex)
{
    items.Add(("Failed to fetch feed", ex.Message));
}

// Left: the scrollable headline list. Right: the selected story's summary. ---
// DockPanel pins one control to an edge and fills the rest with the other. Give the docked control an explicit size with .WithWidth(),
// Otherwise a docked control otherwise takes its intrinsic width.
var headlines = 
  new ListBox([.. items.Select(i => i.Title)])
      .WithWidth(40)
      .WithBorder(BorderStyle.Double)
      .WithTitle("Headlines");
var article = 
  new MarkdownViewer(items.Count > 0 ? items[0].Summary : "")
      .WithBorder(BorderStyle.Double)
      .WithTitle("Article");
// Keep the detail pane in sync with the selected row.
headlines.SelectionChanged += (_, _) =>
{
    var i = headlines.SelectedIndex;
    if (i >= 0 && i < items.Count) article.Markdown = items[i].Summary;
};

//
var root = new DockPanel(DockedControlPlacement.Left, headlines, article);

UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);
UI.SetFocus(headlines);   // arrow keys drive the list
// Wait for the UI to stop.
UI.Start(root, width: 100, height: 30, input: new VtInputSource(anyMotion: true)).Wait();