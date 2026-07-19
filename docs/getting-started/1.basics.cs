#:package Jumbee.Console@*

using Jumbee.Console;

//Import static color names
using static Jumbee.Console.Color; 

var count = 0;

var label = new TextLabel(TextLabelOrientation.Horizontal, "Count: 0", Cyan1);
var button = new Button("Increment");

button.Activated += (_, _) =>
{
	count++;
	label.Text = $"Count: {count}";
};

// Arrange the two controls in a grid: one column, two rows
var root = new Grid(
	columnWidths: [30],
	rowHeights: [1, 3],
	controls:
	[
		[label],
[button.WithRoundedBorder(Grey50)],   // wrap the button in a rounded border
	]);

// Esc quits (Ctrl+Q already does by default)
UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);

// Focus the button on startup so Enter/Space activates it.
UI.SetFocus(button);

// Start the UI. Mouse/hover need a VtInputSource; keyboard works without one.
var t = UI.Start(root, width: 34, height: 6, input: new VtInputSource(anyMotion: true));
// Wait till the UI stops.
t.Wait();
