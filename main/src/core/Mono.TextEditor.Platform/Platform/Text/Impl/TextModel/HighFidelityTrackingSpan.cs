namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Implementation of ITrackingSpan for the UndoRedo and Backward TrackingFidelityMode.
    /// Records noninvertible transition and consults these records as appropriate.
    /// </summary>
    internal class HighFidelityTrackingSpan : TrackingSpan
    {
        #region State and Construction
        private class VersionSpanHistory
        {
            private readonly ITextVersion version;
            private readonly Span span;
            private readonly List<VersionNumberPosition> noninvertibleStartHistory;
            private readonly List<VersionNumberPosition> noninvertibleEndHistory;

            public VersionSpanHistory(ITextVersion version,
                                      Span span,
                                      List<VersionNumberPosition> noninvertibleStartHistory,
                                      List<VersionNumberPosition> noninvertibleEndHistory)
            {
                this.version = version;
                this.span = span;
                this.noninvertibleStartHistory = noninvertibleStartHistory;
                this.noninvertibleEndHistory = noninvertibleEndHistory;
            }

            public ITextVersion Version { get { return this.version; } }
            public Span Span { get { return this.span; } }
            public List<VersionNumberPosition> NoninvertibleStartHistory { get { return this.noninvertibleStartHistory; } }
            public List<VersionNumberPosition> NoninvertibleEndHistory { get { return this.noninvertibleEndHistory; } }
        }

        private VersionSpanHistory cachedSpan;
        private TrackingFidelityMode fidelity;

        internal HighFidelityTrackingSpan(ITextVersion version, Span span, SpanTrackingMode spanTrackingMode, TrackingFidelityMode fidelity)
            : base(version, span, spanTrackingMode)
        {
            if (fidelity != TrackingFidelityMode.UndoRedo && fidelity != TrackingFidelityMode.Backward)
            {
                throw new ArgumentOutOfRangeException("fidelity");
            }
            List<VersionNumberPosition> startHistory = null;
            List<VersionNumberPosition> endHistory = null;
            if (fidelity == TrackingFidelityMode.UndoRedo && version.VersionNumber > 0)
            {
                // The system may perform undo operations that reach prior to the initial version; if any of
                // those transitions are noninvertible, then redoing back to the initial version will give the
                // wrong answer. Thus we save the state of the span for the initial version, unless
                // the initial version happens to be version zero (in which case we could not undo past it).

                startHistory = new List<VersionNumberPosition>();
                endHistory = new List<VersionNumberPosition>();

                if (version.VersionNumber != version.ReiteratedVersionNumber)
                {
                    Debug.Assert(version.ReiteratedVersionNumber < version.VersionNumber);
                    // If the current version and reiterated version differ, also remember the position
                    // using the reiterated version number, since when undoing back to this point it
                    // will be the key that is used.
                    startHistory.Add(new VersionNumberPosition(version.ReiteratedVersionNumber, span.Start));
                    endHistory.Add(new VersionNumberPosition(version.ReiteratedVersionNumber, span.End));
                }

                startHistory.Add(new VersionNumberPosition(version.VersionNumber, span.Start));
                endHistory.Add(new VersionNumberPosition(version.VersionNumber, span.End));
            }
            this.cachedSpan = new VersionSpanHistory(version, span, startHistory, endHistory);
            this.fidelity = fidelity;
        }
        #endregion

        #region Overrides
        public override ITextBuffer TextBuffer
        {
            get { return this.cachedSpan.Version.TextBuffer; }
        }

        public override TrackingFidelityMode TrackingFidelity
        {
            get { return this.fidelity; }
        }

        protected override Span TrackSpan(ITextVersion targetVersion)
        {
            // Compute the span on the requested snapshot.
            // This object caches the most recently requested version and the span in that version.
            //
            // We are relying on the atomicity of pointer copies (this.cachedSpan might change after we've
            // fetched it below but we will always get a self-consistent VersionSpan). This ensures we
            // have proper behavior when called from multiple threads (multiple threads may all track and update the
            // cached value if called at inconvenient times, but they will return consistent results).
            //
            // In most cases, one can track backwards from the cached version to a previously computed
            // version and get the same result, but this is not always the case: in particular, when one or both
            // ends of the span lie in a deleted region, simulating reinsertion of that region will not cause
            // the previous value of the span to be recovered. Such transitions are called noninvertible.
            // This class explicitly tracks the positions of span endpoints for versions for which the subsequent
            // transition is noninvertible; this allows the value to be computed properly when tracking backwards
            // or in undo/redo situations.

            VersionSpanHistory cached = this.cachedSpan;        // must fetch just once
            if (targetVersion == cached.Version)
            {
                // easy!
                return cached.Span;
            }

            PointTrackingMode startMode =
                (this.trackingMode == SpanTrackingMode.EdgeExclusive || this.trackingMode == SpanTrackingMode.EdgePositive)
                    ? PointTrackingMode.Positive
                    : PointTrackingMode.Negative;

            PointTrackingMode endMode =
                (this.trackingMode == SpanTrackingMode.EdgeExclusive || this.trackingMode == SpanTrackingMode.EdgeNegative)
                    ? PointTrackingMode.Negative
                    : PointTrackingMode.Positive;

            List<VersionNumberPosition> noninvertibleStartHistory = cached.NoninvertibleStartHistory;
            List<VersionNumberPosition> noninvertibleEndHistory = cached.NoninvertibleEndHistory;

            Span targetSpan;
            if (targetVersion.VersionNumber > cached.Version.VersionNumber)
            {
                // Compute the target span by going forward from the cached version
                int start = HighFidelityTrackingPoint.TrackPositionForwardInTime
                                (startMode, this.fidelity, ref noninvertibleStartHistory, cached.Span.Start, cached.Version, targetVersion);
                int end = HighFidelityTrackingPoint.TrackPositionForwardInTime
                                (endMode, this.fidelity, ref noninvertibleEndHistory, cached.Span.End, cached.Version, targetVersion);
                targetSpan = Span.FromBounds(start, System.Math.Max(start, end));

                // Cache the new span
                this.cachedSpan = new VersionSpanHistory(targetVersion, targetSpan, noninvertibleStartHistory, noninvertibleEndHistory);
            }
            else
            {
                // we are looking for a version prior to the cached version.
                int start = HighFidelityTrackingPoint.TrackPositionBackwardInTime
                    (startMode, this.fidelity == TrackingFidelityMode.Backward ? noninvertibleStartHistory : null, cached.Span.Start, cached.Version, targetVersion);
                int end = HighFidelityTrackingPoint.TrackPositionBackwardInTime
                    (endMode, this.fidelity == TrackingFidelityMode.Backward ? noninvertibleEndHistory : null, cached.Span.End, cached.Version, targetVersion);

                targetSpan = Span.FromBounds(start, System.Math.Max(start, end));
            }
            return targetSpan;
        }
        #endregion

        #region Diagnostic Support

        public override string ToString()
        {
            VersionSpanHistory c = this.cachedSpan;
            System.Text.StringBuilder sb = new System.Text.StringBuilder("*");
            sb.Append(ToString(c.Version, c.Span, this.trackingMode));
            if (c.NoninvertibleStartHistory != null)
            {
                sb.Append("[Start");
                foreach (VersionNumberPosition vp in c.NoninvertibleStartHistory)
                {
                    sb.Append(string.Format(System.Globalization.CultureInfo.CurrentCulture, "V{0}@{1}", vp.VersionNumber, vp.Position));
                }
                sb.Append("]");
            }
            if (c.NoninvertibleEndHistory != null)
            {
                sb.Append("[End");
                foreach (VersionNumberPosition vp in c.NoninvertibleEndHistory)
                {
                    sb.Append(string.Format(System.Globalization.CultureInfo.CurrentCulture, "V{0}@{1}", vp.VersionNumber, vp.Position));
                }
                sb.Append("]");
            }
            return sb.ToString();
        }
        #endregion
    }
}
