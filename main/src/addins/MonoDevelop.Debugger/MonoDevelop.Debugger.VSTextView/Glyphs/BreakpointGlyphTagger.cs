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
		readonly BreakpointManager breakpointManager;

		public BreakpointGlyphTagger (ITextDocumentFactoryService textDocumentFactoryService, ITextView textView, BreakpointManager breakpointManager)
		{
			this.breakpointManager = breakpointManager;
			this.textView = textView;
			this.textDocumentFactoryService = textDocumentFactoryService;
			breakpointManager.BreakpointsChanged += BreakpointManager_BreakpointsChanged;
			this.textView.Closed += (s, e) => this.breakpointManager.BreakpointsChanged -= BreakpointManager_BreakpointsChanged;
		}

		void BreakpointManager_BreakpointsChanged (object sender, SnapshotSpanEventArgs e)
		{
			TagsChanged?.Invoke (this, e);
		}

		public IEnumerable<ITagSpan<BaseBreakpointGlyphTag>> GetTags (NormalizedSnapshotSpanCollection spans)
		{
			foreach (var breakpointTag in breakpointManager.GetBreakpoints(spans[0].Snapshot)) {
				var span= breakpointTag.Span;
				if (spans.IntersectsWith (span)) {
					var breakpoint = breakpointTag.Breakpoint;
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
