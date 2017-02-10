using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal sealed class SimpleStringRebuilder : BaseStringRebuilder
    {
        #region Private
        private readonly ITextStorage _storage;
        private readonly Span _textSpan;         //subspan of _storage contained in this SimpleStringRebuilder
        private readonly Span _lineBreakSpan;    //subspan of _storage.LineBreaks that contains all line breaks in this SimpleStringRebuilder
        private readonly bool _startsWithNewLine;
        private readonly bool _endsWithReturn;

        private readonly static int[] _emptyLineBreaks = new int[0];
        private readonly static IStringRebuilder _empty = new SimpleStringRebuilder(string.Empty);

        private SimpleStringRebuilder(ITextStorage storage)
        {
            _storage = storage;
            _textSpan = new Span(0, storage.Length);
            _lineBreakSpan = new Span(0, _storage.LineBreaks.Length);
            if (_textSpan.Length > 0)
            {
                _startsWithNewLine = storage.IsNewLine(_textSpan.Start);
                _endsWithReturn = storage.IsReturn(_textSpan.End - 1);
            }
        }

        /// <summary>
        /// Construct a new SimpleStringRebuilder.
        /// </summary>
        private SimpleStringRebuilder(string source)
        {
            //_storage = new SimpleTextStorage(source);
            _storage = SimpleTextStorage.Create(source);
            _textSpan = new Span(0, source.Length);
            _lineBreakSpan = new Span(0, _storage.LineBreaks.Length);
            if (source.Length > 0)
            {
                _startsWithNewLine = source[0] == '\n';
                _endsWithReturn = source[source.Length - 1] == '\r';
            }
        }

        /// <summary>
        /// Construct a new SimpleStringRebuilder that is a substring of another string rebuilder.
        /// </summary>
        private SimpleStringRebuilder(Span span, SimpleStringRebuilder simpleSource)
        {
            _textSpan = new Span(span.Start + simpleSource._textSpan.Start, span.Length);
            _storage = simpleSource._storage;

            int firstLineNumber = simpleSource.GetLineNumberFromPosition(span.Start) + simpleSource._lineBreakSpan.Start;
            int lastLineNumber = simpleSource.GetLineNumberFromPosition(span.End) + simpleSource._lineBreakSpan.Start;

            //Handle the special case where the end position falls in the middle of a linebreak.
            if ((lastLineNumber < simpleSource._lineBreakSpan.End) &&
                (span.End > simpleSource._storage.LineBreaks.StartOfLineBreak(lastLineNumber) - simpleSource._textSpan.Start))
            {
                ++lastLineNumber;
            }

            if (firstLineNumber == lastLineNumber)
            {
                //_lineBreakSpan defaults to 0, which is what we want.
            }
            else
            {
                _lineBreakSpan = Span.FromBounds(firstLineNumber, lastLineNumber);
            }

            if (_textSpan.Length > 0)
            {
                _startsWithNewLine = _storage.IsNewLine(_textSpan.Start);
                _endsWithReturn = _storage.IsReturn(_textSpan.End - 1);
            }
        }

        /// <summary>
        /// Consolidate two string rebuilders, taking advantage of the fact that they have already extracted the line breaks.
        /// </summary>
        private SimpleStringRebuilder(IStringRebuilder left, IStringRebuilder right)
        {
            _textSpan = new Span(0, left.Length + right.Length);
            string text = left.GetText(new Span(0, left.Length)) + right.GetText(new Span(0, right.Length));

            int[] lineBreaks;
            if ((left.LineBreakCount == 0) && (right.LineBreakCount == 0))
            {
                lineBreaks = _emptyLineBreaks;
                //_lineBreakSpan defaults to 0, 0 which is what we want
            }
            else
            {
                int offset = 0;
                if ((text[left.Length] == '\n') && (text[left.Length - 1] == '\r'))
                {
                    //We have a \r\n spanning the seam ... add that as a special linebreak later.
                    offset = 1;
                }

                lineBreaks = new int[left.LineBreakCount + right.LineBreakCount - offset];
                int lastLineBreak = 0;

                int leftLines = left.LineBreakCount - offset;
                for (int i = 0; (i < leftLines); ++i)
                {
                    LineSpan lineSpan = left.GetLineFromLineNumber(i);
                    lineBreaks[lastLineBreak++] = lineSpan.End;
                }

                if (offset == 1)
                {
                    lineBreaks[lastLineBreak++] = left.Length - 1;
                }

                for (int i = offset; (i < right.LineBreakCount); ++i)
                {
                    LineSpan lineSpan = right.GetLineFromLineNumber(i);
                    lineBreaks[lastLineBreak++] = lineSpan.End + left.Length;
                }

                _lineBreakSpan = new Span(0, lastLineBreak);
            }
            _storage = SimpleTextStorage.Create(text, lineBreaks);
            _startsWithNewLine = left.StartsWithNewLine;
            _endsWithReturn = right.EndsWithReturn;
        }
        #endregion

        public static IStringRebuilder Create(ITextStorageLoader loader)
        {
            if (loader == null)
                throw new ArgumentNullException("loader");

            IStringRebuilder content = _empty;
            foreach (ITextStorage storage in loader.Load())
            {
                content = content.Insert(content.Length, storage);
            }
            return content;
        }

        public static IStringRebuilder Create(ITextStorage storage)
        {
            if (storage == null)
                throw new ArgumentNullException("storage");

            return (storage.Length == 0)
                ? _empty
                : new SimpleStringRebuilder(storage);
        }

        public static IStringRebuilder Create(string text)
        {
            // called when performing simple text insertion or deletion.
            if (text == null)
                throw new ArgumentNullException("text");

            return (text.Length == 0)
                   ? _empty
                   : new SimpleStringRebuilder(text);
        }

        public static IStringRebuilder Create(IStringRebuilder left, IStringRebuilder right)
        {
            return new SimpleStringRebuilder(left, right);
        }

        public override string ToString()
        {
            return _storage.GetText(_textSpan.Start, _textSpan.Length);
        }

        #region IStringRebuilder Members
        public override int Length
        {
            get { return _textSpan.Length; }
        }

        public override int LineBreakCount
        {
            get { return _lineBreakSpan.Length; }
        }

        public override int GetLineNumberFromPosition(int position)
        {
            if ((position < 0) || (position > _textSpan.Length))
                throw new ArgumentOutOfRangeException("position");

            //Convert position to a position relative to the start of _text.
            if (position == _textSpan.Length)
            {
                //Handle positions at the end of the span as a special case since otherwise we
                //return the incorrect value if the last line break extends past the end of _textSpan.
                return _lineBreakSpan.Length;
            }

            position += _textSpan.Start;

            int start = _lineBreakSpan.Start;
            int end = _lineBreakSpan.End;

            while (start < end)
            {
                int middle = (start + end) / 2;
                if (position < _storage.LineBreaks.EndOfLineBreak(middle))
                    end = middle;
                else
                    start = middle + 1;
            }

            return start - _lineBreakSpan.Start;
        }

        public override LineSpan GetLineFromLineNumber(int lineNumber)
        {
            if ((lineNumber < 0) || (lineNumber > _lineBreakSpan.Length))
                throw new ArgumentOutOfRangeException("lineNumber");

            ILineBreaks lineBreaks = _storage.LineBreaks;

            int absoluteLineNumber = _lineBreakSpan.Start + lineNumber;

            int start = (lineNumber == 0)
                        ? 0
                        : (Math.Min(_textSpan.End, lineBreaks.EndOfLineBreak(absoluteLineNumber - 1)) - _textSpan.Start);

            int end;
            int breakLength;
            if (lineNumber < _lineBreakSpan.Length)
            {
                end = Math.Max(_textSpan.Start, lineBreaks.StartOfLineBreak(absoluteLineNumber));
                breakLength = Math.Min(_textSpan.End, lineBreaks.EndOfLineBreak(absoluteLineNumber)) - end;

                end -= _textSpan.Start;
            }
            else
            {
                end = _textSpan.Length;
                breakLength = 0;
            }

            return new LineSpan(lineNumber, Span.FromBounds(start, end), breakLength);
        }

        public override IStringRebuilder GetLeaf(int position, out int offset)
        {
            offset = 0;
            return this;
        }

        public override char this[int index]
        {
            get
            {
                if ((index < 0) || (index >= this.Length))
                    throw new ArgumentOutOfRangeException("index");

                return _storage[index + _textSpan.Start];
            }
        }

        public override string GetText(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");

            return _storage.GetText(span.Start + _textSpan.Start, span.Length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException("sourceIndex");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException("destinationIndex");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            if ((sourceIndex + count > this.Length) || (sourceIndex + count < 0))
                throw new ArgumentOutOfRangeException("count");

            if ((destinationIndex + count > destination.Length) || (destinationIndex + count < 0))
                throw new ArgumentOutOfRangeException("count");

            _storage.CopyTo(sourceIndex + _textSpan.Start, destination, destinationIndex, count);
        }

        public override void Write(TextWriter writer, Span span)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");

            _storage.Write(writer, span.Start + _textSpan.Start, span.Length);
        }

        public override IStringRebuilder Substring(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");

            if (span.Length == this.Length)
                return this;
            else if (span.Length == 0)
                return _empty;
            else
                return new SimpleStringRebuilder(span, this);
        }

        public override int Depth
        {
            get { return 0; }
        }

        public override IStringRebuilder Child(bool rightSide)
        {
            throw new InvalidOperationException();
        }

        public override bool StartsWithNewLine
        {
            get { return _startsWithNewLine; }
        }

        public override bool EndsWithReturn
        {
            get { return _endsWithReturn; }
        }
        #endregion
    }
}
