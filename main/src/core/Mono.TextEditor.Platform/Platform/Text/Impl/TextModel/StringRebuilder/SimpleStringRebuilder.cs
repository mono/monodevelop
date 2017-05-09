//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal sealed class SimpleStringRebuilder : StringRebuilder
    {
        #region Private
        private readonly ITextStorage _storage;
        private readonly int _textSpanStart;         //subspan of _storage contained in this SimpleStringRebuilder
        private readonly int _lineBreakSpanStart;    //subspan of _storage.LineBreaks that contains all line breaks in this SimpleStringRebuilder
        private int TextSpanEnd { get { return _textSpanStart + this.Length; } }
        private int LineBreakSpanEnd { get { return _lineBreakSpanStart + this.LineBreakCount; } }
        private readonly bool _startsWithNewLine;
        private readonly bool _endsWithReturn;

        private readonly static int[] _emptyLineBreaks = new int[0];
        private readonly static StringRebuilder _empty = new SimpleStringRebuilder(string.Empty);

        private SimpleStringRebuilder(ITextStorage storage)
            : base(storage.Length, storage.LineBreaks.Length, 0)
        {
            _storage = storage;
            if (this.Length > 0)
            {
                _startsWithNewLine = storage.IsNewLine(_textSpanStart);
                _endsWithReturn = storage.IsReturn(this.TextSpanEnd - 1);
            }
        }

        /// <summary>
        /// Construct a new SimpleStringRebuilder.
        /// </summary>
        private SimpleStringRebuilder(string source)
            : this(SimpleTextStorage.Create(source))
        {
        }

        /// <summary>
        /// Construct a new SimpleStringRebuilder that is a substring of another string rebuilder.
        /// </summary>
        private static SimpleStringRebuilder CreateSubstring(Span span, SimpleStringRebuilder simpleSource)
        {
            int firstLineNumber = simpleSource.GetLineNumberFromPosition(span.Start) + simpleSource._lineBreakSpanStart;
            int lastLineNumber = simpleSource.GetLineNumberFromPosition(span.End) + simpleSource._lineBreakSpanStart;

            //Handle the special case where the end position falls in the middle of a linebreak.
            if ((lastLineNumber < simpleSource.LineBreakSpanEnd) &&
                (span.End > simpleSource._storage.LineBreaks.StartOfLineBreak(lastLineNumber) - simpleSource._textSpanStart))
            {
                ++lastLineNumber;
            }

            return new SimpleStringRebuilder(simpleSource._storage, span.Start + simpleSource._textSpanStart, span.Length, firstLineNumber, lastLineNumber - firstLineNumber);
        }

        private SimpleStringRebuilder(ITextStorage storage, int textSpanStart, int length, int lineBreakSpanStart, int lineBreakCount)
            : base(length, lineBreakCount, 0)
        {
            _storage = storage;
            _textSpanStart = textSpanStart;
            _lineBreakSpanStart = lineBreakSpanStart;

            if (this.Length > 0)
            {
                _startsWithNewLine = _storage.IsNewLine(_textSpanStart);
                _endsWithReturn = _storage.IsReturn(this.TextSpanEnd - 1);
            }
        }
        #endregion

        public static StringRebuilder Create(ITextStorageLoader loader)
        {
            if (loader == null)
                throw new ArgumentNullException("loader");

            StringRebuilder content = _empty;
            foreach (ITextStorage storage in loader.Load())
            {
                content = content.Insert(content.Length, storage);
            }
            return content;
        }

        public static StringRebuilder Create(ITextStorage storage)
        {
            if (storage == null)
                throw new ArgumentNullException("storage");

            return (storage.Length == 0)
                ? _empty
                : new SimpleStringRebuilder(storage);
        }

        public static StringRebuilder Create(string text)
        {
            // called when performing simple text insertion or deletion.
            if (text == null)
                throw new ArgumentNullException("text");

            return (text.Length == 0)
                   ? _empty
                   : new SimpleStringRebuilder(text);
        }

        /// <summary>
        /// Consolidate two string rebuilders, taking advantage of the fact that they have already extracted the line breaks.
        /// </summary>
        public static SimpleStringRebuilder Create(StringRebuilder left, StringRebuilder right)
        {
            Debug.Assert(left.Length > 0);
            Debug.Assert(right.Length > 0);

            int length = left.Length + right.Length;
            char[] result = new char[length];

            left.CopyTo(0, result, 0, left.Length);
            right.CopyTo(0, result, left.Length, right.Length);
            string text = new string(result);

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
            }

            return new SimpleStringRebuilder(SimpleTextStorage.Create(text, lineBreaks));
        }

        public override string ToString()
        {
            return _storage.GetText(_textSpanStart, this.Length);
        }

        #region StringRebuilder Members
        public override int GetLineNumberFromPosition(int position)
        {
            if ((position < 0) || (position > this.Length))
                throw new ArgumentOutOfRangeException("position");

            //Convert position to a position relative to the start of _text.
            if (position == this.Length)
            {
                //Handle positions at the end of the span as a special case since otherwise we
                //return the incorrect value if the last line break extends past the end of _textSpan.
                return this.LineBreakCount;
            }

            position += _textSpanStart;

            int start = _lineBreakSpanStart;
            int end = this.LineBreakSpanEnd;

            while (start < end)
            {
                int middle = (start + end) / 2;
                if (position < _storage.LineBreaks.EndOfLineBreak(middle))
                    end = middle;
                else
                    start = middle + 1;
            }

            return start - _lineBreakSpanStart;
        }

        public override LineSpan GetLineFromLineNumber(int lineNumber)
        {
            if ((lineNumber < 0) || (lineNumber > this.LineBreakCount))
                throw new ArgumentOutOfRangeException("lineNumber");

            ILineBreaks lineBreaks = _storage.LineBreaks;

            int absoluteLineNumber = _lineBreakSpanStart + lineNumber;

            int start = (lineNumber == 0)
                        ? 0
                        : (Math.Min(this.TextSpanEnd, lineBreaks.EndOfLineBreak(absoluteLineNumber - 1)) - _textSpanStart);

            int end;
            int breakLength;
            if (lineNumber < this.LineBreakCount)
            {
                end = Math.Max(_textSpanStart, lineBreaks.StartOfLineBreak(absoluteLineNumber));
                breakLength = Math.Min(this.TextSpanEnd, lineBreaks.EndOfLineBreak(absoluteLineNumber)) - end;

                end -= _textSpanStart;
            }
            else
            {
                end = this.Length;
                breakLength = 0;
            }

            return new LineSpan(lineNumber, Span.FromBounds(start, end), breakLength);
        }

        public override StringRebuilder GetLeaf(int position, out int offset)
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

                return _storage[index + _textSpanStart];
            }
        }

        public override string GetText(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");

            return _storage.GetText(span.Start + _textSpanStart, span.Length);
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

            _storage.CopyTo(sourceIndex + _textSpanStart, destination, destinationIndex, count);
        }

        public override void Write(TextWriter writer, Span span)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");

            _storage.Write(writer, span.Start + _textSpanStart, span.Length);
        }

        public override StringRebuilder Substring(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");

            if (span.Length == this.Length)
                return this;
            else if (span.Length == 0)
                return _empty;
            else
                return SimpleStringRebuilder.CreateSubstring(span, this);
        }

        public override StringRebuilder Child(bool rightSide)
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
