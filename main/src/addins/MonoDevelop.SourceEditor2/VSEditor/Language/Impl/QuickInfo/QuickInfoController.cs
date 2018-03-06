namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Language.Utilities;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Threading;

    internal sealed class QuickInfoController
    {
        private readonly IAsyncQuickInfoBroker quickInfoBroker;
        private readonly JoinableTaskContext joinableTaskContext;
        private readonly ITextView textView;
        private CancellationTokenSource cancellationTokenSource;

        internal QuickInfoController(
            IAsyncQuickInfoBroker quickInfoBroker,
            JoinableTaskContext joinableTaskContext,
            ITextView textView)
        {
            this.quickInfoBroker = quickInfoBroker ?? throw new ArgumentNullException(nameof(quickInfoBroker));
            this.joinableTaskContext = joinableTaskContext ?? throw new ArgumentNullException(nameof(joinableTaskContext));
            this.textView = textView ?? throw new ArgumentNullException(nameof(textView));

            IntellisenseUtilities.ThrowIfNotOnMainThread(joinableTaskContext);

            this.textView.MouseHover += this.OnMouseHover;
            this.textView.Closed += this.OnTextViewClosed;
        }

        // Internal for unit test.
        internal void OnTextViewClosed(object sender, EventArgs e)
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            this.textView.Closed -= this.OnTextViewClosed;

            // Cancel any calculating sessions and dispose the token.
            this.CancelAndDisposeToken();

            // Terminate any open quick info sessions.
            this.joinableTaskContext.Factory.RunAsync(async delegate
            {
                var session = this.quickInfoBroker.GetSession(this.textView);
                if (session != null)
                {
                    await session.DismissAsync();
                }
            });

            this.textView.MouseHover -= this.OnMouseHover;
        }

        private void OnMouseHover(object sender, MouseHoverEventArgs e)
        {
            IntellisenseUtilities.ThrowIfNotOnMainThread(this.joinableTaskContext);

            SnapshotPoint? surfaceHoverPointNullable = e.TextPosition.GetPoint(
                this.textView.TextViewModel.DataBuffer,
                PositionAffinity.Predecessor);

            // Does hover correspond to actual position in document or
            // is there already a session around that is valid?
            if (!surfaceHoverPointNullable.HasValue || this.IsSessionStillValid(surfaceHoverPointNullable.Value))
            {
                return;
            }

            // Cancel last queued quick info update, if there is one.
            CancelAndDisposeToken();

            this.cancellationTokenSource = new CancellationTokenSource();

            // Start quick info session async on the UI thread.
            this.joinableTaskContext.Factory.RunAsync(async delegate
            {
                await UpdateSessionStateAsync(surfaceHoverPointNullable.Value, this.cancellationTokenSource.Token);

                // Clean up the cancellation token source.
                Debug.Assert(this.joinableTaskContext.IsOnMainThread);
                this.cancellationTokenSource.Dispose();
                this.cancellationTokenSource = null;
            });
        }

        private async Task UpdateSessionStateAsync(SnapshotPoint surfaceHoverPoint, CancellationToken cancellationToken)
        {
            // If we were cancelled while queued, do nothing.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            ITrackingPoint triggerPoint = surfaceHoverPoint.Snapshot.CreateTrackingPoint(
                surfaceHoverPoint.Position,
                PointTrackingMode.Negative);

            try
            {
                await this.quickInfoBroker.TriggerQuickInfoAsync(
                    this.textView,
                    triggerPoint,
                    QuickInfoSessionOptions.TrackMouse,
                    cancellationToken);
            }
            catch (OperationCanceledException) { /* swallow exception */ }
        }

        /// <summary>
        /// Ensures that the specified session is still valid given the specified point.  If the point is within the applicability
        /// span of the session, the session will be left alone and the method will return true.  If the point is outside of the
        /// sessions applicability span, the session will be dismissed and the method will return false.
        /// </summary>
        private bool IsSessionStillValid(SnapshotPoint point)
        {
            // Make sure we're being called with a surface snapshot point.
            Debug.Assert(point.Snapshot.TextBuffer == this.textView.TextViewModel.DataBuffer);

            var session = this.quickInfoBroker.GetSession(this.textView);

            if (session != null)
            {
                // First check that the point and applicable span are from the same subject buffer,
                // and then that they intersect.
                if ((session.ApplicableToSpan != null) &&
                    (session.ApplicableToSpan.TextBuffer == point.Snapshot.TextBuffer) &&
                    (session.ApplicableToSpan.GetSpan(point.Snapshot).IntersectsWith(new Span(point.Position, 0))))
                {
                    return true;
                }

                // If this session has an interactive content give it a chance to keep the session alive.
                if (session.HasInteractiveContent)
                {
                    foreach (var content in session.Content)
                    {
                        foreach (var result in session.Content)
                        {
                            if (result is IInteractiveQuickInfoContent interactiveContent
                                && (interactiveContent.KeepQuickInfoOpen || interactiveContent.IsMouseOverAggregated))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void CancelAndDisposeToken()
        {
            if (this.cancellationTokenSource != null)
            {
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource.Dispose();
                this.cancellationTokenSource = null;
            }
        }
    }
}
