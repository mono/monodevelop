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
    using System.Collections.ObjectModel;
    using System.Globalization;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Operations;

    using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
    using System.Diagnostics;

    internal sealed class DefaultDisplayTextPointPrimitive : DisplayTextPoint
    {
        private TextView _textView;
        private TextPoint _bufferPoint;
        private IEditorOptions _editorOptions;

        internal DefaultDisplayTextPointPrimitive(TextView textView, int position, IEditorOptions editorOptions)
        {
            _textView = textView;
            _editorOptions = editorOptions;

            _bufferPoint = _textView.TextBuffer.GetStartPoint();

            // The position coming in will be from the view's snapshot which may not be the same snapshot as the buffer.
            // Force the position to track to the buffer's snapshot to prevent the position from being valid in the view's
            // snapshot but invalid in the buffers
            ITrackingPoint trackingPoint = _textView.AdvancedTextView.TextSnapshot.CreateTrackingPoint(position, PointTrackingMode.Positive);

            MoveTo(trackingPoint.GetPosition(_textView.TextBuffer.AdvancedTextBuffer.CurrentSnapshot));
        }

        public override TextView TextView
        {
            get { return _textView; }
        }

        public override ITextViewLine AdvancedTextViewLine
        {
            get 
            {
                return _textView.AdvancedTextView.GetTextViewLineContainingBufferPosition(this.AdvancedTextPoint);
            }
        }

        public override int DisplayColumn
        {
            get
            {
                ITextView textView = _textView.AdvancedTextView;
                SnapshotPoint position = this.AdvancedTextViewLine.Start;

                return PrimitivesUtilities.GetColumnOfPoint(
                    textView.TextSnapshot, 
                    AdvancedTextPoint, 
                    position, 
                    _editorOptions.GetTabSize(), 
                    (p) => (textView.GetTextElementSpan(p).End));
            }
        }

        public override bool IsVisible
        {
            get
            {
                // if no lines are being drawn then this point can't be visible
                return (this.AdvancedTextViewLine.VisibilityState == VisibilityState.FullyVisible);
            }
        }

        public override DisplayTextRange GetDisplayTextRange(DisplayTextPoint otherPoint)
        {
            if (!object.ReferenceEquals(this.TextBuffer, otherPoint.TextBuffer))
            {
                throw new ArgumentException("The other point must have the same TextBuffer as this one", "otherPoint");
            }
            return TextView.GetTextRange(this, otherPoint);
        }

        public override DisplayTextRange GetDisplayTextRange(int otherPosition)
        {
            return TextView.GetTextRange(CurrentPosition, otherPosition);
        }

        protected override DisplayTextPoint CloneDisplayTextPointInternal()
        {
            return new DefaultDisplayTextPointPrimitive(_textView, CurrentPosition, _editorOptions);
        }

        public override TextBuffer TextBuffer
        {
            get { return _textView.TextBuffer; }
        }

        public override int CurrentPosition
        {
            get { return _bufferPoint.CurrentPosition; }
        }

        public override int Column
        {
            get { return _bufferPoint.Column; }
        }

        public override bool DeleteNext()
        {
            if (_textView.AdvancedTextView.TextViewModel.IsPointInVisualBuffer(AdvancedTextPoint, PositionAffinity.Successor))
            {
                return PrimitivesUtilities.Delete(GetNextTextElementSpan());
            }
            else
            {
                return _bufferPoint.DeleteNext();
            }
        }

        public override bool DeletePrevious()
        {
            SnapshotSpan previousElementSpan = GetPreviousTextElementSpan();

            if ((previousElementSpan.Length > 0) &&
                (_textView.AdvancedTextView.TextViewModel.IsPointInVisualBuffer(AdvancedTextPoint, PositionAffinity.Successor)) &&
                (!_textView.AdvancedTextView.TextViewModel.IsPointInVisualBuffer(previousElementSpan.End - 1, PositionAffinity.Successor)))
            {
                // Since the previous character is not visible but the current one is, delete 
                // the entire previous text element span.
                return PrimitivesUtilities.Delete(previousElementSpan);
            }
            else
            {
                // Delegate to the buffer point's DeletePrevious implementation to handle deleting single
                // characters.  A single character should be deleted if this point and the previous one
                // are both visible or both not visible.
                return _bufferPoint.DeletePrevious();
            }
        }

        public override TextRange GetCurrentWord()
        {
            return _bufferPoint.GetCurrentWord();
        }

        public override TextRange GetNextWord()
        {
            return _bufferPoint.GetNextWord();
        }

        public override TextRange GetPreviousWord()
        {
            return _bufferPoint.GetPreviousWord();
        }

        public override TextRange GetTextRange(TextPoint otherPoint)
        {
            return _bufferPoint.GetTextRange(otherPoint);
        }

        public override TextRange GetTextRange(int otherPosition)
        {
            return _bufferPoint.GetTextRange(otherPosition);
        }

        public override bool InsertNewLine()
        {
            return _bufferPoint.InsertNewLine();
        }

        public override bool InsertIndent()
        {
            return _bufferPoint.InsertIndent();
        }

        public override bool InsertText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            return _bufferPoint.InsertText(text);
        }

        public override int LineNumber
        {
            get { return _bufferPoint.LineNumber; }
        }

        public override int StartOfLine
        {
            get { return _bufferPoint.StartOfLine; }
        }

        public override int EndOfLine
        {
            get { return _bufferPoint.EndOfLine; }
        }

        public override int StartOfViewLine
        {
            get
            {
                return this.AdvancedTextViewLine.Start;
            }
        }

        public override int EndOfViewLine
        {
            get
            {
                return this.AdvancedTextViewLine.End; 
            }
        }

        public override bool RemovePreviousIndent()
        {
            return _bufferPoint.RemovePreviousIndent();
        }

        public override bool TransposeCharacter()
        {
            SnapshotPoint insertionPoint = AdvancedTextPoint;

            ITextSnapshotLine line = _textView.AdvancedTextView.TextSnapshot.GetLineFromPosition(insertionPoint);

            string lineText = line.GetText();

            if (StringInfo.ParseCombiningCharacters(lineText).Length < 2)
            {
                return true;
            }

            SnapshotSpan textElementLeftSpan, textElementRightSpan;

            // We're at the start of a line
            if (insertionPoint == line.Start)
            {
                textElementLeftSpan = TextView.AdvancedTextView.GetTextElementSpan(insertionPoint);
                textElementRightSpan = TextView.AdvancedTextView.GetTextElementSpan(textElementLeftSpan.End);
            }
            // We're at the end of a line
            else if (insertionPoint == line.End)
            {
                textElementRightSpan = TextView.AdvancedTextView.GetTextElementSpan(insertionPoint - 1);
                textElementLeftSpan = TextView.AdvancedTextView.GetTextElementSpan(textElementRightSpan.Start - 1);
            }
            // We're at the middle of a line
            else
            {
                textElementRightSpan = TextView.AdvancedTextView.GetTextElementSpan(insertionPoint);
                textElementLeftSpan = TextView.AdvancedTextView.GetTextElementSpan(textElementRightSpan.Start - 1);
            }

            string transposedText = _textView.AdvancedTextView.TextSnapshot.GetText(textElementRightSpan)
                + _textView.AdvancedTextView.TextSnapshot.GetText(textElementLeftSpan);

            return PrimitivesUtilities.Replace(TextBuffer.AdvancedTextBuffer, new Span(textElementLeftSpan.Start, transposedText.Length), transposedText);
        }

        public override bool TransposeLine()
        {
            return _bufferPoint.TransposeLine();
        }

        public override bool TransposeLine(int lineNumber)
        {
            return _bufferPoint.TransposeLine(lineNumber);
        }

        public override SnapshotPoint AdvancedTextPoint
        {
            // TODO!: Should this not translate (and throw instead)?  Should it be positive or negative?
            // Use positive tracking to behave like the caret would.
            get 
            {
                Debug.Assert(_bufferPoint.AdvancedTextPoint.Snapshot == TextView.AdvancedTextView.TextSnapshot,
                             "WARNING: We are tracking a SnapshotPoint in a display primitive, which could have bad consequences.");
                             
                return _bufferPoint.AdvancedTextPoint.TranslateTo(TextView.AdvancedTextView.TextSnapshot,
                                                                  PointTrackingMode.Positive); 
            }
        }

        public override string GetNextCharacter()
        {
            if (CurrentPosition == _textView.AdvancedTextView.TextSnapshot.Length)
            {
                return string.Empty;
            }
            return _textView.AdvancedTextView.TextSnapshot.GetText(TextView.AdvancedTextView.GetTextElementSpan(AdvancedTextPoint));
        }

        public override string GetPreviousCharacter()
        {
            if (CurrentPosition == 0)
            {
                return string.Empty;
            }
            return _textView.AdvancedTextView.TextSnapshot.GetText(GetPreviousTextElementSpan());
        }

        public override TextRange Find(string pattern, FindOptions findOptions, TextPoint endPoint)
        {
            return _bufferPoint.Find(pattern, findOptions, endPoint);
        }

        public override TextRange Find(string pattern, TextPoint endPoint)
        {
            return _bufferPoint.Find(pattern, endPoint);
        }

        public override TextRange Find(string pattern, FindOptions findOptions)
        {
            return _bufferPoint.Find(pattern, findOptions);
        }

        public override TextRange Find(string pattern)
        {
            return _bufferPoint.Find(pattern);
        }

        public override Collection<TextRange> FindAll(string pattern, TextPoint endPoint)
        {
            return _bufferPoint.FindAll(pattern, endPoint);
        }

        public override Collection<TextRange> FindAll(string pattern, FindOptions findOptions, TextPoint endPoint)
        {
            return _bufferPoint.FindAll(pattern, findOptions, endPoint);
        }

        public override Collection<TextRange> FindAll(string pattern)
        {
            return _bufferPoint.FindAll(pattern);
        }

        public override Collection<TextRange> FindAll(string pattern, FindOptions findOptions)
        {
            return _bufferPoint.FindAll(pattern, findOptions);
        }

        public override void MoveTo(int position)
        {
            SnapshotPoint point = new SnapshotPoint(TextView.AdvancedTextView.TextSnapshot, position);

            _bufferPoint.MoveTo(TextView.AdvancedTextView.GetTextElementSpan(point).Start);
        }

        public override void MoveToNextCharacter()
        {
            int currentPosition = this.CurrentPosition;

            MoveTo(GetNextTextElementSpan().End);

            // It's possible that the above code didn't actually move this point.  Dev11 #109752 shows how
            // this can happen, which is that GetNextTextElementSpan() above (which calls into WPF) may say
            // that the current character doesn't combine with anything, but the _bufferPoint (which calls into
            // framework methods that follow the unicode standard) will say that the character does combine.  In that case
            // the GetNextTextElementSpan().End isn't far enough away for this point to escape the combining character
            // sequence, and the point is left where it is, at the start of a base character.
            if (currentPosition == this.CurrentPosition &&
                currentPosition != this.AdvancedTextPoint.Snapshot.Length)
            {
                _bufferPoint.MoveToNextCharacter();

                Debug.Assert(currentPosition != this.CurrentPosition,
                             "DisplayTextPoint.MoveToNextCharacter was unable to successfully move forward. This may cause unexpected behavior or hangs.");
            }
        }

        public override void MoveToPreviousCharacter()
        {
            MoveTo(GetPreviousTextElementSpan().Start);
        }

        public override void MoveToLine(int lineNumber)
        {
            _bufferPoint.MoveToLine(lineNumber);
        }

        public override void MoveToEndOfLine()
        {
            _bufferPoint.MoveToEndOfLine();
        }

        public override void MoveToStartOfLine()
        {
            _bufferPoint.MoveToStartOfLine();
        }

        public override void MoveToEndOfViewLine()
        {
            MoveTo(this.EndOfViewLine);
        }

        public override void MoveToStartOfViewLine()
        {
            MoveTo(this.StartOfViewLine);
        }

        public override void MoveToEndOfDocument()
        {
            _bufferPoint.MoveToEndOfDocument();
        }

        public override void MoveToStartOfDocument()
        {
            _bufferPoint.MoveToStartOfDocument();
        }

        public override void MoveToBeginningOfNextLine()
        {
            _bufferPoint.MoveToBeginningOfNextLine();
        }

        public override void MoveToBeginningOfPreviousLine()
        {
            _bufferPoint.MoveToBeginningOfPreviousLine();
        }

        public override void MoveToBeginningOfNextViewLine()
        {
            this.MoveTo(this.AdvancedTextViewLine.EndIncludingLineBreak);
        }

        public override void MoveToBeginningOfPreviousViewLine()
        {
            ITextViewLine line = this.AdvancedTextViewLine;
            if (line.Start > 0)
                line = _textView.AdvancedTextView.GetTextViewLineContainingBufferPosition(line.Start - 1);

            this.MoveTo(line.Start);
        }

        public override void MoveToNextWord()
        {
            _bufferPoint.MoveToNextWord();

            // make sure the word structure navigator didn't return a position in the middle of a view element
            SnapshotPoint bufferPoint = _bufferPoint.AdvancedTextPoint;
            SnapshotSpan textElementSpan = _textView.AdvancedTextView.GetTextElementSpan(bufferPoint);
            if (bufferPoint > textElementSpan.Start)
                this.MoveTo(textElementSpan.End);
        }

        public override void MoveToPreviousWord()
        {
            _bufferPoint.MoveToPreviousWord();

            // make sure the word structure navigator didn't return a position in the middle of a view element
            this.MoveTo(_bufferPoint.AdvancedTextPoint);
        }

        public override DisplayTextPoint GetFirstNonWhiteSpaceCharacterOnViewLine()
        {
            ITextViewLine viewLine = _textView.AdvancedTextView.GetTextViewLineContainingBufferPosition(AdvancedTextPoint);
            ITextSnapshot snapshot = viewLine.Extent.Snapshot;

            int firstNonWhitespaceCharacter = viewLine.Start;
            while ((firstNonWhitespaceCharacter < viewLine.End) &&
                    char.IsWhiteSpace(snapshot[firstNonWhitespaceCharacter]))
            {
                firstNonWhitespaceCharacter++;
            }

            return new DefaultDisplayTextPointPrimitive(_textView, firstNonWhitespaceCharacter, _editorOptions);
        }

        public override TextPoint GetFirstNonWhiteSpaceCharacterOnLine()
        {
            return _bufferPoint.GetFirstNonWhiteSpaceCharacterOnLine();
        }

        #region Private helpers
        private SnapshotSpan GetNextTextElementSpan()
        {
            return TextView.AdvancedTextView.GetTextElementSpan(AdvancedTextPoint);
        }

        private SnapshotSpan GetPreviousTextElementSpan()
        {
            if (CurrentPosition == 0)
            {
                return new SnapshotSpan(AdvancedTextPoint, 0);
            }
            return TextView.AdvancedTextView.GetTextElementSpan(AdvancedTextPoint - 1);
        }
        #endregion
    }
}
