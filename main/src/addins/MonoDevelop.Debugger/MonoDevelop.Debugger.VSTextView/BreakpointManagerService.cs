using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Debugger
{
	static class BreakpointManagerService
	{
		public static BreakpointManager GetBreakpointManager (ITextView textView)
		{
			return textView.Properties.GetOrCreateSingletonProperty (delegate {
				var manager = new BreakpointManager (textView.TextBuffer);
				textView.Closed += delegate { manager.Dispose (); };
				return manager;
			});
		}
	}
}
