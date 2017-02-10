namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Diagnostics;

    internal partial class TextSnapshotLine : ITextSnapshotLine
    {
        private readonly int lineNumber;
        private readonly int lineBreakLength;
        private readonly SnapshotSpan extent;

        public TextSnapshotLine(ITextSnapshot snapshot, LineSpan lineSpan)
        {
            this.extent = new SnapshotSpan(snapshot, lineSpan.Extent);

            //This is inner loop code called only from private methods so we don't need to guard against bad data in released bits.
            Debug.Assert(lineSpan.EndIncludingLineBreak <= snapshot.Length);

            this.lineNumber = lineSpan.LineNumber;
            this.lineBreakLength = lineSpan.LineBreakLength;
        }

        /// <summary>
        /// ITextSnapshot in which the line appears.
        /// </summary>
        public ITextSnapshot Snapshot
        {
            get { return this.extent.Snapshot; }
        }

        /// <summary>
        /// The 0-origin line number of the line.
        /// </summary>
        public int LineNumber
        {
            get
            {
                return this.lineNumber;
            }
        }

        /// <summary>
        /// Position in TextBuffer of the first character in the line.
        /// </summary>
        public SnapshotPoint Start
        {
            get { return this.extent.Start; }
        }

        /// <summary>
        /// Length of the line, excluding any line break characters.
        /// </summary>
        public int Length
        {
            get { return this.extent.Length; }
        }

        /// <summary>
        /// Length of the line, including any line break characters.
        /// </summary>
        public int LengthIncludingLineBreak
        {
            get { return this.extent.Length + this.lineBreakLength; }
        }

        /// <summary>
        /// Length of line break characters (always falls in the range [0..2])
        /// </summary>
        public int LineBreakLength
        {
            get { return this.lineBreakLength; }
        }

        /// <summary>
        /// The position of the first character past the end of the line, excluding any
        /// line break characters (thus will address a line break character, except 
        /// for the last line in the buffer).
        /// </summary>
        public SnapshotPoint End
        {
            get { return this.extent.End; }
        }

        /// <summary>
        /// The position of the first character past the end of the line, including any
        /// line break characters (thus will address the first character in 
        /// the succeeding line, unless this is the last line).
        /// </summary>
        public SnapshotPoint EndIncludingLineBreak
        {
            get { return new SnapshotPoint(this.extent.Snapshot, this.extent.Span.End + this.lineBreakLength); }
        }

        /// <summary>
        /// The extent of the line, excluding any line break characters.
        /// </summary>
        public SnapshotSpan Extent
        {
            get
            {
                return this.extent;
            }
        }

        /// <summary>
        /// The extent of the line, including any line break characters.
        /// </summary>
        public SnapshotSpan ExtentIncludingLineBreak
        {
            get { return new SnapshotSpan(this.extent.Start, this.LengthIncludingLineBreak); }
        }

        /// <summary>
        /// The text of the line, excluding any line break characters.
        /// May return incorrect results or fail if the text buffer has changed since the 
        /// ITextBufferLine was created.
        /// </summary>
        public string GetText()
        {
            return this.Extent.GetText();
        }

        /// <summary>
        /// The text of the line, including any line break characters.
        /// May return incorrect results of fail if the text buffer has changed since the 
        /// ITextBufferLine was created.
        /// </summary>
        /// <returns></returns>
        public string GetTextIncludingLineBreak()
        {
            return this.ExtentIncludingLineBreak.GetText();
        }

        /// <summary>
        /// The string consisting of the line break characters (if any) at the
        /// end of the line. Has zero length for the last line in the buffer.
        /// May return incorrect results of fail if the text buffer has changed since the 
        /// ITextBufferLine was created.
        /// </summary>
        /// <returns></returns>
        public string GetLineBreakText()
        {
            return this.extent.Snapshot.GetText(new Span(this.Extent.Span.End, this.lineBreakLength));
        }
    }
}