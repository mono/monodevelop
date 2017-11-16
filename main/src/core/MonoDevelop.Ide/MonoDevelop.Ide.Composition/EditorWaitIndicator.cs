using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.Utilities;

namespace MonoDevelop.Ide.Composition
{
	[Export(typeof(IWaitIndicator))]
	class EditorWaitIndicator : IWaitIndicator
	{
		WaitIndicatorResult IWaitIndicator.Wait (string title, string message, bool allowCancel, Action<IWaitContext> action)
		{
			using (var waitContext = StartWait (title, message, allowCancel)) {
				action (waitContext);
			}

			return WaitIndicatorResult.Completed;
		}

		public IWaitContext StartWait (string title, string message, bool allowCancel)
		{
			return new WaitContext (title, message, allowCancel);
		}

		private sealed class WaitContext : IWaitContext
		{
			public WaitContext (string title, string message, bool allowCancel)
			{
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

			public void Dispose ()
			{
			}
		}
	}
}
