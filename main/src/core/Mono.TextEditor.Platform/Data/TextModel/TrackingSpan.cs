namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;

    /// <summary>
    /// Base class for a tracking span in a particular <see cref="ITextBuffer"/>.
    /// </summary>
    internal abstract partial class TrackingSpan : ITrackingSpan
    {
        #region State and Construction
        protected readonly SpanTrackingMode trackingMode;

        public TrackingSpan(ITextVersion version, Span span, SpanTrackingMode trackingMode)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }
            if (span.End > version.Length)
            {
                throw new ArgumentOutOfRangeException("span");
            }
            if (trackingMode < SpanTrackingMode.EdgeExclusive || trackingMode > SpanTrackingMode.Custom)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }

            this.trackingMode = trackingMode;
        }
        #endregion

        #region ITrackingSpan members
        public abstract ITextBuffer TextBuffer { get; }

        public SpanTrackingMode TrackingMode
        {
            get { return this.trackingMode; }
        }

        public abstract TrackingFidelityMode TrackingFidelity { get; }

        public Span GetSpan(ITextVersion version)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }
            if (version.TextBuffer != this.TextBuffer)
            {
                throw new ArgumentException(Strings.InvalidVersion);
            }
            return TrackSpan(version);
        }

        public SnapshotSpan GetSpan(ITextSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }
            if (snapshot.TextBuffer != this.TextBuffer)
            {
                throw new ArgumentException(Strings.InvalidSnapshot);
            }

            return new SnapshotSpan(snapshot, TrackSpan(snapshot.Version));
        }

        public SnapshotPoint GetStartPoint(ITextSnapshot snapshot)
        {
            SnapshotSpan s = this.GetSpan(snapshot);
            return new SnapshotPoint(snapshot, s.Start);
        }

        public SnapshotPoint GetEndPoint(ITextSnapshot snapshot)
        {
            SnapshotSpan s = this.GetSpan(snapshot);
            return new SnapshotPoint(snapshot, s.End);
        }

        public string GetText(ITextSnapshot snapshot)
        {
            return GetSpan(snapshot).GetText();
        }
        #endregion

        #region Helpers
        protected abstract Span TrackSpan(ITextVersion targetVersion);
        #endregion

        #region Diagnostic Support
        protected static string SpanTrackingModeToString(SpanTrackingMode trackingMode)
        {
            switch (trackingMode)
            {
                case SpanTrackingMode.EdgeExclusive:
                    return "→←";
                case SpanTrackingMode.EdgeInclusive:
                    return "←→";
                case SpanTrackingMode.EdgeNegative:
                    return "←←";
                case SpanTrackingMode.EdgePositive:
                    return "→→";
                case SpanTrackingMode.Custom:
                    return "custom";
                default:
                    return "??";
            }
        }

        protected static string ToString(ITextVersion version, Span span, SpanTrackingMode trackingMode)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "V{0} {2}@{1}",
                                 version.VersionNumber, span.ToString(), SpanTrackingModeToString(trackingMode));
        }
        #endregion
    }
}
