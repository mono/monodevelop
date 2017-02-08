namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// Implementation of ITrackingPoint for the Forward TrackingFidelityMode.
    /// Results of moving backwards in version space are not guaranteed to match results
    /// for the same version when moving forward (i.e., no special support for noninvertible transitions).
    /// No special support for Undo/Redo.
    /// </summary>
    internal class ForwardFidelityTrackingPoint : TrackingPoint
    {
        #region State and Construction
        private class VersionPosition
        {
            private ITextVersion version;
            private int position;

            public VersionPosition(ITextVersion version, int position)
            {
                this.version = version;
                this.position = position;
            }

            public ITextVersion Version { get { return this.version; } }
            public int Position { get { return this.position; } }
        }

        private VersionPosition cachedPosition;

        public ForwardFidelityTrackingPoint(ITextVersion version, int position, PointTrackingMode trackingMode)
            : base(version, position, trackingMode)
        {
            this.cachedPosition = new VersionPosition(version, position);
        }
        #endregion

        #region Overridden methods
        public override ITextBuffer TextBuffer
        {
            get { return this.cachedPosition.Version.TextBuffer; }
        }

        public override TrackingFidelityMode TrackingFidelity
        {
            get { return TrackingFidelityMode.Forward; }
        }

        protected override int TrackPosition(ITextVersion targetVersion)
        {
            // Compute the new position on the requested snapshot.
            //
            // This method can be called simultaneously from multiple threads, and must be fast.
            //
            // We are relying on the atomicity of pointer copies (this.cachedPosition might change after we've
            // fetched it but we will always get a self-consistent VersionPosition). This ensures we
            // have proper behavior when called from multiple threads--multiple threads may all track and update the
            // cached value if called at inconvenient times, but they will return consistent results.
            // ForwardFidelity points do not support tracking backward, so consistency is not guaranteed in that case.

            VersionPosition cached = this.cachedPosition;
            int targetPosition;
            if (targetVersion == cached.Version)
            {
                targetPosition = cached.Position;
            }
            else if (targetVersion.VersionNumber > cached.Version.VersionNumber)
            {
                // Roll the cached version forward to the requested version.
                targetPosition = Tracking.TrackPositionForwardInTime(this.trackingMode, cached.Position, cached.Version, targetVersion);

                // Cache new cached version.
                this.cachedPosition = new VersionPosition(targetVersion, targetPosition);
            }
            else
            {
                // Roll backwards from the cached version.
                targetPosition = Tracking.TrackPositionBackwardInTime(this.trackingMode, cached.Position, cached.Version, targetVersion);
            }
            return targetPosition;
        }
        #endregion

        #region Diagnostic Support
        public override string ToString()
        {
            VersionPosition c = this.cachedPosition;
            return ToString(c.Version, c.Position, this.trackingMode);
        }
        #endregion
    }
}
