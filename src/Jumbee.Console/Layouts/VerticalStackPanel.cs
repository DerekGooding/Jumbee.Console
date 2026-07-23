namespace Jumbee.Console;
/// <summary>A layout that arranges its child controls in a single vertical column.</summary>
public class VerticalStackPanel : Layout<ConsoleGUI.Controls.VerticalStackPanel>
{
    /// <summary>Initializes a new <see cref="VerticalStackPanel"/> containing the given <paramref name="controls"/>.</summary>
    public VerticalStackPanel(params IFocusable[]? controls) : base(new ConsoleGUI.Controls.VerticalStackPanel())
    {
        if (controls != null)
        {
            foreach (var control in controls)
            {
                this.control.Add(control);
            }
        }
    }

    /// <summary>Appends the given <paramref name="controls"/> to the column.</summary>
    public void Add(params IFocusable[] controls)
    {
        foreach (var control in controls)
        {
            this.control.Add(control);
        }
    }

    /// <summary>Removes the given <paramref name="controls"/> from the column.</summary>
    public void Remove(params IFocusable[] controls)
    {
        foreach (var control in controls)
        {
            this.control.Remove(control);
        }
    }

    /// <summary>Number of rows, i.e. the child count.</summary>
    public override int Rows => control.Children.Count();

    /// <summary>Number of columns in the layout grid (always 1).</summary>
    public override int Columns => 1;

    /// <summary>Gets the control at the given <paramref name="row"/> (<paramref name="column"/> must be 0).</summary>
    public override IFocusable this[int row, int column]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(column, 0);
            return (IFocusable)control.Children.ElementAt(row);
        }
    }
}