using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide;
using Mono.Debugging.Client;
using MonoDevelop.Core;

namespace MonoDevelop.Debugger
{
	internal class AbstractCurrentStatementTagger<T> : ITagger<T>, IDisposable
		where T : ITag
	{
		private readonly ITextBuffer textBuffer;
		private readonly T tag;
		private readonly string filePath;
		private readonly bool isGreen;
		private ITextSnapshot snapshotAtStartOfDebugging;

		public AbstractCurrentStatementTagger (T tag, ITextBuffer textBuffer, bool isGreen)
		{
			this.textBuffer = textBuffer;
			this.snapshotAtStartOfDebugging = textBuffer.CurrentSnapshot;
			this.filePath = textBuffer.GetFilePathOrNull ();
			this.tag = tag;
			this.isGreen = isGreen;
			DebuggingService.CurrentFrameChanged += OnDebuggerCurrentStatementChanged;
			DebuggingService.ExecutionLocationChanged += OnDebuggerCurrentStatementChanged;
			DebuggingService.DebugSessionStarted += OnDebugSessionStarted;
		}

		private void OnDebugSessionStarted (object sender, EventArgs e)
		{
			snapshotAtStartOfDebugging = textBuffer.CurrentSnapshot;
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
			if (!DebuggingService.IsPaused)
				yield break;
			if (isGreen) {
				if (DebuggingService.CurrentFrameIndex > 0) {
					var newTag = CreateTag ();
					if (newTag != null && spans.IntersectsWith (newTag.Span))
						yield return newTag;
				}
			} else {
				if (DebuggingService.CurrentFrameIndex == 0) {
					var newTag = CreateTag ();
					if (newTag != null && spans.IntersectsWith (newTag.Span))
						yield return newTag;
				}
			}
		}

		SourceLocation CheckLocationIsInFile (SourceLocation location)
		{
			if (!string.IsNullOrEmpty (filePath) && location != null && !string.IsNullOrEmpty (location.FileName)
				&& ((FilePath)location.FileName).FullPath == ((FilePath)filePath).FullPath)
				return location;
			return null;
		}

		private TagSpan<T> CreateTag ()
		{
			var sourceLocation = CheckLocationIsInFile (DebuggingService.NextStatementLocation)
					?? CheckLocationIsInFile (DebuggingService.CurrentFrame?.SourceLocation)
					?? CheckLocationIsInFile (DebuggingService.GetCurrentVisibleFrame ()?.SourceLocation);
			if (sourceLocation == null)
				return null;
			var span = snapshotAtStartOfDebugging.SpanFromMDColumnAndLine (sourceLocation.Line, sourceLocation.Column, sourceLocation.EndLine, sourceLocation.EndColumn);
			var translatedSpan = span.TranslateTo (textBuffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive);
			return new TagSpan<T> (translatedSpan, tag);
		}

		public void Dispose ()
		{
			DebuggingService.CurrentFrameChanged -= OnDebuggerCurrentStatementChanged;
			DebuggingService.ExecutionLocationChanged -= OnDebuggerCurrentStatementChanged;
			DebuggingService.DebugSessionStarted -= OnDebugSessionStarted;
		}

		public event System.EventHandler<SnapshotSpanEventArgs> TagsChanged;
	}
}
