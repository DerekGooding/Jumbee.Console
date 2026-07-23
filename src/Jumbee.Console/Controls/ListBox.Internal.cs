using Spectre.Console;
using Spectre.Console.Rendering;

namespace Jumbee.Console;

public partial class ListBox
{
    /// <summary>
    /// An item in a ListBox.
    /// </summary>
    public class ListBoxItem
    {
        #region Constructors

        /// <summary>Initializes an item with the given renderable content at <paramref name="index"/> in <paramref name="listBox"/>.</summary>
        public ListBoxItem(ListBox listBox, int index, IRenderable content)
        {
            ListBox = listBox;
            Index = index;
            _content = content;
        }

        /// <summary>Initializes a text item with optional foreground/background colours at <paramref name="index"/> in <paramref name="listBox"/>.</summary>
        public ListBoxItem(ListBox listBox, int index, string text, Color? foreground = null, Color? background = null)
        {
            ListBox = listBox;
            Index = index;
            _text = text;
            _foregroundColor = foreground;
            _backgroundColor = background;
            UpdateTextContent();
        }

        #endregion Constructors

        #region Properties

        /// <summary>This item's stable index within its owning list.</summary>
        public readonly int Index;

        /// <summary>The list this item belongs to, or <see langword="null"/> once detached.</summary>
        public ListBox? ListBox { get; private set; }

        private IRenderable _content = default!;

        /// <summary>The renderable drawn for this item; setting it clears any text and re-measures the list.</summary>
        public IRenderable Content
        {
            get => _content;
            set
            {
                _content = value;
                _text = null;
                ListBox?.InvalidateLayout();   // content (and thus height) changed — re-measure
            }
        }

        private string? _text;

        /// <summary>The item's plain text, or <see langword="null"/> if it was created from a renderable.</summary>
        public string? Text
        {
            get => _text;
            set
            {
                _text = value;
                UpdateTextContent();
            }
        }

        /// <summary>Arbitrary application data associated with this item — e.g. the domain object it represents — so
        /// you can map a selected row back to your model without a side dictionary. Not used by the list.</summary>
        public object? Tag { get; set; }

        private Color? _foregroundColor;

        /// <summary>Foreground colour of a text item.</summary>
        public Color? ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                _foregroundColor = value;
                UpdateTextContent();
            }
        }

        private Color? _backgroundColor;

        /// <summary>Background colour of a text item.</summary>
        public Color? BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                UpdateTextContent();
            }
        }

        #endregion Properties

        #region Methods

        private void UpdateTextContent()
        {
            if (_text != null)
            {
                _content = new Markup(_text, new Spectre.Console.Style(_foregroundColor, _backgroundColor));
                ListBox?.InvalidateLayout();   // text/colour rebuilt the content — re-measure (height may change)
            }
        }

        /// <summary>Detaches the item from its owning list.</summary>
        public void Detach() => ListBox = null;

        /// <summary>Whether the item has been detached from its list.</summary>
        public bool IsDetached => ListBox is null;

        #endregion Methods
    }
}