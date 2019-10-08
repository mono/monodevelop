using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Debugger
{
	class BreakpointManager
	{
		private ITextBuffer textBuffer;
		private ITextView textView;
		private readonly ITextDocument textDocument;

		public BreakpointManager (ITextView textView)
		{
			this.textBuffer = textView.TextBuffer;
			this.textView = textView;
			if (textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty (typeof (ITextDocument), out textDocument) && textDocument.FilePath != null) {
				textDocument.FileActionOccurred += TextDocument_FileActionOccurred;
			} else {
				LoggingService.LogWarning ("Failed to get filename of textbuffer, breakpoints integration will not work.");
				return;
			}
			textBuffer.Changed += TextBuffer_Changed;
			DebuggingService.Breakpoints.Changed += OnBreakpointsChanged;
			DebuggingService.Breakpoints.BreakpointStatusChanged += OnBreakpointsChanged;
			DebuggingService.Breakpoints.BreakpointModified += OnBreakpointsChanged;
			OnBreakpointsChanged (null, null);
		}

		private void TextDocument_FileActionOccurred (object sender, TextDocumentFileActionEventArgs e)
		{
			if (e.FileActionType == FileActionTypes.DocumentRenamed)
				OnBreakpointsChanged (null, null);
		}

		void TextBuffer_Changed (object sender, TextContentChangedEventArgs e)
		{
			foreach (var breakpoint in breakpoints.Values) {
				var span = breakpoint.TrackingSpan.GetSpan (e.After);

				if (span.IsEmpty || string.IsNullOrWhiteSpace (span.GetText ())) {
					DebuggingService.Breakpoints.Remove (breakpoint.Breakpoint);
					continue;
				}

				var newLineNumber = e.After.GetLineFromPosition (span.Start).LineNumber + 1;

				if (breakpoint.Breakpoint.Line != newLineNumber)
					DebuggingService.Breakpoints.UpdateBreakpointLine (breakpoint.Breakpoint, newLineNumber);
			}
		}

		class ManagerBreakpoint
		{
			public Breakpoint Breakpoint { get; set; }
			public ITrackingSpan TrackingSpan { get; set; }
			public Span Span { get; set; }
		}

		private Dictionary<Breakpoint, ManagerBreakpoint> breakpoints = new Dictionary<Breakpoint, ManagerBreakpoint> ();

		private async void OnBreakpointsChanged (object sender, EventArgs eventArgs)
		{
			var newBreakpoints = new Dictionary<Breakpoint, ManagerBreakpoint> ();
			var breakpointStore = DebuggingService.Breakpoints;
			var snapshot = textView.TextSnapshot;
			var bps = new List<Breakpoint> ();
			var needsUpdate = false;

			foreach (var breakpoint in breakpointStore.GetBreakpointsAtFile (textDocument.FilePath)) {
				if (breakpoint.Line > snapshot.LineCount)
					continue;

				bps.Add (breakpoint);
			}

			foreach (var breakpoint in bps) {
				if (eventArgs is BreakpointEventArgs breakpointEventArgs && breakpointEventArgs.Breakpoint == breakpoint)
					needsUpdate = true;

				var line = snapshot.GetLineFromLineNumber (breakpoint.Line - 1);
				var position = line.Start.Position + breakpoint.Column - 1;
				var span = await DebuggingService.GetBreakpointSpanAsync (textDocument, position);

				if (span.IsEmpty)
					continue;

				if (breakpoints.TryGetValue (breakpoint, out var existingBreakpoint)) {
					newBreakpoints.Add (breakpoint, existingBreakpoint);
					if (existingBreakpoint.Span != span) {
						// Update if anything was modified
						existingBreakpoint.Span = span;
						needsUpdate = true;
					}
				} else if (span.End < snapshot.Length) {
					var trackingSpan = snapshot.CreateTrackingSpan (span, SpanTrackingMode.EdgeExclusive);

					// Update if anything was added
					newBreakpoints.Add (breakpoint, new ManagerBreakpoint {
						Breakpoint = breakpoint,
						TrackingSpan = trackingSpan,
						Span = span
					});
					needsUpdate = true;
				}
			}

			// Update if anything was removed
			if (needsUpdate || breakpoints.Keys.Except (newBreakpoints.Keys).Any ())
				needsUpdate = true;

			breakpoints = newBreakpoints;

			if (needsUpdate)
				BreakpointsChanged?.Invoke (this, new SnapshotSpanEventArgs (new SnapshotSpan (snapshot, 0, snapshot.Length)));
		}
		public event EventHandler<SnapshotSpanEventArgs> BreakpointsChanged;

		public void Dispose ()
		{
			BreakpointsChanged = null;
			textBuffer.Changed -= TextBuffer_Changed;
			DebuggingService.Breakpoints.Changed -= OnBreakpointsChanged;
			DebuggingService.Breakpoints.BreakpointStatusChanged -= OnBreakpointsChanged;
			DebuggingService.Breakpoints.BreakpointModified -= OnBreakpointsChanged;
			if (textDocument != null)
				textDocument.FileActionOccurred -= TextDocument_FileActionOccurred;
		}

		public IEnumerable<BreakpointSpan> GetBreakpoints (ITextSnapshot snapshot)
		{
			foreach (var item in breakpoints.Values) {
				if (item.TrackingSpan.TextBuffer == snapshot.TextBuffer)
					yield return new BreakpointSpan (item.Breakpoint, item.TrackingSpan.GetSpan (snapshot));
			}
		}
	}

	class BreakpointSpan
	{
		public Breakpoint Breakpoint { get; }
		public SnapshotSpan Span { get; }

		public BreakpointSpan (Breakpoint breakpoint, SnapshotSpan span)
		{
			Breakpoint = breakpoint;
			Span = span;
		}
	}
}
