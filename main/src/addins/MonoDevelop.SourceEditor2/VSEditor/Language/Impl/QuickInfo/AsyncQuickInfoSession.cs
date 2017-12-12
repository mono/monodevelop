namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Internal.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Language.Utilities;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    internal sealed class AsyncQuickInfoSession : IAsyncQuickInfoSession
    {
        private readonly IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> orderedSourceProviders;

        private readonly IGuardedOperations guardedOperations;
        private readonly IToolTipService toolTipService;
        private readonly JoinableTaskContext joinableTaskContext;
        private readonly ITrackingPoint triggerPoint;

        // For the purposes of synchronization, state updates are non-atomic and 'Calculating'
        // state is considered to be transient. The properties of the object can be updated
        // individually and are immediately visible to all threads. External extenders are
        // essentially not impacted by this lack of atomicity because they only have a reference
        // to the session from the broker after it is finished calculating, and the non-atomic
        // updating of the properties happens after all IAsyncQuickInfoSources have returned.
        // This class avoids its own races by marshalling all calls into the class and all state
        // changes through the UI thread and by only allowing one invocation of 'StartAsync()'.

        #region Cross Thread Readable, Modifiable

        // All state in this region can be read or modified from any thread and must
        // be accessed with VOLATILE.READ() + VOLATILE.WRITE().
        private ImmutableList<object> content = ImmutableList<object>.Empty;
        private ITrackingSpan applicableToSpan;

        #endregion

        #region Cross Thread Readable, Modifiable Only Via UI Thread

        // State in this region can be read from any thread at any time (often via properties)
        // but writes are synchronized via the UI thread. All readers should use VOLATILE.READ()
        // all writers should use VOLATILE.WRITE().
        private bool uiThreadWritableHasInteractiveContent;
        private int uiThreadWritableState = (int)QuickInfoSessionState.Created;

        #endregion

        #region Reable/Writeable via UI Thread Only

        private CancellationTokenSource uiThreadOnlyLinkedCancellationTokenSource;
        internal IToolTipPresenter uiThreadOnlyPresenter;

        #endregion
		
        public AsyncQuickInfoSession (
			IEnumerable<Lazy<IAsyncQuickInfoSourceProvider, IOrderableContentTypeMetadata>> orderedSourceProviders,
			IGuardedOperations guardedOperations,
            JoinableTaskContext joinableTaskContext,
            IToolTipService toolTipService,
            ITextView textView,
            ITrackingPoint triggerPoint,
            QuickInfoSessionOptions options,
            PropertyCollection propertyCollection)
        {
            this.orderedSourceProviders = orderedSourceProviders ?? throw new ArgumentNullException(nameof(orderedSourceProviders));
            this.guardedOperations = guardedOperations ?? throw new ArgumentNullException(nameof(guardedOperations));
            this.joinableTaskContext = joinableTaskContext ?? throw new ArgumentNullException(nameof(joinableTaskContext));
            this.toolTipService = toolTipService ?? throw new ArgumentNullException(nameof(toolTipService));
            this.TextView = textView ?? throw new ArgumentNullException(nameof(textView));
            this.triggerPoint = triggerPoint ?? throw new ArgumentNullException(nameof(triggerPoint));
            this.Options = options;

            // Bug #512117: Remove compatibility shims for 2nd gen. Quick Info APIs.
            // We can remove this null check once we remove the legacy APIs.
            this.Properties = propertyCollection ?? new PropertyCollection();

            // Trigger point must be a tracking point on the view's buffer.
            if (triggerPoint.TextBuffer != textView.TextBuffer)
            {
                throw new ArgumentException("The specified ITextSnapshot doesn't belong to the correct TextBuffer");
            }
        }

        #region IAsyncQuickInfoSession

        // All state changes are dispatched on the UI thread via TransitionState().
        public event EventHandler<QuickInfoSessionStateChangedEventArgs> StateChanged;

        public bool HasInteractiveContent => Volatile.Read(ref this.uiThreadWritableHasInteractiveContent);

        public ITrackingSpan ApplicableToSpan => Volatile.Read(ref this.applicableToSpan);

        public QuickInfoSessionOptions Options { get; }

        public PropertyCollection Properties { get; }

        public QuickInfoSessionState State => (QuickInfoSessionState)Volatile.Read(ref this.uiThreadWritableState);

        public ITextView TextView { get; }

        public IEnumerable<object> Content => Volatile.Read(ref this.content);

        public async Task DismissAsync()
        {
            // Ensure that we have the UI thread. To avoid races, the rest of this method must be sync.
            await this.joinableTaskContext.Factory.SwitchToMainThreadAsync();

            var currentState = this.State;
            if (currentState != QuickInfoSessionState.Dismissed)
            {
                // Cancel any running computations.
                this.uiThreadOnlyLinkedCancellationTokenSource?.Cancel();
                this.uiThreadOnlyLinkedCancellationTokenSource = null;

                // Dismiss presenter.
                var presenter = this.uiThreadOnlyPresenter;
                if (presenter != null)
                {
                    presenter.Dismissed -= this.OnDismissed;
                    this.uiThreadOnlyPresenter.Dismiss();

                    this.uiThreadOnlyPresenter = null;
                }

                // Update object state.
                Volatile.Write(ref this.content, ImmutableList<object>.Empty);
                Volatile.Write(ref this.applicableToSpan, null);

                // Alert subscribers on the UI thread.
                this.TransitionTo(QuickInfoSessionState.Dismissed);
            }
        }

        public ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer)
        {
            var mappedTriggerPoint = GetTriggerPoint(textBuffer.CurrentSnapshot);

            if (!mappedTriggerPoint.HasValue)
            {
                return null;
            }

            return mappedTriggerPoint.Value.Snapshot.CreateTrackingPoint(mappedTriggerPoint.Value, PointTrackingMode.Negative);
        }

        public SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot)
        {
            var triggerSnapshotPoint = this.triggerPoint.GetPoint(this.TextView.TextSnapshot);
            var triggerSpan = new SnapshotSpan(triggerSnapshotPoint, 0);

            var mappedSpans = new FrugalList<SnapshotSpan>();
            MappingHelper.MapDownToBufferNoTrack(triggerSpan, textSnapshot.TextBuffer, mappedSpans);

            if (mappedSpans.Count == 0)
            {
                return null;
            }
            else
            {
                return mappedSpans[0].Start;
            }
        }

        #endregion

        #region Internal Impl

        internal async Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.State != QuickInfoSessionState.Created)
            {
                throw new InvalidOperationException($"Session must be in the {QuickInfoSessionState.Created} state to be started");
            }

            // Ensure we have the UI thread.
            await this.joinableTaskContext.Factory.SwitchToMainThreadAsync();

            cancellationToken.ThrowIfCancellationRequested();

            // Create a linked cancellation token and store this in the class so we can be canceled by calls to DismissAsync()
            // without impacting the caller's cancellation token.
            using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    this.uiThreadOnlyLinkedCancellationTokenSource = linkedCancellationTokenSource;
                    var failures = await this.ComputeContentAndStartPresenterAsync(this.uiThreadOnlyLinkedCancellationTokenSource.Token);

                    if (failures?.Any() ?? false)
                    {
                        // Guarded operations is not safe to use from the background so
                        // we catch all exceptions and post them here on the UI thread.
                        this.guardedOperations.HandleException(this, new AggregateException(failures));
                    }
                }
                finally
                {
                    this.uiThreadOnlyLinkedCancellationTokenSource = null;

                    // If we were unable to await some computation task and don't end up with
                    // a visible presenter + content, ensure that we cleanup and change our state appropriately.
                    if (Volatile.Read(ref this.uiThreadOnlyPresenter) == null)
                    {
                        await this.DismissAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        #endregion

        #region Private Impl

        private async Task<IList<Exception>> ComputeContentAndStartPresenterAsync(CancellationToken cancellationToken)
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            // Read current state.
            var originalState = this.State;
            (IList<object> items, IList<ITrackingSpan> applicableToSpans, IList<Exception> failures)? results = null;

            // Alert subscribers on the UI thread.
            this.TransitionTo(QuickInfoSessionState.Calculating);

            cancellationToken.ThrowIfCancellationRequested();

            // Find and create the sources.  Sources cache is smart enough to
            // invalidate on content-type changed and free on view close.
            var sources = IntellisenseSourceCache.GetSources(
                this.TextView,
                GetBuffersForTriggerPoint().ToList(),
                this.CreateSources);

            // Compute quick info items. This method switches off the UI thread.
            // From here on out we're on an arbitrary thread.
            results = await ComputeContentAsync(sources, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Update our content, or put the empty list if there is none.
            Volatile.Write(
                ref this.content,
                results != null ? ImmutableList.CreateRange(results.Value.items) : ImmutableList<object>.Empty);

            await StartUIThreadEpilogueAsync(originalState, results?.applicableToSpans, cancellationToken).ConfigureAwait(false);

            return results?.failures;
        }

        private async Task StartUIThreadEpilogueAsync(
            QuickInfoSessionState originalState,
            IList<ITrackingSpan> applicableToSpans,
            CancellationToken cancellationToken)
        {
            // Ensure we're back on the UI thread.
            await this.joinableTaskContext.Factory.SwitchToMainThreadAsync();

            if (applicableToSpans != null)
            {
                // Update the applicable-to span.
                this.ComputeApplicableToSpan(applicableToSpans);
            }

            // Check if any of our content is interactive and cache that so it's not done on mouse move.
            this.ComputeHasInteractiveContent();

            // If we have results and a span for which to show them and we aren't cancelled update the tip.
            if ((originalState == QuickInfoSessionState.Created)
                && this.Content.Any()
                && (this.ApplicableToSpan != null)
                && !cancellationToken.IsCancellationRequested)
            {
                this.CreateAndStartPresenter();
            }
        }

        private void CreateAndStartPresenter()
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);
            Debug.Assert(this.uiThreadOnlyPresenter == null);

            // Configure presenter behavior.
            var parameters = new ToolTipParameters(
                this.Options.HasFlag(QuickInfoSessionOptions.TrackMouse),
                keepOpenFunc: this.ContentRequestsKeepOpen);

            // Create and show presenter.
            this.uiThreadOnlyPresenter = this.toolTipService.CreatePresenter(this.TextView, parameters);
            this.uiThreadOnlyPresenter.Dismissed += this.OnDismissed;
            this.uiThreadOnlyPresenter.StartOrUpdate(this.ApplicableToSpan, this.Content);

            // Ensure that the presenter didn't dismiss the session.
            if (this.State != QuickInfoSessionState.Dismissed)
            {
                // Update state and alert subscribers on the UI thread.
                this.TransitionTo(QuickInfoSessionState.Visible);
            }
        }

        private bool ContentRequestsKeepOpen()
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            if (this.HasInteractiveContent)
            {
                foreach (var content in this.Content)
                {
                    if ((content is IInteractiveQuickInfoContent interactiveContent)
                        && ((interactiveContent.KeepQuickInfoOpen || interactiveContent.IsMouseOverAggregated)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void OnDismissed(object sender, EventArgs e)
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            this.joinableTaskContext.Factory.RunAsync(async delegate
            {
                await this.DismissAsync().ConfigureAwait(false);
            });
        }

        private async Task<(IList<object> items, IList<ITrackingSpan> applicableToSpans, IList<Exception> failures)> ComputeContentAsync(
            IEnumerable<OrderedSource> unorderedSources,
            CancellationToken cancellationToken)
        {
            // Ensure we're off the UI thread.
            await TaskScheduler.Default;

            cancellationToken.ThrowIfCancellationRequested();

            var items = new FrugalList<object>();
            var applicableToSpans = new FrugalList<ITrackingSpan>();
            var failures = new FrugalList<Exception>();

            // Sources from the cache are from the flattened projection buffer graph
            // so they're initially out of order. We recorded their MEF ordering in
            // a property though so we can reorder them now.
            foreach (var source in unorderedSources.OrderBy(source => source.Order))
            {
                // This code is sequential to enable back-compat with the IQuickInfo* APIs,
                // but when the shims are removed, consider parallelizing as a potential optimization.
                await this.ComputeSourceContentAsync(
                    source.Source,
                    items,
                    applicableToSpans,
                    failures,
                    cancellationToken);
            }

            return (items, applicableToSpans, failures);
        }

        private void ComputeHasInteractiveContent()
        {
            foreach (var result in this.Content)
            {
                if (result is IInteractiveQuickInfoContent)
                {
                    Volatile.Write(ref this.uiThreadWritableHasInteractiveContent, true);
                    break;
                }
            }
        }

        private async Task ComputeSourceContentAsync(
            IAsyncQuickInfoSource source,
            IList<object> items,
            IList<ITrackingSpan> applicableToSpans,
            IList<Exception> failures,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!this.joinableTaskContext.IsOnMainThread);

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                    var result = await source.GetQuickInfoItemAsync(this, cancellationToken).ConfigureAwait(false);
                    if (result != null)
                    {
                        items.Add(result.Item);
                        if (result.ApplicableToSpan != null)
                        {
                            applicableToSpans.Add(result.ApplicableToSpan);
                        }
                    }
            }
            catch (Exception ex) when (ex.GetType() != typeof(OperationCanceledException))
            {
                failures.Add(ex);
            }
        }

        private void ComputeApplicableToSpan(IEnumerable<ITrackingSpan> applicableToSpans)
        {
            // Requires UI thread for access to BufferGraph.
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            ITrackingSpan newApplicableToSpan = Volatile.Read(ref this.applicableToSpan);

            foreach (var result in applicableToSpans)
            {
                var applicableToSpan = result;

                if (applicableToSpan != null)
                {
                    SnapshotSpan subjectAppSnapSpan = applicableToSpan.GetSpan(applicableToSpan.TextBuffer.CurrentSnapshot);

                    var surfaceAppSpans = this.TextView.BufferGraph.MapUpToBuffer(
                        subjectAppSnapSpan,
                        applicableToSpan.TrackingMode,
                        this.TextView.TextBuffer);

                    if (surfaceAppSpans.Count >= 1)
                    {
                        applicableToSpan = surfaceAppSpans[0].Snapshot.CreateTrackingSpan(surfaceAppSpans[0], applicableToSpan.TrackingMode);

                        newApplicableToSpan = IntellisenseUtilities.GetEncapsulatingSpan(
                            this.TextView,
                            newApplicableToSpan,
                            applicableToSpan);
                    }
                }
            }

            Volatile.Write(ref this.applicableToSpan, newApplicableToSpan);
        }

        private IEnumerable<OrderedSource> CreateSources(ITextBuffer textBuffer)
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            int i = 0;

            foreach (var sourceProvider in this.orderedSourceProviders)
            {
                foreach (var contentType in sourceProvider.Metadata.ContentTypes)
                {
                    if (textBuffer.ContentType.IsOfType(contentType))
                    {
                        var source = this.guardedOperations.InstantiateExtension(
                            this,
                            sourceProvider,
                            provider => provider.TryCreateQuickInfoSource(textBuffer));

                        if (source != null)
                        {
                            yield return new OrderedSource(i, source);
                        }
                    }
                }

                ++i;
            }
        }

        private Collection<ITextBuffer> GetBuffersForTriggerPoint()
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            return this.TextView.BufferGraph.GetTextBuffers(
                buffer => this.GetTriggerPoint(buffer.CurrentSnapshot) != null);
        }

        private void TransitionTo(QuickInfoSessionState newState)
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            var oldState = this.State;
            bool isValid = false;

            switch (newState)
            {
                case QuickInfoSessionState.Created:
                    isValid = false;
                    break;
                case QuickInfoSessionState.Calculating:
                    isValid = oldState == QuickInfoSessionState.Created || oldState == QuickInfoSessionState.Visible;
                    break;
                case QuickInfoSessionState.Dismissed:
                    isValid = oldState == QuickInfoSessionState.Visible
                        || oldState == QuickInfoSessionState.Calculating;
                    break;
                case QuickInfoSessionState.Visible:
                    isValid = oldState == QuickInfoSessionState.Calculating;
                    break;
            }

            if (!isValid)
            {
                throw new InvalidOperationException($"Invalid {nameof(IAsyncQuickInfoSession)} state transition from {oldState} to {newState}");
            }

            Volatile.Write(ref this.uiThreadWritableState, (int)newState);
            this.StateChanged?.Invoke(this, new QuickInfoSessionStateChangedEventArgs(oldState, newState));
        }

        private sealed class OrderedSource : IDisposable
        {
            public OrderedSource(int order, IAsyncQuickInfoSource source)
            {
                this.Order = order;
                this.Source = source ?? throw new ArgumentNullException(nameof(source));
            }

            public IAsyncQuickInfoSource Source { get; }

            public int Order { get; }

            public void Dispose()
            {
                this.Source.Dispose();
            }
        }

        #endregion
    }
}
