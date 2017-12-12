namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using System.Windows;
    using Microsoft.VisualStudio.Core.Imaging;
    using Microsoft.VisualStudio.Imaging;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;
	using UIElement = Xwt.Widget;

	[Export(typeof(IViewElementFactory))]
    [Name("default ImageElement to UIElement")]
    [TypeConversion(from: typeof(ImageElement), to: typeof(UIElement))]
    [Order]
    internal sealed class WpfImageElementViewElementFactory : IViewElementFactory
    {
        public TView CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            // Should never happen if the service's code is correct, but it's good to be paranoid.
            if (typeof(TView) != typeof(UIElement) || !(model is ImageElement element))
            {
                throw new ArgumentException($"Invalid type conversion. Unsupported {nameof(model)} or {nameof(TView)} type");
            }

			var imageElement = new Xwt.ImageView ();

			imageElement.Image = MonoDevelop.Ide.ImageService.GetIcon ("md-monodevelop");

			// Add a slight margin so we don't contact any ClassifiedTextElements directly following us.
			imageElement.Margin = new Xwt.WidgetSpacing (4, 4, 4, 4);
			imageElement.HorizontalPlacement = Xwt.WidgetPlacement.Start;
			imageElement.HorizontalPlacement = Xwt.WidgetPlacement.Start;

			return imageElement as TView;
        }
    }
}
