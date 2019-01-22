using System;
using System.Collections.Generic;
using Microsoft.Ide.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	internal class AbstractCurrentStatementTagger<T> : ITagger<T>, IDisposable
		where T : ITag
	{
		private readonly ITextBuffer textBuffer;
		private readonly T tag;
		private readonly string filePath;
		private readonly bool isGreen;

		public AbstractCurrentStatementTagger (T tag, ITextBuffer textBuffer, bool isGreen)
		{
			this.textBuffer = textBuffer;
			this.filePath = textBuffer.GetFilePathOrNull ();
			this.tag = tag;
			this.isGreen = isGreen;
			DebuggingService.CurrentFrameChanged += OnDebuggerCurrentStatementChanged;
		}

		private void OnDebuggerCurrentStatementChanged (object sender, EventArgs eventArgs)
		{
			var handler = TagsChanged;
			if (handler != null) {
				var snapshot = textBuffer.CurrentSnapshot;
				var snapshotSpan = new SnapshotSpan (snapshot, 0, snapshot.Length);
				var args = new SnapshotSpanEventArgs (snapshotSpan);
				handler (this, args);
			}
		}

		public IEnumerable<ITagSpan<T>> GetTags (NormalizedSnapshotSpanCollection spans)
		{
			if (DebuggingService.CurrentCallStack == null)
				yield break;
			if (isGreen) {
				if (DebuggingService.CurrentFrameIndex > 0) {
					var newTag = CreateTag (DebuggingService.CurrentFrameIndex);
					if (newTag != null && spans.IntersectsWith (newTag.Span))
						yield return newTag;
				}
			} else {
				if (DebuggingService.CurrentFrameIndex == 0) {
					var newTag = CreateTag (0);
					if (newTag != null && spans.IntersectsWith (newTag.Span))
						yield return newTag;
				}
			}
		}

		private TagSpan<T> CreateTag (int stackIndex)
		{
			var sourceLocation = DebuggingService.CurrentCallStack.GetFrame (stackIndex).SourceLocation;
			if (sourceLocation.FileName != filePath)
				return null;
			var span = textBuffer.CurrentSnapshot.SpanFromMDColumnAndLine (sourceLocation.Line, sourceLocation.Column, sourceLocation.EndLine, sourceLocation.EndColumn);
			return new TagSpan<T> (span, tag);
		}

		public void Dispose ()
		{
			DebuggingService.CurrentFrameChanged -= OnDebuggerCurrentStatementChanged;
		}

		public event System.EventHandler<SnapshotSpanEventArgs> TagsChanged;
	}
}
