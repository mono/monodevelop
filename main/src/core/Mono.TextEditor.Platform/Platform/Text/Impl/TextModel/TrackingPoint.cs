namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;

    /// <summary>
    /// Base class for a tracking position in a particular <see cref="ITextBuffer"/>.
    /// </summary>
    internal abstract partial class TrackingPoint : ITrackingPoint
    {
        #region State and Construction
        protected readonly PointTrackingMode trackingMode;

        protected TrackingPoint(ITextVersion version, int position, PointTrackingMode trackingMode)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }
            if (position < 0 | position > version.Length)
            {
                throw new ArgumentOutOfRangeException("position");
            }
            if (trackingMode < PointTrackingMode.Positive || trackingMode > PointTrackingMode.Negative)
            {
                throw new ArgumentOutOfRangeException("trackingMode");
            }

            this.trackingMode = trackingMode;
        }
        #endregion

        #region ITrackingPoint members
        public abstract ITextBuffer TextBuffer { get; }

        public PointTrackingMode TrackingMode
        {
            get { return this.trackingMode; }
        }

        public abstract TrackingFidelityMode TrackingFidelity { get; }

        public int GetPosition(ITextVersion version)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }
            if (version.TextBuffer != this.TextBuffer)
            {
                throw new ArgumentException(Strings.InvalidVersion);
            }
            return TrackPosition(version);
        }

        public int GetPosition(ITextSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }
            if (snapshot.TextBuffer != this.TextBuffer)
            {
                throw new ArgumentException(Strings.InvalidSnapshot);
            }
            return TrackPosition(snapshot.Version);
        }

        public SnapshotPoint GetPoint(ITextSnapshot snapshot)
        {
            return new SnapshotPoint(snapshot, GetPosition(snapshot));
        }

        public char GetCharacter(ITextSnapshot snapshot)
        {
            return GetPoint(snapshot).GetChar();
        }
        #endregion

        protected abstract int TrackPosition(ITextVersion targetVersion);

        #region Diagnostic Support
        protected static string PointTrackingModeToString(PointTrackingMode trackingMode)
        {
            return trackingMode == PointTrackingMode.Positive ? "→" : "←";
        }

        protected static string ToString(ITextVersion version, int position, PointTrackingMode trackingMode)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "V{0} {2}@{1}",
                                 version.VersionNumber, position, PointTrackingModeToString(trackingMode));
        }
        #endregion
    }
}
