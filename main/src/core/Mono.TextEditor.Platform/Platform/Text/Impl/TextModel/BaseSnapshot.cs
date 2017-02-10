namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Base class for all varieties of Text Snapshots.
    /// </summary>
    internal abstract class BaseSnapshot : ITextSnapshot
    {
        #region State and Construction
        protected readonly ITextVersion version;
        private readonly IContentType contentType;

        protected BaseSnapshot(ITextVersion version)
        {
            this.version = version;
            // we must extract the content type here, because the content type of the text buffer may change later.
            this.contentType = version.TextBuffer.ContentType;
        }
        #endregion

        #region ITextSnapshot implementations

        public ITextBuffer TextBuffer 
        {
            get { return this.TextBufferHelper; }
        }

        public IContentType ContentType
        {
            get { return this.contentType; }
        }

        public ITextVersion Version
        {
            get { return this.version; }
        }

        public string GetText(int startIndex, int length)
        {
            return GetText(new Span(startIndex, length));
        }

        public string GetText()
        {
            return GetText(new Span(0, this.Length));
        }

        #region Point and Span factories
        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        {
            return this.version.CreateTrackingPoint(position, trackingMode);
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            return this.version.CreateTrackingPoint(position, trackingMode, trackingFidelity);
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        {
            return this.version.CreateTrackingSpan(start, length, trackingMode);
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            return this.version.CreateTrackingSpan(start, length, trackingMode, trackingFidelity);
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
        {
            return this.version.CreateTrackingSpan(span, trackingMode, TrackingFidelityMode.Forward);
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            return this.version.CreateTrackingSpan(span, trackingMode, trackingFidelity);
        }
        #endregion
        #endregion

        #region ITextSnapshot abstract methods
        protected abstract ITextBuffer TextBufferHelper { get; }
        public abstract int Length { get; }
        public abstract int LineCount { get; }
        public abstract string GetText(Span span);
        public abstract char[] ToCharArray(int startIndex, int length);
        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);
        public abstract char this[int position] { get; }
        public abstract ITextSnapshotLine GetLineFromLineNumber(int lineNumber);
        public abstract ITextSnapshotLine GetLineFromPosition(int position);
        public abstract int GetLineNumberFromPosition(int position);
        public abstract IEnumerable<ITextSnapshotLine> Lines { get; }
        public abstract void Write(System.IO.TextWriter writer, Span span);
        public abstract void Write(System.IO.TextWriter writer);
        #endregion

        public override string ToString()
        {
            return String.Format("version: {0} lines: {1} length: {2} \r\n content: {3}",
                Version.VersionNumber, LineCount, Length,
                Microsoft.VisualStudio.Text.Utilities.TextUtilities.Escape(this.GetText(0, Math.Min(40, this.Length))));
        }

#if _DEBUG
        internal string DebugOnly_AllText
        {
            get
            {
                return this.GetText(0, Math.Min(this.Length, 1024*1024));
            }
        }
#endif
    }
}
