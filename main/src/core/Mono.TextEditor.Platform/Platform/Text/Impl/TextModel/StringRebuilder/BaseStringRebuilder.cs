using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal abstract class BaseStringRebuilder : IStringRebuilder
    {
        #region Private
        private IStringRebuilder Assemble(Span left, Span right)
        {
            if (left.Length == 0)
                return this.Substring(right);
            else if (right.Length == 0)
                return this.Substring(left);
            else if (left.Length + right.Length == this.Length)
                return this;
            else
                return BinaryStringRebuilder.Create(this.Substring(left), this.Substring(right));
        }

        private IStringRebuilder Assemble(Span left, IStringRebuilder text, Span right)
        {
            if (text.Length == 0)
                return Assemble(left, right);
            else if (left.Length == 0)
                return (right.Length == 0) ? text : BinaryStringRebuilder.Create(text, this.Substring(right));
            else if (right.Length == 0)
                return BinaryStringRebuilder.Create(this.Substring(left), text);
            else if (left.Length < right.Length)
                return BinaryStringRebuilder.Create(BinaryStringRebuilder.Create(this.Substring(left), text),
                                                    this.Substring(right));
            else
                return BinaryStringRebuilder.Create(this.Substring(left),
                                                    BinaryStringRebuilder.Create(text, this.Substring(right)));
        }
        #endregion

        #region IStringRebuilder Members
        public abstract int Length { get; }
        public abstract int LineBreakCount { get; }
        public abstract int GetLineNumberFromPosition(int position);
        public abstract LineSpan GetLineFromLineNumber(int lineNumber);
        public abstract IStringRebuilder GetLeaf(int position, out int offset);
        public abstract char this[int index] { get; }
        public abstract string GetText(Span span);

        public char[] ToCharArray(int startIndex, int length)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex");

            if ((length < 0) || (startIndex + length > this.Length) || (startIndex + length < 0))
                throw new ArgumentOutOfRangeException("length");

            char[] copy = new char[length];
            this.CopyTo(startIndex, copy, 0, length);

            return copy;
        }

        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);
        public abstract void Write(TextWriter writer, Span span);
        public abstract IStringRebuilder Substring(Span span);

        public IStringRebuilder Delete(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");

            return this.Assemble(Span.FromBounds(0, span.Start), Span.FromBounds(span.End, this.Length));
        }

        public IStringRebuilder Replace(Span span, string text)
        {
            return this.Replace(span, SimpleStringRebuilder.Create(text));
        }

        public IStringRebuilder Replace(Span span, IStringRebuilder text)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");
            if (text == null)
                throw new ArgumentNullException("text");

            return this.Assemble(Span.FromBounds(0, span.Start), text, Span.FromBounds(span.End, this.Length));
        }

        public IStringRebuilder Append(string text)
        {
            return this.Insert(this.Length, text);
        }

        public IStringRebuilder Append(IStringRebuilder text)
        {
            return this.Insert(this.Length, text);
        }

        public IStringRebuilder Insert(int position, string text)
        {
            return this.Insert(position, SimpleStringRebuilder.Create(text));
        }

        public IStringRebuilder Insert(int position, IStringRebuilder text)
        {
            if ((position < 0) || (position > this.Length))
                throw new ArgumentOutOfRangeException("position");
            if (text == null)
                throw new ArgumentNullException("text");

            return this.Assemble(Span.FromBounds(0, position), text, Span.FromBounds(position, this.Length));
        }

        public IStringRebuilder Insert(int position, ITextStorage storage)
        {
            return this.Insert(position, SimpleStringRebuilder.Create(storage));
        }

        public abstract int Depth { get; }

        public abstract IStringRebuilder Child(bool rightSide);

        public abstract bool EndsWithReturn { get; }
        public abstract bool StartsWithNewLine { get; }
        #endregion
    }
}
