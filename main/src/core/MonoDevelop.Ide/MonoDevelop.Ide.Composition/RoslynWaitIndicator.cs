using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Utilities;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Composition
{
	[Export (typeof (IWaitIndicator))]
	internal class RoslynWaitIndicator : IWaitIndicator
	{
		public WaitIndicatorResult Wait (string title, string message, bool allowCancel, Action<IWaitContext> action)
		{
			using (var waitContext = StartWait (title, message, allowCancel)) {
				action (waitContext);
			}

			return WaitIndicatorResult.Completed;
		}

		public WaitIndicatorResult Wait (string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action)
		{
			using (var waitContext = StartWait (title, message, allowCancel, showProgress)) {
				action (waitContext);
			}

			return WaitIndicatorResult.Completed;
		}

		public IWaitContext StartWait (string title, string message, bool allowCancel)
		{
			//var service = MonoDevelopWorkspace.HostServices.GetService<IGlobalOperationNotificationService> ();
			return new WaitContext (null, title, message, allowCancel);
		}

		public IWaitContext StartWait (string title, string message, bool allowCancel, bool showProgress)
		{
			//var service = MonoDevelopWorkspace.HostServices.GetService<IGlobalOperationNotificationService> ();
			return new WaitContext (null, title, message, allowCancel);
		}

		private sealed class WaitContext : IWaitContext
		{
			private readonly GlobalOperationRegistration registration;

			public WaitContext (IGlobalOperationNotificationService service, string title, string message, bool allowCancel)
			{
				//this.registration = service.Start (title);
			}

			public CancellationToken CancellationToken {
				get { return CancellationToken.None; }
			}

			public void UpdateProgress ()
			{
			}

			public bool AllowCancel {
				get {
					return false;
				}

				set {
				}
			}

			public string Message {
				get {
					return "";
				}

				set {
				}
			}

			public IProgressTracker ProgressTracker { get; } = new ProgressTracker ();

			public void Dispose ()
			{
				//this.registration.Dispose ();
			}
		}
	}
}
