//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    internal sealed class DefaultBufferPrimitive : TextBuffer
    {
        #region Private members
        private ITextBuffer _textBuffer;
        private IBufferPrimitivesFactoryService _bufferPrimitivesFactory;
        #endregion

        public DefaultBufferPrimitive(ITextBuffer textBuffer, IBufferPrimitivesFactoryService bufferPrimitivesFactory)
        {
            _textBuffer = textBuffer;
            _bufferPrimitivesFactory = bufferPrimitivesFactory;
        }

        public override TextPoint GetTextPoint(int position)
        {
            if ((position < 0) || (position > _textBuffer.CurrentSnapshot.Length))
            {
                throw new ArgumentOutOfRangeException("position");
            }
            return _bufferPrimitivesFactory.CreateTextPoint(this, position);
        }

        public override TextPoint GetTextPoint(int line, int column)
        {
            if ((line < 0) || (line > _textBuffer.CurrentSnapshot.LineCount))
            {
                throw new ArgumentOutOfRangeException("line");
            }

            ITextSnapshotLine snapshotLine = _textBuffer.CurrentSnapshot.GetLineFromLineNumber(line);

            if ((column < 0) || (column > snapshotLine.Length))
            {
                throw new ArgumentOutOfRangeException("column");
            }
            return _bufferPrimitivesFactory.CreateTextPoint(this, snapshotLine.Start + column);
        }

        public override TextRange GetLine(int line)
        {
            if ((line < 0) || (line > _textBuffer.CurrentSnapshot.LineCount))
            {
                throw new ArgumentOutOfRangeException("line");
            }

            ITextSnapshotLine snapshotLine = _textBuffer.CurrentSnapshot.GetLineFromLineNumber(line);

            return GetTextRange(snapshotLine.Extent.Start, snapshotLine.Extent.End);            
        }

        public override TextRange GetTextRange(TextPoint startPoint, TextPoint endPoint)
        {
            if (startPoint == null)
            {
                throw new ArgumentNullException("startPoint");
            }
            if (endPoint == null)
            {
                throw new ArgumentNullException("endPoint");
            }

            if (!object.ReferenceEquals(startPoint.TextBuffer, this))
            {
                throw new ArgumentException(Strings.TextPointFromWrongBuffer);
            }

            if (!object.ReferenceEquals(endPoint.TextBuffer, this))
            {
                throw new ArgumentException(Strings.TextPointFromWrongBuffer);
            }

            return _bufferPrimitivesFactory.CreateTextRange(this, startPoint, endPoint);
        }

        public override TextRange GetTextRange(int startPosition, int endPosition)
        {
            if ((startPosition < 0) || (startPosition > _textBuffer.CurrentSnapshot.Length))
            {
                throw new ArgumentOutOfRangeException("startPosition");
            }

            if ((endPosition < 0) || (endPosition > _textBuffer.CurrentSnapshot.Length))
            {
                throw new ArgumentOutOfRangeException("endPosition");
            }
            
            TextPoint startPoint = GetTextPoint(startPosition);
            TextPoint endPoint = GetTextPoint(endPosition);

            return _bufferPrimitivesFactory.CreateTextRange(this, startPoint, endPoint);
        }

        public override ITextBuffer AdvancedTextBuffer
        {
            get { return _textBuffer; }
        }

        public override TextPoint GetStartPoint()
        {
            return GetTextPoint(0);
        }

        public override TextPoint GetEndPoint()
        {
            return GetTextPoint(_textBuffer.CurrentSnapshot.Length);
        }

        public override IEnumerable<TextRange> Lines
        {
            get 
            {
                foreach (ITextSnapshotLine line in _textBuffer.CurrentSnapshot.Lines)
                {
                    yield return GetTextRange(line.Start, line.End);
                }
            }
        }
    }
}
