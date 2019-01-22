using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Mono.Debugging.Client;
using MonoDevelop.Debugger;

namespace MonoDevelop.Debugger
{
	internal class BreakpointGlyphTagger : ITagger<BaseBreakpointGlyphTag>
	{
		private readonly ITextView textView;
		private readonly ITextDocumentFactoryService textDocumentFactoryService;

		public BreakpointGlyphTagger (ITextDocumentFactoryService textDocumentFactoryService, ITextView textView)
		{
			this.textView = textView;
			this.textDocumentFactoryService = textDocumentFactoryService;
			DebuggingService.Breakpoints.Changed += OnBreakpointsChanged;
			this.textView.Closed += (s, e) => DebuggingService.Breakpoints.Changed -= OnBreakpointsChanged;
		}

		private void OnBreakpointsChanged (object sender, EventArgs eventArgs)
		{
			if (TagsChanged != null) {
				var snapshot = textView.TextBuffer.CurrentSnapshot;
				var snapshotSpan = new SnapshotSpan (snapshot, 0, snapshot.Length);
				var args = new SnapshotSpanEventArgs (snapshotSpan);
				TagsChanged (this, args);
			}
		}

		public IEnumerable<ITagSpan<BaseBreakpointGlyphTag>> GetTags (NormalizedSnapshotSpanCollection spans)
		{
			var found = textDocumentFactoryService.TryGetTextDocument (textView.TextBuffer, out var document);
			if (!found || document.FilePath == null)
				yield break;

			foreach (var breakpoint in DebuggingService.Breakpoints.GetBreakpointsAtFile (document.FilePath)) {
				var span = textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber (breakpoint.Line - 1).Extent;
				if (spans.IntersectsWith (span)) {
					bool tracepoint = (breakpoint.HitAction & HitAction.Break) == HitAction.None;
					if (breakpoint.Enabled) {
						var status = DebuggingService.GetBreakpointStatus (breakpoint);
						if (status == BreakEventStatus.Bound || status == BreakEventStatus.Disconnected) {
							if (tracepoint)
								yield return new TagSpan<TracepointGlyphTag> (span, new TracepointGlyphTag (breakpoint));
							else
								yield return new TagSpan<BreakpointGlyphTag> (span, new BreakpointGlyphTag (breakpoint));
						} else {
							if (tracepoint)
								yield return new TagSpan<TracepointInvalidGlyphTag> (span, new TracepointInvalidGlyphTag (breakpoint));
							else
								yield return new TagSpan<BreakpointInvalidGlyphTag> (span, new BreakpointInvalidGlyphTag (breakpoint));
						}
					} else {
						if (tracepoint)
							yield return new TagSpan<TracepointDisabledGlyphTag> (span, new TracepointDisabledGlyphTag (breakpoint));
						else
							yield return new TagSpan<BreakpointDisabledGlyphTag> (span, new BreakpointDisabledGlyphTag (breakpoint));
					}
				}
			}
		}

		public event System.EventHandler<SnapshotSpanEventArgs> TagsChanged;
	}
}
