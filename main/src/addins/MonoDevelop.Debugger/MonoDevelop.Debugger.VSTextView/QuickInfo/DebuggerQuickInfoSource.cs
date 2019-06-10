using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Core;
using Microsoft.VisualStudio.Text.Editor;
using Gtk;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.Debugger.VSTextView.QuickInfo
{
	class DebuggerQuickInfoSource : IAsyncQuickInfoSource
	{
		readonly DebuggerQuickInfoSourceProvider provider;
		readonly ITextBuffer textBuffer;
		DebugValueWindow window;
		ITextView lastView;
		DocumentView lastDocumentView;

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
			var debuggerSession = window.Tree.Frame?.DebuggerSession;
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
			var point = triggerPoint.GetPoint (snapshot);

			foreach (var debugInfoProvider in provider.debugInfoProviders) {
				var debugInfo = await debugInfoProvider.Value.GetDebugInfoAsync (point, cancellationToken);
				if (debugInfo.Text == null) {
					continue;
				}

				var options = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
				options.AllowMethodEvaluation = true;
				options.AllowTargetInvoke = true;

				var val = DebuggingService.CurrentFrame.GetExpressionValue (debugInfo.Text, options);

				if (val.IsEvaluating)
					await WaitOneAsync (val.WaitHandle, cancellationToken);
				if (cancellationToken.IsCancellationRequested)
					return null;
				if (val == null || val.IsUnknown || val.IsNotSupported || val.IsError)
					return null;

				if (!view.Properties.TryGetProperty (typeof (Gtk.Widget), out Gtk.Widget gtkParent))
					return null;
				provider.textDocumentFactoryService.TryGetTextDocument (textBuffer, out var textDocument);

				// This is a bit hacky, since AsyncQuickInfo is designed to display multiple elements if multiple sources
				// return value, we don't want that for debugger value hovering, hence we dismiss AsyncQuickInfo
				// and do our own thing, notice VS does same thing
				await session.DismissAsync ();
				await provider.joinableTaskContext.Factory.SwitchToMainThreadAsync ();
				this.lastView = view;
				val.Name = debugInfo.Text;
				window = new DebugValueWindow ((Gtk.Window)gtkParent.Toplevel, textDocument?.FilePath, textBuffer.CurrentSnapshot.GetLineNumberFromPosition (debugInfo.Span.GetStartPoint (textBuffer.CurrentSnapshot)), DebuggingService.CurrentFrame, val, null);
				Ide.IdeApp.CommandService.RegisterTopWindow (window);
				var bounds = view.TextViewLines.GetCharacterBounds (point);
				view.LayoutChanged += LayoutChanged;
#if CLOSE_ON_FOCUS_LOST
				view.LostAggregateFocus += View_LostAggregateFocus;
#endif
				RegisterForHiddenAsync (view).Ignore ();
				window.LeaveNotifyEvent += LeaveNotifyEvent;
#if MAC
				var cocoaView = ((ICocoaTextView)view);
				var cgPoint = cocoaView.VisualElement.ConvertPointToView (new CoreGraphics.CGPoint (bounds.Left - view.ViewportLeft, bounds.Top - view.ViewportTop), cocoaView.VisualElement.Superview);
				cgPoint.Y = cocoaView.VisualElement.Superview.Frame.Height - cgPoint.Y;
				window.ShowPopup (gtkParent, new Gdk.Rectangle ((int)cgPoint.X, (int)cgPoint.Y, (int)bounds.Width, (int)bounds.Height), Components.PopupPosition.TopLeft);
#else
				throw new NotImplementedException ();
#endif
				return null;
			}
			return null;
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

		private void LeaveNotifyEvent (object o, LeaveNotifyEventArgs args)
		{
			if(args.Event.Detail != Gdk.NotifyType.Nonlinear)
				return;
			DestroyWindow ();
		}

		void DestroyWindow ()
		{
			Runtime.AssertMainThread ();
			if (window != null) {
				window.Destroy ();
				window.LeaveNotifyEvent -= LeaveNotifyEvent;
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
