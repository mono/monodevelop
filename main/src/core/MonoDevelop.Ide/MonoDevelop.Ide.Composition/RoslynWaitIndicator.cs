using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Utilities;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (IWaitIndicator))]
	internal class RoslynWaitIndicator : IWaitIndicator
	{
		static readonly Func<string, string, string> s_messageGetter = (t, m) => string.Format ("{0} : {1}", t, m);

		public WaitIndicatorResult Wait (string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action)
		{
			using (Logger.LogBlock (FunctionId.Misc_VisualStudioWaitIndicator_Wait, s_messageGetter, title, message, CancellationToken.None))
			using (var waitContext = StartWait (title, message, allowCancel, showProgress)) {
				try {
					action (waitContext);

					return WaitIndicatorResult.Completed;
				} catch (OperationCanceledException) {
					return WaitIndicatorResult.Canceled;
				} catch (AggregateException aggregate) when (aggregate.InnerExceptions.All (e => e is OperationCanceledException)) {
					return WaitIndicatorResult.Canceled;
				}
			}
		}

		public IWaitContext StartWait (string title, string message, bool allowCancel, bool showProgress)
		{
			var service = TypeSystemService.emptyWorkspace.Services.GetService<IGlobalOperationNotificationService> ();
			return new WaitContext (service, title, message, allowCancel, showProgress);
		}

		private sealed class WaitContext : IWaitContext
		{
			private readonly GlobalOperationRegistration registration;
			readonly MessageDialogProgressMonitor monitor;

			public WaitContext (IGlobalOperationNotificationService service, string title, string message, bool allowCancel, bool showProgress)
			{
				this.registration = service.Start (title);

				ProgressTracker = showProgress
					? new ProgressTracker ((description, completedItems, totalItems) => UpdateProgress (description, completedItems, totalItems))
					: new ProgressTracker ();

				monitor = new MessageDialogProgressMonitor (showProgress, allowCancel, showDetails: false, hideWhenDone: true);
				// TODO: only show if blocking for 2 seconds...
			}

			public CancellationToken CancellationToken {
				get { return monitor.AllowCancel ? monitor.CancellationToken : CancellationToken.None; }
			}

			public bool AllowCancel {
				get => monitor.AllowCancel;
				set => monitor.AllowCancel = value;
			}

			public string Message {
				get => monitor.Message;
				set => monitor.Message = value;
			}

			public IProgressTracker ProgressTracker { get; }

			void UpdateProgress (string description, int completedItems, int totalItems)
			{
				monitor.Message = description;
				// TODO: update progress here, step interface makes it hard
			}

			public void Dispose ()
			{
				if (!monitor.AllowCancel || CancellationToken.IsCancellationRequested)
					registration.Done ();

				this.registration.Dispose ();
			}
		}
	}
}
