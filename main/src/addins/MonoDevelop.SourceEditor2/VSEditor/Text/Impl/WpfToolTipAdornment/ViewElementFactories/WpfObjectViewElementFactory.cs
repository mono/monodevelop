namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
	using System;
	using System.ComponentModel.Composition;
	using System.Diagnostics;
	using Microsoft.VisualStudio.Text.Adornments;
	using Microsoft.VisualStudio.Text.Classification;
	using Microsoft.VisualStudio.Text.Editor;
	using Microsoft.VisualStudio.Text.Utilities;
	using Microsoft.VisualStudio.Utilities;
	using UIElement = Xwt.Widget;

	[Export (typeof (IViewElementFactory))]
	[Name ("default object to Xwt.Widget")]
	[TypeConversion (from: typeof (object), to: typeof (UIElement))]
	[Order]
	internal sealed class WpfObjectViewElementFactory : IViewElementFactory
	{
		public TView CreateViewElement<TView> (ITextView textView, object model) where TView : class
		{
			// Should never happen if the service's code is correct, but it's good to be paranoid.
			if (typeof (UIElement) != typeof (TView)) {
				throw new ArgumentException ($"Invalid type conversion. Unsupported {nameof (model)} or {nameof (TView)} type");
			}

			string text;

			if (model is ITextBuffer modelBuffer) {
				//TODO: This is very simplified compared to VSWindows
				//which supports classification of ITextBuffer
				//but our editor is not ready yet to be used like VSWindows editor.
				text = modelBuffer.CurrentSnapshot.GetText ();
			} else {
				text = model.ToString ();
			}

			return new Xwt.Label (text) as TView;
		}
	}
}
