namespace Jumbee.Console.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Jumbee.Console;
using Jumbee.Console.Snapshot;

using Xunit;

public class ConcurrencyTests
{
    /// <summary>
    /// Phase B removed the global UI lock. This hammers a control with mutations from several background
    /// threads while a dispatcher thread renders it, asserting nothing tears or throws (the control's own
    /// concurrent collections / atomic writes must carry the safety the lock used to provide).
    /// </summary>
    [Fact]
    public void ConcurrentMutationDuringRender_DoesNotThrow()
    {
        var list = new ListBox { SelectedForegroundColor = Color.White, SelectedBackgroundColor = Color.Blue };
        for (var i = 0; i < 20; i++) list.AddItem($"Item {i}");
        list.WithRoundedBorder(Color.Blue).WithTitle("Live");

        Exception? renderError = null;
        var d = new Dispatcher();
        d.Start(() =>
        {
            try { ConsoleSnapshot.Render(list, 24, 10); }
            catch (Exception ex) { renderError = ex; }
        }, frameIntervalMs: 10);

        try
        {
            var mutators = Enumerable.Range(0, 4).Select(t => Task.Run(() =>
            {
                for (var i = 0; i < 300; i++)
                {
                    list.AddItem($"t{t}-{i}");
                    list.SelectedForegroundColor = (t % 2 == 0) ? Color.Red : Color.Green;
                }
            })).ToArray();

            Assert.True(Task.WaitAll(mutators, 10000), "Mutator tasks did not finish in time.");
            Thread.Sleep(50);   // let a few more frames render the final state
        }
        finally
        {
            d.Stop();
        }

        Assert.Null(renderError);
    }
}
