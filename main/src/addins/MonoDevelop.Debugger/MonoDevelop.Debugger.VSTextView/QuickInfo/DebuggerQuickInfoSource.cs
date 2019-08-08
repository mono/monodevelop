using System;
using System.Threading;
using System.Threading.Tasks;

using Gtk;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.Debugger.VSTextView.QuickInfo
{
	class DebuggerQuickInfoSource : IAsyncQuickInfoSource
	{
		readonly DebuggerQuickInfoSourceProvider provider;
		readonly ITextBuffer textBuffer;
		DocumentView lastDocumentView;
#if MAC
		MacDebuggerTooltipWindow window;
#endif
		ITextView lastView;

		public DebuggerQuickInfoSource (DebuggerQuickInfoSourceProvider provider, ITextBuffer textBuffer)
		{
			this.provider = provider;
			this.textBuffer = textBuffer;
			DebuggingService.CurrentFrameChanged += CurrentFrameChanged;
			DebuggingService.StoppedEvent += TargetProcessExited;
		}

		void CurrentFrameChanged (object sender, EventArgs e)
		{
			if (window != null) {
				DestroyWindow ();
			}
		}

		void TargetProcessExited (object sender, EventArgs e)
		{
			if (window == null)
				return;

			var debuggerSession = window.GetDebuggerSession ();
			if (debuggerSession == null || debuggerSession == sender) {
				DestroyWindow ();
			}
		}

		public void Dispose ()
		{
			DebuggingService.CurrentFrameChanged -= CurrentFrameChanged;
			DebuggingService.StoppedEvent -= TargetProcessExited;
			Runtime.RunInMainThread (DestroyWindow).Ignore ();
		}

		static async Task<bool> WaitOneAsync (WaitHandle handle, CancellationToken cancellationToken)
		{
			RegisteredWaitHandle registeredHandle = null;
			var tokenRegistration = default (CancellationTokenRegistration);
			try {
				var tcs = new TaskCompletionSource<bool> ();
				registeredHandle = ThreadPool.RegisterWaitForSingleObject (
					handle,
					(state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult (!timedOut),
					tcs,
					int.MaxValue,
					true);
				tokenRegistration = cancellationToken.Register (
					state => ((TaskCompletionSource<bool>)state).TrySetCanceled (),
					tcs);
				return await tcs.Task;
			} finally {
				if (registeredHandle != null)
					registeredHandle.Unregister (null);
				tokenRegistration.Dispose ();
			}
		}

		public async Task<QuickInfoItem> GetQuickInfoItemAsync (IAsyncQuickInfoSession session, CancellationToken cancellationToken)
		{
			if (DebuggingService.CurrentFrame == null)
				return null;

			if (window != null)
				await Runtime.RunInMainThread (DestroyWindow);

			var view = session.TextView;
			var textViewLines = view.TextViewLines;
			var snapshot = textViewLines.FormattedSpan.Snapshot;
			var triggerPoint = session.GetTriggerPoint (textBuffer);
			if (snapshot.TextBuffer != triggerPoint.TextBuffer)
				return null;
			var point = triggerPoint.GetPoint (snapshot);

			foreach (var debugInfoProvider in provider.debugInfoProviders) {
				DataTipInfo debugInfo = default;

				if (!view.Selection.IsEmpty) {
					foreach (var span in view.Selection.SelectedSpans) {
						if (span.Contains (point)) {
							//debugInfo = new DataTipInfo (snapshot.CreateTrackingSpan (span, SpanTrackingMode.EdgeInclusive), snapshot.GetText (span));
							debugInfo = await debugInfoProvider.Value.GetDebugInfoAsync (span, cancellationToken);
							break;
						}
					}
				} else {
					debugInfo = await debugInfoProvider.Value.GetDebugInfoAsync (point, cancellationToken);
				}

				if (!debugInfo.IsDefault) {
					await EvaluateAndShowTooltipAsync (session, view, point, debugInfo, cancellationToken);
					return null;
				}
			}

			return null;
		}

		private async Task EvaluateAndShowTooltipAsync (IAsyncQuickInfoSession session, ITextView view, SnapshotPoint point, DataTipInfo debugInfo, CancellationToken cancellationToken)
		{
			var options = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			options.AllowMethodEvaluation = true;
			options.AllowTargetInvoke = true;

			var val = DebuggingService.CurrentFrame.GetExpressionValue (debugInfo.Text, options);

			if (val.IsEvaluating)
				await WaitOneAsync (val.WaitHandle, cancellationToken);

			if (cancellationToken.IsCancellationRequested)
				return;

			if (val == null || val.IsUnknown || val.IsNotSupported)
				return;

			if (!view.Properties.TryGetProperty (typeof (Widget), out Widget gtkParent))
				return;

			provider.textDocumentFactoryService.TryGetTextDocument (view.TextDataModel.DocumentBuffer, out var textDocument);

			// This is a bit hacky, since AsyncQuickInfo is designed to display multiple elements if multiple sources
			// return value, we don't want that for debugger value hovering, hence we dismiss AsyncQuickInfo
			// and do our own thing, notice VS does same thing
			await session.DismissAsync ();
			await provider.joinableTaskContext.Factory.SwitchToMainThreadAsync ();
			lastView = view;

			val.Name = debugInfo.Text;

#if MAC
			var location = new PinnedWatchLocation (textDocument?.FilePath);
			var snapshot = view.TextDataModel.DocumentBuffer.CurrentSnapshot;
			int line, column;

			var start = debugInfo.Span.GetStartPoint (snapshot);
			snapshot.GetLineAndColumn (start, out line, out column);
			location.Column = column;
			location.Line = line;

			var end = debugInfo.Span.GetEndPoint (snapshot);
			snapshot.GetLineAndColumn (end, out line, out column);
			location.EndColumn = column;
			location.EndLine = line;

			window = new MacDebuggerTooltipWindow (location, DebuggingService.CurrentFrame, val, watch: null);

			view.LayoutChanged += LayoutChanged;
#if CLOSE_ON_FOCUS_LOST
			view.LostAggregateFocus += View_LostAggregateFocus;
#endif
			RegisterForHiddenAsync (view).Ignore ();

			var cocoaView = (ICocoaTextView) view;
			var bounds = view.TextViewLines.GetCharacterBounds (point);
			var rect = new CoreGraphics.CGRect (bounds.Left - view.ViewportLeft, bounds.Top - view.ViewportTop, bounds.Width, bounds.Height);

			window.Show (rect, cocoaView.VisualElement, AppKit.NSRectEdge.MaxXEdge);
#else
			throw new NotImplementedException ();
#endif
		}

		private async Task RegisterForHiddenAsync (ITextView view)
		{
			if (view.Properties.TryGetProperty<FileDocumentController> (typeof (DocumentController), out var documentController)) {
				lastDocumentView = await documentController.GetDocumentView ();
				lastDocumentView.ContentHidden += DocumentView_ContentHidden;
			}
		}

		private void DocumentView_ContentHidden (object sender, EventArgs e)
		{
			DestroyWindow ();
		}

#if CLOSE_ON_FOCUS_LOST
		private void View_LostAggregateFocus (object sender, EventArgs e)
		{
#if MAC
			var nsWindow = MacInterop.GtkQuartz.GetWindow (window);
			if (nsWindow == AppKit.NSApplication.SharedApplication.KeyWindow)
				return;
			DestroyWindow ();
#else
			throw new NotImplementedException ();
#endif
		}
#endif

		private void LayoutChanged (object sender, TextViewLayoutChangedEventArgs e)
		{
			if (e.OldViewState.ViewportLeft != e.NewViewState.ViewportLeft ||
				e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth ||
				e.OldViewState.ViewportTop != e.NewViewState.ViewportTop ||
				e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				DestroyWindow ();
		}

		void DestroyWindow ()
		{
			Runtime.AssertMainThread ();
			if (window != null) {
				window.Close ();
				window = null;
			}
			if (lastView != null) {
				lastView.LayoutChanged -= LayoutChanged;
#if CLOSE_ON_FOCUS_LOST
				lastView.LostAggregateFocus -= View_LostAggregateFocus;
#endif
				lastView = null;
			}
			if (lastDocumentView != null) {
				lastDocumentView.ContentHidden -= DocumentView_ContentHidden;
				lastDocumentView = null;
			}
		}
	}
}
