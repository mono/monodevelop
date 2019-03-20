using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Mono.Debugging.Client;
using MonoDevelop.Debugger;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	internal abstract class AbstractBreakpointTagger<T> : ITagger<T> where T : ITag
	{
		private readonly ITextView textView;
		private readonly T tag;
		private readonly T disabled;
		private readonly T invalid;
		readonly BreakpointManager breakpointManager;

		public AbstractBreakpointTagger (
			T tag, T disabled, T invalid, ITextView textView, BreakpointManager breakpointManager)
		{
			this.breakpointManager = breakpointManager;
			this.textView = textView;
			this.tag = tag;
			this.disabled = disabled;
			this.invalid = invalid;
			breakpointManager.BreakpointsChanged += BreakpointManager_BreakpointsChanged;
			this.textView.Closed += (s, e) => this.breakpointManager.BreakpointsChanged -= BreakpointManager_BreakpointsChanged;
		}

		void BreakpointManager_BreakpointsChanged (object sender, SnapshotSpanEventArgs e)
		{
			TagsChanged?.Invoke (this, e);
		}



		public IEnumerable<ITagSpan<T>> GetTags (NormalizedSnapshotSpanCollection spans)
		{
			foreach (var breakpointTag in breakpointManager.GetBreakpoints(spans[0].Snapshot)) {
				var span = breakpointTag.Span;
				if (spans.IntersectsWith (span)) {
					var breakpoint = breakpointTag.Breakpoint;
					var status = DebuggingService.GetBreakpointStatus (breakpoint);
					if (breakpoint.Enabled)
						if (status == BreakEventStatus.Bound || status == BreakEventStatus.Disconnected)
							yield return new TagSpan<T> (span, tag);
						else
							yield return new TagSpan<T> (span, invalid);
					else
						yield return new TagSpan<T> (span, disabled);

				}

			}
		}

		public event System.EventHandler<SnapshotSpanEventArgs> TagsChanged;
	}
}
