using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger.VSTextView.ExceptionCaught
{
	[Export (typeof (ICocoaTextViewCreationListener))]
	[ContentType ("text")]
	[TextViewRole (PredefinedTextViewRoles.Debuggable)]
	sealed class ExceptionCaughtProvider : ICocoaTextViewCreationListener
	{
		public void TextViewCreated (ICocoaTextView textView)
		{
			var manager = new ExceptionCaughtAdornmentManager (textView);
			textView.Closed += (s, e) => manager.Dispose ();
		}

		[Export]
		[Name ("ExceptionCaught")]
		[Order (After = PredefinedAdornmentLayers.Caret)]
		internal AdornmentLayerDefinition visibleWhitespaceLayer;
	}
}
