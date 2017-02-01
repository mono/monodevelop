namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// Implementation of ITrackingSpan for the Forward TrackingFidelityMode.
    /// Results of moving backwards in version space are not guaranteed to match results
    /// for the same version when moving forward (i.e., no special support for noninvertible transitions).
    /// No special support for Undo/Redo.
    /// </summary>
    internal class ForwardFidelityTrackingSpan : TrackingSpan
    {
        #region State and Construction
        private class VersionSpan
        {
            private ITextVersion version;
            private Span span;

            public VersionSpan(ITextVersion version, Span span)
            {
                this.version = version;
                this.span = span;
            }

            public ITextVersion Version { get { return this.version; } }
            public Span Span { get { return this.span; } }
        }

        private VersionSpan cachedSpan;

        public ForwardFidelityTrackingSpan(ITextVersion version, Span span, SpanTrackingMode trackingMode)
            : base(version, span, trackingMode)
        {
            this.cachedSpan = new VersionSpan(version, span);
        }
        #endregion

        #region ITrackingSpan members
        public override ITextBuffer TextBuffer
        {
            get { return this.cachedSpan.Version.TextBuffer; }
        }

        public override TrackingFidelityMode TrackingFidelity
        {
            get { return TrackingFidelityMode.Forward; }
        }

        protected override Span TrackSpan(ITextVersion targetVersion)
        {
            // Compute the new span on the requested snapshot.
            //
            // This method can be called simultaneously from multiple threads, and must be fast.
            //
            // We are relying on the atomicity of pointer copies (this.cachedSpan might change after we've
            // fetched it but we will always get a self-consistent VersionPosition). This ensures we
            // have proper behavior when called from multiple threads--multiple threads may all track and update the
            // cached value if called at inconvenient times, but they will return consistent results.
            // ForwardFidelity spans do not support tracking backward, so consistency is not guaranteed in that case.

            VersionSpan cached = this.cachedSpan;
            Span targetSpan;
            if (targetVersion == cached.Version)
            {
                targetSpan = cached.Span;
            }
            else if (targetVersion.VersionNumber  > cached.Version.VersionNumber)
            {
                // Compute the target span by going forward from the cached version
                targetSpan = TrackSpanForwardInTime(cached.Span, cached.Version, targetVersion);

                // Update the cached value
                this.cachedSpan = new VersionSpan(targetVersion, targetSpan);
            }
            else
            {
                // Roll backwards from the cached version.
                targetSpan = TrackSpanBackwardInTime(cached.Span, cached.Version, targetVersion);
            }
            return targetSpan;
        }
        #endregion

        #region Helpers
        protected virtual Span TrackSpanForwardInTime(Span span, ITextVersion currentVersion, ITextVersion targetVersion)
        {
            return Tracking.TrackSpanForwardInTime(this.trackingMode, span, currentVersion, targetVersion);
        }

        /// <summary>
        /// Backward mapping. Used by all fidelity modes for mapping backwards under various circumstances.
        /// </summary>
        protected virtual Span TrackSpanBackwardInTime(Span span, ITextVersion currentVersion, ITextVersion targetVersion)
        {
            return Tracking.TrackSpanBackwardInTime(this.trackingMode, span, currentVersion, targetVersion);
        }
        #endregion

        #region Diagnostic Support
        public override string ToString()
        {
            VersionSpan c = this.cachedSpan;
            return ToString(c.Version, c.Span, this.trackingMode);
        }
        #endregion
    }
}
