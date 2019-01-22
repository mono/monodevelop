using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger.VSTextView.QuickInfo
{
	[Export (typeof (IAsyncQuickInfoSourceProvider))]
	[Name ("MonodevelopDebugger")]
	[Order]
	[ContentType ("any")]
	class DebuggerQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
	{
		[ImportMany]
		internal IEnumerable<Lazy<IDebugInfoProvider>> debugInfoProviders = null;

		[Import]
		internal ITextDocumentFactoryService textDocumentFactoryService { get; set; }

		[Import]
		internal JoinableTaskContext joinableTaskContext;

		public IAsyncQuickInfoSource TryCreateQuickInfoSource (ITextBuffer textBuffer)
		{
			return new DebuggerQuickInfoSource (this, textBuffer);
		}
	}
}
