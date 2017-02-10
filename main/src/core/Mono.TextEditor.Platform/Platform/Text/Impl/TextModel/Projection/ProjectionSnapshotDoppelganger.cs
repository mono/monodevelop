using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Implementation;

namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    /// <summary>
    /// This specialization is used when the owner of the projection buffer has asserted that its contents exactly shadow
    /// those of another buffer (the "doppelganger"). In that case, some operations can be made much simpler by
    /// directly accessing the doppelganger instead of laboriously mapping through layers of projection. Those simplifications
    /// are implemented by this class.
    /// </summary>
    internal class ProjectionSnapshotDoppelganger : ProjectionSnapshot
    {
        private ITextSnapshot doppelSnap;

        public ProjectionSnapshotDoppelganger(ProjectionBuffer projectionBuffer, ITextVersion version, IList<SnapshotSpan> sourceSpans, ITextSnapshot doppelSnap)
            : base(projectionBuffer, version, sourceSpans)
        {
            this.doppelSnap = doppelSnap;
        }

        #region Line Methods
        public override ITextSnapshotLine GetLineFromLineNumber(int lineNumber)
        {
            ITextSnapshotLine doppelLine = this.doppelSnap.GetLineFromLineNumber(lineNumber);
            return new TextSnapshotLine(this, new LineSpan(lineNumber, doppelLine.Extent, doppelLine.LineBreakLength));
        }

        public override ITextSnapshotLine GetLineFromPosition(int position)
        {
            ITextSnapshotLine doppelLine = doppelSnap.GetLineFromPosition(position);
            return new TextSnapshotLine(this, new LineSpan(doppelLine.LineNumber, doppelLine.Extent, doppelLine.LineBreakLength));
        }

        public override int GetLineNumberFromPosition(int position)
        {
            return this.doppelSnap.GetLineNumberFromPosition(position);
        }
        #endregion

        #region Text Fetching
        public override string GetText(Span span)
        {
            return this.doppelSnap.GetText(span);
        }

        public override char this[int position]
        {
            get
            {
                return this.doppelSnap[position];
            }
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.doppelSnap.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override char[] ToCharArray(int startIndex, int length)
        {
            return this.doppelSnap.ToCharArray(startIndex, length);
        }
        #endregion

        #region Writing
        public override void Write(System.IO.TextWriter writer)
        {
            this.doppelSnap.Write(writer);
        }

        public override void Write(System.IO.TextWriter writer, Span span)
        {
            this.doppelSnap.Write(writer, span);
        }
        #endregion
    }
}