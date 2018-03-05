namespace Microsoft.VisualStudio.Language.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Threading;

    internal static class IntellisenseUtilities
    {
        internal static void ThrowIfNotOnMainThread(JoinableTaskContext joinableTaskContext)
        {
            if (!joinableTaskContext.IsOnMainThread)
            {
                throw new InvalidOperationException("Operation must occur on main thread.");
            }
        }

        internal static ITrackingSpan GetEncapsulatingSpan(ITextView textView, ITrackingSpan span1, ITrackingSpan span2)
        {
            // If either of the spans is null, return the other one.  If they're both null, we'll end up returning null
            // (as it should be).
            if (span1 == null)
            {
                return MapTrackingSpanToBuffer(textView, span2);
            }

            if (span2 == null)
            {
                return MapTrackingSpanToBuffer(textView, span1);
            }

            var surfaceSpans = new List<SnapshotSpan>();
            surfaceSpans.AddRange(MapTrackingSpanToSpansOnBuffer(textView, span1));
            surfaceSpans.AddRange(MapTrackingSpanToSpansOnBuffer(textView, span2));

            ITrackingSpan encapsulatingSpan = null;
            foreach (var span in surfaceSpans)
            {
                encapsulatingSpan = GetEncapsulatingSpan(
                    encapsulatingSpan,
                    span.Snapshot.CreateTrackingSpan(span, span1.TrackingMode));
            }

            return encapsulatingSpan;
        }

        private static ITrackingSpan GetEncapsulatingSpan(ITrackingSpan span1, ITrackingSpan span2)
        {
            // If either of the spans is null, return the other one.  If they're both null, we'll end up returning null
            // (as it should be).
            if (span1 == null)
            {
                return span2;
            }

            if (span2 == null)
            {
                return span1;
            }

            // Use the other overload if you need mapping between buffers.
            if (span1.TextBuffer != span2.TextBuffer)
            {
                throw new ArgumentException("Spans must be from the same textBuffer");
            }

            var snapshot = span1.TextBuffer.CurrentSnapshot;
            var snapSpan1 = span1.GetSpan(snapshot);
            var snapSpan2 = span2.GetSpan(snapshot);

            return snapshot.CreateTrackingSpan(
                Span.FromBounds(
                    Math.Min(snapSpan1.Start.Position, snapSpan2.Start.Position),
                    Math.Max(snapSpan1.End.Position, snapSpan2.End.Position)),
                span1.TrackingMode);
        }

        private static ITrackingSpan MapTrackingSpanToBuffer(ITextView textView, ITrackingSpan trackingSpan)
        {
            if (trackingSpan == null)
            {
                return null;
            }

            var spans = MapTrackingSpanToSpansOnBuffer(textView, trackingSpan);
            Debug.Assert(spans.Count == 1);
            return spans[0].Snapshot.CreateTrackingSpan(spans[0], trackingSpan.TrackingMode);
        }

        private static NormalizedSnapshotSpanCollection MapTrackingSpanToSpansOnBuffer(ITextView textView, ITrackingSpan trackingSpan)
        {
            return textView.BufferGraph.MapUpToBuffer(
                trackingSpan.GetSpan(trackingSpan.TextBuffer.CurrentSnapshot),
                trackingSpan.TrackingMode,
                textView.TextBuffer);
        }
    }
}
