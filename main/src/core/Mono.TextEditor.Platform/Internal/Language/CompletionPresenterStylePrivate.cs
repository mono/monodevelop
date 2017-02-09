using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.Text.Internal.Language
{
    public class CompletionPresenterStylePrivate : CompletionPresenterStyle
    {
        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the border/separator between completion tabs.
        /// </summary>
        public virtual Brush TabItemSeparatorBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of a completion tab item when it is
        /// selected.
        /// </summary>
        public virtual TextRunProperties TabItemSelectedTextRunProperties { get; protected set; }
        /// <summary>
        /// Creates a modified image which is themed to match the target background color for the
        /// completion UI.  The icon may be modified to improve its appearance regardless of
        /// whether or not the background is dark or light.
        /// </summary>
        /// <param name="sourceImage">The source image to theme.</param>
        /// <param name="backgroundColor">The background color which the image
        /// should be targeted to look good on.</param>
        /// <returns>A derivative work of the original source image, or a reference
        /// to the source image if theming is not supported or required.</returns>
        public virtual ImageSource ThemeImage(ImageSource sourceImage, Color backgroundColor)
        {
            return sourceImage;
        }
    }
}
