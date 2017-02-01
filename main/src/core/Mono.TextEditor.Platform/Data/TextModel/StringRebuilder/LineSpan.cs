using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// Describe a line of text.
    /// </summary>
    internal struct LineSpan
    {
        #region Private
        private readonly int _lineNumber;
        private readonly Span _span;
        private readonly int _lineBreakLength;
        #endregion
        
        /// <summary>
        /// Create a new LineSpan given the span of the line's text.
        /// </summary>
        /// <param name="lineNumber">Line number of the line.</param>
        /// <param name="span">Start and end of the line's text.</param>
        /// <param name="lineBreakLength">Length of the line's line break.</param>
        /// <returns>The newly created lineSpan.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineBreakLength"/> is less than 0, greater than 2 or
        ///                                               <paramref name="span"/>.End + <paramref name="lineBreakLength"/> overflows.</exception>
        public LineSpan(int lineNumber, Span span, int lineBreakLength)
        {
            if (lineNumber < 0)
                throw new ArgumentOutOfRangeException("lineNumber");
            if ((lineBreakLength > 2) || (span.End + lineBreakLength < span.End))
                throw new ArgumentOutOfRangeException("lineBreakLength");

            _lineNumber = lineNumber;
            _span = span;
            _lineBreakLength = lineBreakLength;
        }

        /// <summary>
        /// The line number of the line.
        /// </summary>
        public int LineNumber { get { return _lineNumber; } }

        /// <summary>
        /// The starting index of the line.
        /// </summary>
        public int Start { get { return _span.Start; } }

        /// <summary>
        /// The ending index of the line, excluding line break characters.
        /// </summary>
        public int End { get { return _span.End; } }

        /// <summary>
        /// The ending index of the line, including line break characters.
        /// </summary>
        public int EndIncludingLineBreak { get { return _span.End + _lineBreakLength; } }

        /// <summary>
        /// The length of the line, excluding line break characters.
        /// </summary>
        public int Length { get { return _span.Length; } }

        /// <summary>
        /// The number of line break characters (always an integer between zero and two, inclusive).
        /// </summary>
        public int LineBreakLength { get { return _lineBreakLength; } }

        /// <summary>
        /// The length of the line, including line break characters.
        /// </summary>
        public int LengthIncludingLineBreak { get { return _span.Length + _lineBreakLength; } }

        /// <summary>
        /// The span covered by the line, excluding line break characters.
        /// </summary>
        public Span Extent { get { return _span; } }

        /// <summary>
        /// The span covered by the line, including line break characters.
        /// </summary>
        public Span ExtentIncludingLineBreak { get { return Span.FromBounds(_span.Start, this.EndIncludingLineBreak); } }

        #region Overridden methods and operators

        /// <summary>
        /// Useful debugging aid.
        /// </summary>
        public override string ToString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0}, {1}+{2}]", this.Start, this.End, this.LineBreakLength);
        }

        /// <summary>
        /// HashCode suitable for value semantics.
        /// </summary>
        public override int GetHashCode()
        {
            return _span.GetHashCode();
        }

        /// <summary>
        /// Value semantics for equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is LineSpan)
            {
                LineSpan other = (LineSpan)obj;
                return other == this;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Value semantics for equality.
        /// </summary>
        public static bool operator ==(LineSpan left, LineSpan right)
        {
            return ((left._lineBreakLength == right._lineBreakLength) &&
                (left._lineNumber == right._lineNumber) &&
                (left._span == right._span));
        }

        /// <summary>
        /// Value semantics for inequality.
        /// </summary>
        public static bool operator !=(LineSpan left, LineSpan right)
        {
            return !(left == right);
        }

        #endregion
    }
}
