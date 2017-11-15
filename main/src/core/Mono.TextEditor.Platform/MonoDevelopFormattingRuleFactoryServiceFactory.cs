using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Platform
{
	[ExportWorkspaceServiceFactory (typeof (IHostDependentFormattingRuleFactoryService), ServiceLayer.Host), Shared]
	internal sealed class MonoDevelopFormattingRuleFactoryServiceFactory : IWorkspaceServiceFactory
	{
		public MonoDevelopFormattingRuleFactoryServiceFactory ()
		{
		}

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			return new Factory ();
		}

		private sealed class Factory : IHostDependentFormattingRuleFactoryService
		{
			private readonly IFormattingRule _noopRule = new NoOpFormattingRule ();

			public bool ShouldUseBaseIndentation (Document document)
			{
				return IsContainedDocument (document);
			}

			public bool ShouldNotFormatOrCommitOnPaste (Document document)
			{
				return IsContainedDocument (document);
			}

			private bool IsContainedDocument (Document document)
			{
				MonoDevelopContainedDocument containedDocument = MonoDevelopContainedDocument.FromDocument (document);

				return (containedDocument != null);
			}

			public IFormattingRule CreateRule (Document document, int position)
			{
				MonoDevelopContainedDocument containedDocument = MonoDevelopContainedDocument.FromDocument (document);
				if (containedDocument == null)
				{
					return _noopRule;
				}

				List<TextSpan> spans = new List<TextSpan>();

				var root = document.GetSyntaxRootSynchronously (CancellationToken.None);
				var text = root.SyntaxTree.GetText (CancellationToken.None);

				spans.AddRange (containedDocument.GetEditorVisibleSpans ());

				for (var i = 0; i < spans.Count; i++) {
					var visibleSpan = spans[i];
					if (visibleSpan.IntersectsWith (position) || visibleSpan.End == position) {
						return containedDocument.GetBaseIndentationRule (root, text, spans, i);
					}
				}

				// in razor (especially in @helper tag), it is possible for us to be asked for next line of visible span
				var line = text.Lines.GetLineFromPosition (position);
				if (line.LineNumber > 0) {
					line = text.Lines[line.LineNumber - 1];

					// find one that intersects with previous line
					for (var i = 0; i < spans.Count; i++) {
						var visibleSpan = spans[i];
						if (visibleSpan.IntersectsWith (line.Span)) {
							return containedDocument.GetBaseIndentationRule (root, text, spans, i);
						}
					}
				}

				throw new InvalidOperationException ();
			}

			public IEnumerable<TextChange> FilterFormattedChanges (Document document, TextSpan span, IList<TextChange> changes)
			{
				return changes;
			}
		}
	}
}