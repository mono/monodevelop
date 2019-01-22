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
		private readonly string file;

		public AbstractBreakpointTagger (
			T tag, T disabled, T invalid, ITextView textView)
		{
			this.textView = textView;
			this.tag = tag;
			this.disabled = disabled;
			this.invalid = invalid;
			file = this.textView.TextBuffer.GetFilePathOrNull ();
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

		public IEnumerable<ITagSpan<T>> GetTags (NormalizedSnapshotSpanCollection spans)
		{
			if (file == null)
				yield break;
			foreach (var breakpoint in DebuggingService.Breakpoints.GetBreakpointsAtFile (file)) {
				var snapshot = textView.TextBuffer.CurrentSnapshot;
				if (snapshot.LineCount <= breakpoint.Line)
					continue;
				var span = snapshot.GetLineFromLineNumber (breakpoint.Line - 1).Extent;
				if (spans.IntersectsWith (span)) {
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
