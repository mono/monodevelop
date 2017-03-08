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
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Operations;

    using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

    internal sealed class DefaultCaretPrimitive : Caret
    {
        #region Private members
        private TextView _textView;
        private IEditorOptions _editorOptions;
        #endregion

        internal DefaultCaretPrimitive(TextView textView, IEditorOptions editorOptions)
        {
            _textView = textView;
            _editorOptions = editorOptions;
        }

        public override void MoveToNextCharacter(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            TextPoint endPoint = TextView.Selection.GetEndPoint();
            bool selectionIsEmpty = TextView.Selection.IsEmpty;

            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            // If the selection is not empty, VS clears the selection and sets
            // the caret at the end of the selection (regardless of it were reversed or not)
            if (!extendSelection && !selectionIsEmpty)
            {
                MoveTo(endPoint.CurrentPosition);
            }
            else
            {
                AdvancedCaret.MoveToNextCaretPosition();
            }

            AdvancedCaret.EnsureVisible();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToPreviousCharacter(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            TextPoint startPoint = TextView.Selection.GetStartPoint();
            bool selectionIsEmpty = TextView.Selection.IsEmpty;

            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            if (!extendSelection && !selectionIsEmpty)
            {
                MoveTo(startPoint.CurrentPosition);
                
            }
            else
            {
                AdvancedCaret.MoveToPreviousCaretPosition();
            }

            AdvancedCaret.EnsureVisible();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToBeginningOfPreviousLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            this.MoveToBeginningOfPreviousLine();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToBeginningOfNextLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            this.MoveToBeginningOfNextLine();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToBeginningOfPreviousViewLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            this.MoveToBeginningOfPreviousViewLine();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToBeginningOfNextViewLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            this.MoveToBeginningOfNextViewLine();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToPreviousLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;

            ITextViewLine caretLine;
            if (this.TextView.Selection.IsEmpty || extendSelection)
            {
                caretLine = this.TextView.AdvancedTextView.Caret.ContainingTextViewLine;
            }
            else
            {
                caretLine = this.TextView.AdvancedTextView.GetTextViewLineContainingBufferPosition(this.TextView.AdvancedTextView.Selection.Start.Position);
            }

            if (caretLine.Start != 0)
            {
                caretLine = this.TextView.AdvancedTextView.GetTextViewLineContainingBufferPosition(caretLine.Start - 1);
            }

            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            this.AdvancedCaret.MoveTo(caretLine);
            this.AdvancedCaret.EnsureVisible();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToNextLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;

            ITextViewLine caretLine;
            if (this.TextView.Selection.IsEmpty || extendSelection)
            {
                caretLine = this.TextView.AdvancedTextView.Caret.ContainingTextViewLine;
            }
            else
            {
                SnapshotPoint end = this.TextView.AdvancedTextView.Selection.End.Position;
                caretLine = this.TextView.AdvancedTextView.GetTextViewLineContainingBufferPosition(end);

                if ((!caretLine.IsFirstTextViewLineForSnapshotLine) && (end.Position == caretLine.Start.Position))
                {
                    //The end of the selection is at the seam between two word-wrapped lines. In this case, we want
                    //the line before (since the selection is drawn to the end of that line rather than to the beginning
                    //of the other).
                    caretLine = this.TextView.AdvancedTextView.GetTextViewLineContainingBufferPosition(end - 1);
                }
            }

            if ((!caretLine.IsLastTextViewLineForSnapshotLine) || (caretLine.LineBreakLength != 0)) //If we are not on the last line of the file
            {
                caretLine = this.TextView.AdvancedTextView.GetTextViewLineContainingBufferPosition(caretLine.EndIncludingLineBreak);
            }

            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            this.AdvancedCaret.MoveTo(caretLine);
            this.AdvancedCaret.EnsureVisible();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveTo(int position, bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            SnapshotPoint bufferPosition = new SnapshotPoint(TextView.AdvancedTextView.TextSnapshot, position);
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            AdvancedCaret.MoveTo(bufferPosition);
            AdvancedCaret.EnsureVisible();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MovePageUp()
        {
            PageUpDown(ScrollDirection.Up);
        }

        public override void MovePageDown()
        {
            PageUpDown(ScrollDirection.Down);
        }

        public override void MovePageUp(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            MovePageUp();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MovePageDown(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            MovePageDown();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToEndOfLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;

            DisplayTextPoint caret = this.CaretPoint;
            if ((!this.TextView.Selection.IsEmpty) && !extendSelection)
            {
                caret = this.TextView.GetTextPoint(this.TextView.Selection.AdvancedSelection.End.Position);
            }
            caret.MoveToEndOfLine();

            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            this.AdvancedCaret.MoveTo(caret.AdvancedTextPoint, PositionAffinity.Successor);

            this.AdvancedCaret.EnsureVisible();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToStartOfLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;

            DisplayTextPoint caret = this.CaretPoint;
            if ((!this.TextView.Selection.IsEmpty) && !extendSelection)
            {
                caret = this.TextView.GetTextPoint(this.TextView.Selection.AdvancedSelection.Start.Position);
            }
            caret.MoveToStartOfLine();

            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            this.AdvancedCaret.MoveTo(caret.AdvancedTextPoint, PositionAffinity.Successor);

            this.AdvancedCaret.EnsureVisible();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToEndOfViewLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            SnapshotPoint selectionEnd = this.TextView.Selection.AdvancedSelection.End.Position;
            bool selectionIsEmpty = TextView.Selection.IsEmpty;

            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            if (!selectionIsEmpty && !extendSelection)
            {
                var selectionEndLine = this.TextView.AdvancedTextView.GetTextViewLineContainingBufferPosition(selectionEnd);
                AdvancedCaret.MoveTo(selectionEndLine.End, PositionAffinity.Predecessor);
            }
            else
            {
                ITextViewLine line = this.AdvancedCaret.ContainingTextViewLine;
                AdvancedCaret.MoveTo(line.End, line.IsLastTextViewLineForSnapshotLine ? PositionAffinity.Successor : PositionAffinity.Predecessor);
            }

            AdvancedCaret.EnsureVisible();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToStartOfViewLine(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            SnapshotPoint selectionStart = this.TextView.Selection.AdvancedSelection.Start.Position;
            bool selectionIsEmpty = TextView.Selection.IsEmpty;
            bool selectionIsReversed = TextView.Selection.AdvancedSelection.IsReversed;

            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            //We can't use the default behavior of the DisplayTextPoint.MoveToStartOfViewLine since the ITextViewLine
            //the caret is on may not be the same as the ITextViewLine the DisplayTextPoint created from the caret thinks
            //it is on (c.f. word wrap & affinity). Simulate the behavior of the DisplayTextPoint, adjusting for the
            //ITextViewLine and selection.
            ITextViewLine line;
            if (!selectionIsEmpty && !selectionIsReversed && !extendSelection)
            {
                line = this.TextView.AdvancedTextView.GetTextViewLineContainingBufferPosition(selectionStart);
            }
            else
            {
                line = this.AdvancedCaret.ContainingTextViewLine;
            }

            SnapshotPoint startOfLine = line.Start;

            this.AdvancedCaret.MoveTo(startOfLine, PositionAffinity.Successor);
            this.AdvancedCaret.EnsureVisible();
            this.UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToLine(int lineNumber, bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            MoveToLine(lineNumber);
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToLine(int lineNumber, int offset, bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            ITextSnapshotLine line = TextView.AdvancedTextView.TextSnapshot.GetLineFromLineNumber(lineNumber);
            AdvancedCaret.MoveTo(new VirtualSnapshotPoint(line, offset));

            AdvancedCaret.EnsureVisible();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToStartOfDocument(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            AdvancedCaret.MoveTo(new SnapshotPoint(TextView.AdvancedTextView.TextSnapshot, 0));
            AdvancedCaret.EnsureVisible();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToEndOfDocument(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            AdvancedCaret.MoveTo(new SnapshotPoint(TextView.AdvancedTextView.TextSnapshot,
                                                   TextView.AdvancedTextView.TextSnapshot.Length));
            AdvancedCaret.EnsureVisible();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToNextWord(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            DisplayTextPoint caretLocation = CaretPoint;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            caretLocation.MoveToNextWord();
            AdvancedCaret.MoveTo(caretLocation.AdvancedTextPoint);
            AdvancedCaret.EnsureVisible();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void MoveToPreviousWord(bool extendSelection)
        {
            VirtualSnapshotPoint oldCaretPoint = this.AdvancedCaret.Position.VirtualBufferPosition;
            DisplayTextPoint caretLocation = CaretPoint;
            if (!extendSelection)
            {
                TextView.AdvancedTextView.Selection.Clear();
            }

            caretLocation.MoveToPreviousWord();
            AdvancedCaret.MoveTo(caretLocation.AdvancedTextPoint);
            AdvancedCaret.EnsureVisible();
            UpdateSelection(extendSelection, oldCaretPoint);
        }

        public override void EnsureVisible()
        {
            AdvancedCaret.EnsureVisible();
        }

        public override ITextCaret AdvancedCaret
        {
            get { return _textView.AdvancedTextView.Caret; }
        }

        public override TextView TextView
        {
            get { return _textView; }
        }

        public override ITextViewLine AdvancedTextViewLine
        {
            get
            {
                return this.AdvancedCaret.ContainingTextViewLine;
            }
        }

        public override int DisplayColumn
        {
            get
            {
                ITextViewLine textViewLine = AdvancedTextViewLine;

                if ((textViewLine != null) && (textViewLine.IsValid))
                {
                    return PrimitivesUtilities.GetColumnOfPoint(
                        TextView.AdvancedTextView.TextSnapshot,
                        AdvancedTextPoint,
                        textViewLine.Start,
                        _editorOptions.GetTabSize(),
                        (p) => (textViewLine.GetTextElementSpan(p).End));
                }
                else
                {
                    return CaretPoint.DisplayColumn;
                }
            }
        }

        public override bool IsVisible
        {
            get { return CaretPoint.IsVisible; }
        }

        public override DisplayTextRange GetDisplayTextRange(DisplayTextPoint otherPoint)
        {
            return CaretPoint.GetDisplayTextRange(otherPoint);
        }

        public override DisplayTextRange GetDisplayTextRange(int otherPosition)
        {
            return CaretPoint.GetDisplayTextRange(otherPosition);
        }

        protected override DisplayTextPoint CloneDisplayTextPointInternal()
        {
            return CaretPoint.Clone();
        }

        public override TextBuffer TextBuffer
        {
            get { return _textView.TextBuffer; }
        }

        public override int CurrentPosition
        {
            get { return CaretPoint.CurrentPosition; }
        }

        public override int Column
        {
            get { return CaretPoint.Column; }
        }

        public override bool DeleteNext()
        {
            if (TextView.Selection.IsEmpty)
            {
                if (!CaretPoint.DeleteNext())
                    return false;
            }
            else
            {
                if (!TextView.Selection.Delete())
                    return false;
            }

            AdvancedCaret.EnsureVisible();

            return true;
        }

        public override bool DeletePrevious()
        {
            if (TextView.Selection.IsEmpty)
            {
                if (AdvancedCaret.InVirtualSpace)
                {
                    AdvancedCaret.MoveToPreviousCaretPosition();
                }
                else
                {
                    if (!CaretPoint.DeletePrevious())
                        return false;
                    if (AdvancedCaret.InVirtualSpace)
                        AdvancedCaret.MoveTo(AdvancedCaret.Position.BufferPosition);
                }
            }
            else
            {
                if (!TextView.Selection.Delete())
                    return false;
            }

            AdvancedCaret.EnsureVisible();

            return true;
        }

        public override TextRange GetCurrentWord()
        {
            return CaretPoint.GetCurrentWord();
        }

        public override TextRange GetNextWord()
        {
            return CaretPoint.GetNextWord();
        }

        public override TextRange GetPreviousWord()
        {
            return CaretPoint.GetPreviousWord();
        }

        public override TextRange GetTextRange(TextPoint otherPoint)
        {
            return CaretPoint.GetTextRange(otherPoint);
        }

        public override TextRange GetTextRange(int otherPosition)
        {
            return CaretPoint.GetTextRange(otherPosition);
        }

        public override bool InsertNewLine()
        {
            if (!TextView.Selection.IsEmpty)
            {
                if (!TextView.Selection.Delete())
                    return false;
            }

            if (!CaretPoint.InsertNewLine())
                return false;

            AdvancedCaret.EnsureVisible();

            return true;
        }

        public override bool InsertIndent()
        {
            if (!CaretPoint.InsertIndent())
                return false;

            AdvancedCaret.EnsureVisible();

            return true;
        }

        public override bool InsertText(string text)
        {
            if (!TextView.Selection.IsEmpty)
            {
                if (!TextView.Selection.Delete())
                    return false;
            }

            if (!CaretPoint.InsertText(text))
                return false;

            AdvancedCaret.EnsureVisible();

            return true;
        }

        public override int LineNumber
        {
            get { return CaretPoint.LineNumber; }
        }

        public override int StartOfLine
        {
            get { return CaretPoint.StartOfLine; }
        }

        public override int EndOfLine
        {
            get { return CaretPoint.EndOfLine; }
        }

        public override int StartOfViewLine
        {
            get { return this.AdvancedTextViewLine.Start; }
        }

        public override int EndOfViewLine
        {
            get { return this.AdvancedTextViewLine.End; }
        }

        public override bool RemovePreviousIndent()
        {
            if (!CaretPoint.RemovePreviousIndent())
                return false;

            AdvancedCaret.EnsureVisible();

            return true;
        }

        public override bool TransposeCharacter()
        {
            if (!CaretPoint.TransposeCharacter())
                return false;

            TextView.Selection.Clear();
            AdvancedCaret.EnsureVisible();

            return true;
        }

        public override bool TransposeLine()
        {
            if (TextView.Selection.IsEmpty)
            {
                TextPoint caretPoint = CaretPoint;
                double oldLeft = AdvancedCaret.Left;

                if (!caretPoint.TransposeLine())
                    return false;

                AdvancedCaret.MoveTo(caretPoint.AdvancedTextPoint, PositionAffinity.Successor, false);
                AdvancedCaret.EnsureVisible();

                ITextViewLine newLine = AdvancedTextViewLine;
                AdvancedCaret.MoveTo(newLine, oldLeft);
                AdvancedCaret.EnsureVisible();
            }
            return true;
        }

        public override bool TransposeLine(int lineNumber)
        {
            if (!TextView.Selection.IsEmpty)
                return true;

            if (!CaretPoint.TransposeLine(lineNumber))
                return false;

            AdvancedCaret.EnsureVisible();

            return true;
        }

        public override SnapshotPoint AdvancedTextPoint
        {
            get { return CaretPoint.AdvancedTextPoint; }
        }

        public override string GetNextCharacter()
        {
            return CaretPoint.GetNextCharacter();
        }

        public override string GetPreviousCharacter()
        {
            return CaretPoint.GetPreviousCharacter();
        }

        public override TextRange Find(string pattern, FindOptions findOptions, TextPoint endPoint)
        {
            return CaretPoint.Find(pattern, findOptions, endPoint);
        }

        public override TextRange Find(string pattern, TextPoint endPoint)
        {
            return CaretPoint.Find(pattern, endPoint);
        }

        public override TextRange Find(string pattern, FindOptions findOptions)
        {
            return CaretPoint.Find(pattern, findOptions);
        }

        public override TextRange Find(string pattern)
        {
            return CaretPoint.Find(pattern);
        }

        public override Collection<TextRange> FindAll(string pattern, TextPoint endPoint)
        {
            return CaretPoint.FindAll(pattern, endPoint);
        }

        public override Collection<TextRange> FindAll(string pattern, FindOptions findOptions, TextPoint endPoint)
        {
            return CaretPoint.FindAll(pattern, findOptions, endPoint);
        }

        public override Collection<TextRange> FindAll(string pattern)
        {
            return CaretPoint.FindAll(pattern);
        }

        public override Collection<TextRange> FindAll(string pattern, FindOptions findOptions)
        {
            return CaretPoint.FindAll(pattern, findOptions);
        }

        public override void MoveTo(int position)
        {
            MoveTo(position, false);
        }

        public override void MoveToNextCharacter()
        {
            MoveToNextCharacter(false);
        }

        public override void MoveToPreviousCharacter()
        {
            MoveToPreviousCharacter(false);
        }

        public override void MoveToLine(int lineNumber)
        {
            DisplayTextPoint caretLocation = CaretPoint;
            TextView.Selection.Clear();
            caretLocation.MoveToLine(lineNumber);
            AdvancedCaret.MoveTo(caretLocation.AdvancedTextPoint);
            AdvancedCaret.EnsureVisible();
        }

        public override void MoveToEndOfLine()
        {
            MoveToEndOfLine(false);
        }

        public override void MoveToStartOfLine()
        {
            MoveToStartOfLine(false);
        }

        public override void MoveToEndOfViewLine()
        {
            MoveToEndOfViewLine(false);
        }

        public override void MoveToStartOfViewLine()
        {
            MoveToStartOfViewLine(false);
        }

        public override void MoveToEndOfDocument()
        {
            MoveToEndOfDocument(false);
        }

        public override void MoveToStartOfDocument()
        {
            MoveToStartOfDocument(false);
        }

        public override void MoveToBeginningOfNextLine()
        {
            DisplayTextPoint caret = this.CaretPoint;
            caret.MoveToBeginningOfNextLine();
            this.AdvancedCaret.MoveTo(caret.AdvancedTextPoint);
            this.AdvancedCaret.EnsureVisible();
        }

        public override void MoveToBeginningOfPreviousLine()
        {
            DisplayTextPoint caret = this.CaretPoint;
            caret.MoveToBeginningOfPreviousLine();
            this.AdvancedCaret.MoveTo(caret.AdvancedTextPoint);
            this.AdvancedCaret.EnsureVisible();
        }

        public override void MoveToBeginningOfNextViewLine()
        {
            this.AdvancedCaret.MoveTo(this.AdvancedTextViewLine.EndIncludingLineBreak);
            this.AdvancedCaret.EnsureVisible();
        }

        public override void MoveToBeginningOfPreviousViewLine()
        {
            ITextViewLine line = this.AdvancedTextViewLine;
            if (line.Start > 0)
                line = _textView.AdvancedTextView.GetTextViewLineContainingBufferPosition(line.Start - 1);

            this.AdvancedCaret.MoveTo(line.Start);
            this.AdvancedCaret.EnsureVisible();
        }

        public override void MoveToNextWord()
        {
            MoveToNextWord(false);
        }

        public override void MoveToPreviousWord()
        {
            MoveToPreviousWord(false);
        }

        // Note: This overrides the equivalent DisplayTextPoint method in order to use the caret's
        // ContainingTextViewLine, which can differ at the wrap points of word-wrapped lines.
        public override DisplayTextPoint GetFirstNonWhiteSpaceCharacterOnViewLine()
        {
            ITextViewLine viewLine = this.AdvancedTextViewLine;
            ITextSnapshot snapshot = viewLine.Extent.Snapshot;

            int firstNonWhitespaceCharacter = viewLine.Start;
            while ((firstNonWhitespaceCharacter < viewLine.End) &&
                    char.IsWhiteSpace(snapshot[firstNonWhitespaceCharacter]))
            {
                firstNonWhitespaceCharacter++;
            }

            return TextView.GetTextPoint(firstNonWhitespaceCharacter);
        }

        public override TextPoint GetFirstNonWhiteSpaceCharacterOnLine()
        {
            return CaretPoint.GetFirstNonWhiteSpaceCharacterOnLine();
        }

        #region Private methods
        private DisplayTextPoint CaretPoint
        {
            get
            {
                return TextView.GetTextPoint(_textView.AdvancedTextView.Caret.Position.BufferPosition);
            }
        }

        /// <summary>
        /// Update the selection based on the movement of the caret.
        /// </summary>
        private void UpdateSelection(bool extendSelection, VirtualSnapshotPoint oldPosition)
        {
            if (extendSelection)
            {
                if (TextView.Selection.IsEmpty)
                {
                    //The provided old position might be stale
                    this.TextView.AdvancedTextView.Selection.Select(oldPosition.TranslateTo(this.TextView.AdvancedTextView.TextSnapshot),
                                                                    this.AdvancedCaret.Position.VirtualBufferPosition);
                }
                else
                {
                    this.TextView.AdvancedTextView.Selection.Select(this.TextView.AdvancedTextView.Selection.AnchorPoint,
                                                                    this.AdvancedCaret.Position.VirtualBufferPosition);
                }
            }
            else
            {
                TextView.AdvancedTextView.Selection.Clear();
            }
        }

        private void PageUpDown(ScrollDirection direction)
        {
            if (direction == ScrollDirection.Up)
            {
                //If we scrolled a full page, then the bottom of the first fully visible line will be below
                //the bottom of the view.
                ITextViewLine firstVisibleLine = TextView.AdvancedTextView.TextViewLines.FirstVisibleLine;
                SnapshotPoint oldFullyVisibleStart = (firstVisibleLine.VisibilityState == VisibilityState.FullyVisible)
                                           ? firstVisibleLine.Start
                                           : firstVisibleLine.EndIncludingLineBreak; //Start of next line.

                if (TextView.AdvancedTextView.ViewScroller.ScrollViewportVerticallyByPage(direction))
                {
                    ITextViewLine newFirstLine = TextView.AdvancedTextView.TextViewLines.GetTextViewLineContainingBufferPosition(oldFullyVisibleStart);

                    //The old fully visible line should -- if we scrolled as much as we could -- be partially
                    //obscured. The shortfall between a full page and what we actually scrolled is the distance
                    //between the bottom of that line and the bottom of the screen.
                    if (TextView.AdvancedTextView.ViewportBottom > newFirstLine.Bottom)
                    {
                        AdvancedCaret.MoveTo(TextView.AdvancedTextView.TextViewLines.FirstVisibleLine);
                    }
                    else
                    {
                        AdvancedCaret.MoveToPreferredCoordinates();
                    }
                }
            }
            else
            {
                //If we scroll a full page, the bottom of the last fully visible line will be
                //positioned at 0.0.
                ITextViewLine lastVisibleLine = TextView.AdvancedTextView.TextViewLines.LastVisibleLine;

                // If the last line in the buffer is fully visible , then just move the caret to
                // the last visible line
                if ((lastVisibleLine.VisibilityState == VisibilityState.FullyVisible) &&
                    (lastVisibleLine.End == lastVisibleLine.Snapshot.Length))
                {
                    AdvancedCaret.MoveTo(lastVisibleLine);

                    // No need to ensure the caret is visible since the line it just moved to is fully visible.
                    return;
                }

                SnapshotPoint oldFullyVisibleStart = ((lastVisibleLine.VisibilityState == VisibilityState.FullyVisible) || (lastVisibleLine.Start == 0))
                                           ? lastVisibleLine.Start
                                           : (lastVisibleLine.Start - 1); //Actually just a point on the previous line.

                if (TextView.AdvancedTextView.ViewScroller.ScrollViewportVerticallyByPage(direction))
                {
                    ITextViewLine newLastLine = TextView.AdvancedTextView.TextViewLines.GetTextViewLineContainingBufferPosition(oldFullyVisibleStart);
                    if (newLastLine.Bottom > TextView.AdvancedTextView.ViewportTop)
                    {
                        AdvancedCaret.MoveTo(TextView.AdvancedTextView.TextViewLines.LastVisibleLine);
                    }
                    else
                    {
                        AdvancedCaret.MoveToPreferredCoordinates();
                    }
                }
            }

            EnsureVisible();
        }
        #endregion
    }
}
