namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Internal.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Language.Utilities;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(IAsyncQuickInfoBroker))]
    internal sealed class AsyncQuickInfoBroker : IAsyncQuickInfoBroker
    {
        private readonly IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> unorderedSourceProviders;
        private readonly IGuardedOperations guardedOperations;
        private readonly IToolTipService toolTipService;
        private readonly JoinableTaskContext joinableTaskContext;
        private IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> orderedSourceProviders;

        [ImportingConstructor]
        public AsyncQuickInfoBroker(
            [ImportMany]IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> unorderedSourceProviders,
            IGuardedOperations guardedOperations,
            IToolTipService toolTipService,
            JoinableTaskContext joinableTaskContext)
        {
            // Bug #512117: Remove compatibility shims for 2nd gen. Quick Info APIs.
            // Combines new + legacy providers into a single series for relative ordering.
            var combinedProviders = unorderedSourceProviders ?? throw new ArgumentNullException(nameof(unorderedSourceProviders));

            this.unorderedSourceProviders = combinedProviders;
#pragma warning restore 618
            this.guardedOperations = guardedOperations ?? throw new ArgumentNullException(nameof(guardedOperations));
            this.joinableTaskContext = joinableTaskContext ?? throw new ArgumentNullException(nameof(joinableTaskContext));
            this.toolTipService = toolTipService;
        }

        #region IAsyncQuickInfoBroker

        public IAsyncQuickInfoSession GetSession(ITextView textView)
        {
            if (textView.Properties.TryGetProperty(typeof(AsyncQuickInfoSession), out AsyncQuickInfoSession property))
            {
                return property;
            }

            return null;
        }

        public bool IsQuickInfoActive(ITextView textView) => GetSession(textView) != null;

		public async Task<IAsyncQuickInfoSession> TriggerQuickInfoAsync (
		   ITextView textView,
		   ITrackingPoint triggerPoint,
		   QuickInfoSessionOptions options,
		   CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			// Caret element requires UI thread.
			await this.joinableTaskContext.Factory.SwitchToMainThreadAsync ();

			// We switched threads and there is some latency, so ensure that we're still not canceled.
			cancellationToken.ThrowIfCancellationRequested ();

			// Dismiss any currently open session.
			var currentSession = this.GetSession (textView);
			if (currentSession != null) {
				await currentSession.DismissAsync ();
			}

			// Get the trigger point from the caret if none is provided.
			triggerPoint = triggerPoint ?? textView.TextSnapshot.CreateTrackingPoint (
				textView.Caret.Position.BufferPosition,
				PointTrackingMode.Negative);

			var newSession = new AsyncQuickInfoSession (
				this.OrderedSourceProviders,
				this.guardedOperations,
				this.joinableTaskContext,
				this.toolTipService,
				textView,
				triggerPoint,
				options,
				null);

			// StartAsync() is responsible for dispatching a StateChange
			// event if canceled so no need to clean these up on cancellation.
			newSession.StateChanged += this.OnStateChanged;
			textView.Properties.AddProperty (typeof (AsyncQuickInfoSession), newSession);

			try {
				await newSession.StartAsync (cancellationToken);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
				// Don't throw OperationCanceledException unless the caller canceled us.
				// This can happen if computation was canceled by a quick info source
				// dismissing the session during computation, which we want to consider
				// more of a 'N/A' than an error.
				return null;
			}

			return newSession;
		}


		#endregion

		#region Private Impl

		// Bug #512117: Remove compatibility shims for 2nd gen. Quick Info APIs.
		// This interface exists only to expose additional functionality required by the shims.
#pragma warning disable 618
		private IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> OrderedSourceProviders
            => this.orderedSourceProviders ?? (this.orderedSourceProviders = Orderer.Order(this.unorderedSourceProviders));
#pragma warning restore 618

        // Listens for the session being dismissed so that we can remove it from the view's property bag.
        private void OnStateChanged(object sender, QuickInfoSessionStateChangedEventArgs e)
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            if (e.NewState == QuickInfoSessionState.Dismissed)
            {
                if (sender is AsyncQuickInfoSession session)
                {
                    session.TextView.Properties.RemoveProperty(typeof(AsyncQuickInfoSession));
                    session.StateChanged -= this.OnStateChanged;
                    return;
                }

                Debug.Fail("Unexpected sender type");
            }
        }

        #endregion
    }
}
