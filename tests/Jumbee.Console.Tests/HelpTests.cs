namespace Jumbee.Console.Tests;

using System.Linq;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class HelpTests
{
    #region Compilation (GetHelpInfo + OnHelp + dedup)
    [Fact]
    public void CompileHelp_IncludesGeneralAndControlHelp()
    {
        _ = new Button("Go");
        _ = new ListBox("a", "b");

        var infos = UI.CompileHelp();

        Assert.Contains(infos, i => i.Name == "General");   // the built-in global-keys entry, always present
        Assert.Contains(infos, i => i.Name == "Button");    // from Button.GetHelpInfo
        Assert.Contains(infos, i => i.Name == "List");      // from ListBox.GetHelpInfo
    }

    [Fact]
    public void CompileHelp_DeduplicatesByName()
    {
        _ = new Button("One");
        _ = new Button("Two");

        var infos = UI.CompileHelp();

        Assert.Equal(1, infos.Count(i => i.Name == "Button"));   // many buttons -> a single shared tab
    }

    [Fact]
    public void OnHelp_SuppliesHelp_ForControlWithoutOverride()
    {
        // A TextLabel has no GetHelpInfo override; an OnHelp handler can still supply (create) help for it.
        var label = new TextLabel(TextLabelOrientation.Horizontal, "status");
        label.OnHelp += info => { info.Name = "StatusBar"; info.Title = "Status"; info.Text = "Shows the current status."; };

        var infos = UI.CompileHelp();

        var help = Assert.Single(infos, i => i.Name == "StatusBar");
        Assert.Equal("Status", help.Title);
        Assert.Equal("Shows the current status.", help.Text);
    }

    [Fact]
    public void OnHelp_ModifiesExistingHelp()
    {
        var button = new Button("Save");
        button.OnHelp += info => info.WithKey("Ctrl+S", "Save the file");   // augment the built-in Button help

        var info = button.CompileHelp();

        Assert.Equal("Button", info!.Name);                         // GetHelpInfo's entry, modified in place
        Assert.Contains(info.Keys, k => k.Keys == "Ctrl+S");        // the handler's added key
    }
    #endregion

    #region HelpControl rendering
    [Fact]
    public void HelpControl_RendersTabTitlesAndActiveText()
    {
        var infos = new[]
        {
            new HelpInfo("General", "General", "Global keys.").WithKey("F1", "Help"),
            new HelpInfo("Button", "Button", "A clickable button.").WithKey("Enter", "Activate"),
        };

        var help = new HelpControl(infos, onClose: () => { });
        var text = ConsoleSnapshot.ToText(help, 80, 24);

        Assert.Contains("General", text);          // tab header
        Assert.Contains("Button", text);           // tab header
        Assert.Contains("Global keys.", text);     // the active (first) tab's body
        Assert.Contains("F1", text);               // its key section
        Assert.Contains("Help", text);
    }

    [Fact]
    public void HelpControl_OpensOnTheRequestedTab()
    {
        var infos = new[]
        {
            new HelpInfo("General", "General", "Global keys here."),
            new HelpInfo("Editor", "Editor", "Editing keys here."),
        };

        var help = new HelpControl(infos, onClose: () => { }, initialTab: 1);
        var text = ConsoleSnapshot.ToText(help, 80, 24);

        Assert.Contains("Editing keys here.", text);          // the requested tab's body is shown
        Assert.DoesNotContain("Global keys here.", text);     // the first tab's body is not
    }
    #endregion
}
