#:package Jumbee.Console@*

using Jumbee.Console;

var clock = new Digits(DateTime.Now.ToString("HH:mm:ss")) { DigitStyle = Color.Green1 };

var root = new Grid(rowHeights: [3], columnWidths: [26], controls: [[clock]]);

_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(1000);
        clock.Text = DateTime.Now.ToString("HH:mm:ss");   // safe from any thread
    }
});

UI.RegisterHotKey(UI.HotKeys.Escape, UI.Stop);
UI.Start(root, width: 30, height: 5).Wait();