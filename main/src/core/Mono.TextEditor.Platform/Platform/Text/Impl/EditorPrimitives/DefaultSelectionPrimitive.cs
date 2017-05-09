//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Operations;

    using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
    
    internal sealed class DefaultSelectionPrimitive : Selection
    {
        private TextView _textView;
        private IEditorOptions _editorOptions;

        public DefaultSelectionPrimitive(TextView textView, IEditorOptions editorOptions)
        {
            _textView = textView;
            _editorOptions = editorOptions;
        }

        private ITextSelection TextSelection 
        {
            get { return _textView.AdvancedTextView.Selection; } 
        }

        private ITextCaret Caret
        {
            get { return TextView.AdvancedTextView.Caret; }
        }

        private DisplayTextRange TextRange
        {
            get
            {
                // TODO: What do we do in box mode?
                return TextView.GetTextRange(TextSelection.Start.Position, TextSelection.End.Position);
            }
        }

        public override void SelectRange(TextRange textRange)
        {
            this.SelectRange(textRange.GetStartPoint().CurrentPosition, textRange.GetEndPoint().CurrentPosition);
        }

        public override void SelectRange(TextPoint selectionStart, TextPoint selectionEnd)
        {
            this.SelectRange(selectionStart.CurrentPosition, selectionEnd.CurrentPosition);
        }

        public override void SelectAll()
        {
            // For select all, the selection always goes back to stream mode.
            this.AdvancedSelection.Mode = TextSelectionMode.Stream;
            this.SelectRange(0, TextView.AdvancedTextView.TextSnapshot.Length);
        }

        public override void ExtendSelection(TextPoint newEnd)
        {
            int selectionStart, selectionEnd = newEnd.CurrentPosition;

            // Now, figure out where the selection actually started
            if (IsEmpty)
                selectionStart = TextSelection.Start.Position;
            else
            {
                if (IsReversed)
                {
                    selectionStart = GetEndPoint().CurrentPosition;
                }
                else
                {
                    selectionStart = GetStartPoint().CurrentPosition;
                }
            }

            this.SelectRange(selectionStart, selectionEnd);
        }

        private void SelectRange(int selectionStart, int selectionEnd)
        {
            SnapshotPoint startPoint = new SnapshotPoint(TextView.AdvancedTextView.TextSnapshot, selectionStart);
            SnapshotPoint endPoint = new SnapshotPoint(TextView.AdvancedTextView.TextSnapshot, selectionEnd);

            TextSelection.Select(new VirtualSnapshotPoint(startPoint), new VirtualSnapshotPoint(endPoint));

            ITextViewLine textViewLine = TextView.AdvancedTextView.GetTextViewLineContainingBufferPosition(endPoint);
            PositionAffinity affinity = (textViewLine.IsLastTextViewLineForSnapshotLine || (endPoint != textViewLine.End)) ? PositionAffinity.Successor : PositionAffinity.Predecessor;

            Caret.MoveTo(endPoint, affinity);
            TextView.AdvancedTextView.ViewScroller.EnsureSpanVisible(TextSelection.StreamSelectionSpan.SnapshotSpan,
                                                                     (selectionStart <= selectionEnd) 
                                                                     ? EnsureSpanVisibleOptions.MinimumScroll
                                                                     : (EnsureSpanVisibleOptions.MinimumScroll | EnsureSpanVisibleOptions.ShowStart));
        }

        public override void Clear()
        {
            TextSelection.Clear();
        }

        public override ITextSelection AdvancedSelection
        {
            get { return TextSelection; }
        }

        public override bool IsEmpty
        {
            get { return TextSelection.IsEmpty; }
        }

        public override bool IsReversed
        {
            get { return TextSelection.IsReversed; }
            set 
            {
                if (value)
                {
                    SelectRange(TextRange.GetEndPoint().CurrentPosition, TextRange.GetStartPoint().CurrentPosition);
                }
                else
                {
                    SelectRange(TextRange.GetStartPoint().CurrentPosition, TextRange.GetEndPoint().CurrentPosition);
                }
            }
        }

        public override TextView TextView
        {
            get { return _textView; }
        }

        public override DisplayTextPoint GetDisplayStartPoint()
        {
            return TextRange.GetDisplayStartPoint();
        }

        public override DisplayTextPoint GetDisplayEndPoint()
        {
            return TextRange.GetDisplayEndPoint();
        }

        public override VisibilityState Visibility
        {
            get { return TextRange.Visibility; }
        }

        protected override DisplayTextRange CloneDisplayTextRangeInternal()
        {
            return TextRange.Clone();
        }

        protected override IEnumerator<DisplayTextPoint> GetDisplayPointEnumeratorInternal()
        {
            return TextRange.GetEnumerator();
        }

        public override TextPoint GetStartPoint()
        {
            return TextRange.GetStartPoint();
        }

        public override TextPoint GetEndPoint()
        {
            return TextRange.GetEndPoint();
        }

        public override TextBuffer TextBuffer
        {
            get { return TextView.TextBuffer; }
        }

        public override SnapshotSpan AdvancedTextRange
        {
            get { return TextRange.AdvancedTextRange; }
        }

        public override bool MakeUppercase()
        {
            // NOTE: We store this as a *Span* because we don't want it to track
            Span selectionSpan = TextRange.AdvancedTextRange;
            bool isReversed = IsReversed;
            if (!TextRange.MakeUppercase())
                return false;
            TextSelection.Select(new SnapshotSpan(_textView.AdvancedTextView.TextSnapshot, selectionSpan), isReversed);
            return true;
        }

        public override bool MakeLowercase()
        {
            // NOTE: We store this as a *Span* because we don't want it to track
            Span selectionSpan = TextRange.AdvancedTextRange;
            bool isReversed = IsReversed;
            if (!TextRange.MakeLowercase())
                return false;
            TextSelection.Select(new SnapshotSpan(_textView.AdvancedTextView.TextSnapshot, selectionSpan), isReversed);
            return true;
        }

        public override bool Capitalize()
        {
            // NOTE: We store this as a *Span* because we don't want it to track
            Span selectionSpan = TextRange.AdvancedTextRange;
            bool isReversed = IsReversed;
            bool isEmpty = IsEmpty;
            if (!TextRange.Capitalize())
                return false;

            if (!isEmpty)
            {
                TextSelection.Select(new SnapshotSpan(_textView.AdvancedTextView.TextSnapshot, selectionSpan), isReversed);
            }

            return true;
        }

        public override bool ToggleCase()
        {
            // NOTE: We store this as a *Span* because we don't want it to track
            Span selectionSpan = TextRange.AdvancedTextRange;
            bool isReversed = IsReversed;
            bool isEmpty = IsEmpty;
            if (!TextRange.ToggleCase())
                return false;

            if (!isEmpty)
            {
                TextSelection.Select(new SnapshotSpan(_textView.AdvancedTextView.TextSnapshot, selectionSpan), isReversed);
            }

            return true;
        }

        public override bool Delete()
        {
            foreach (var span in TextSelection.SelectedSpans)
            {
                DisplayTextRange selectedRange = TextView.GetTextRange(span.Start, span.End);
                if (!selectedRange.Delete())
                    return false;
            }

            return true;
        }

        public override bool Indent()
        {
            bool singleLineSelection = (GetStartPoint().LineNumber == GetEndPoint().LineNumber);
            bool entireLastLineSelected 
                = (GetStartPoint().CurrentPosition != GetEndPoint().CurrentPosition &&
                   GetStartPoint().CurrentPosition == TextBuffer.GetEndPoint().StartOfLine &&
                   GetEndPoint().CurrentPosition == TextBuffer.GetEndPoint().EndOfLine);
            
            if (singleLineSelection && !entireLastLineSelected)
            {
                TextPoint endPoint = GetEndPoint();
                if (!Delete())
                    return false;
                if (!endPoint.InsertIndent())
                    return false;
                TextView.AdvancedTextView.Caret.MoveTo(endPoint.AdvancedTextPoint);
            }
            else // indent the selected lines
            {
                VirtualSnapshotPoint oldStartPoint = TextSelection.Start;
                VirtualSnapshotPoint oldEndPoint = TextSelection.End;
                bool isReversed = TextSelection.IsReversed;

                ITextSnapshotLine startLine = AdvancedTextRange.Snapshot.GetLineFromPosition(oldStartPoint.Position);
                ITextSnapshotLine endLine = AdvancedTextRange.Snapshot.GetLineFromPosition(oldEndPoint.Position);

                // If the selection span initially starts at the whitespace at the beginning of the line in the startLine or 
                // ends at the whitespace at the beginning of the line in the endLine, restore selection and caret position,
                // *unless* the selection was in box mode.
                bool startAtStartLineWhitespace = oldStartPoint.Position <= _textView.GetTextPoint(startLine.Start).GetFirstNonWhiteSpaceCharacterOnLine().CurrentPosition;
                bool endAtEndLineWhitespace = oldEndPoint.Position < _textView.GetTextPoint(endLine.Start).GetFirstNonWhiteSpaceCharacterOnLine().CurrentPosition;
                bool isBoxSelection = AdvancedSelection.Mode == TextSelectionMode.Box;

                if (isBoxSelection)
                {
                    if (!this.BoxIndent())
                        return false;
                }
                else
                {
                    if (!TextRange.Indent())
                        return false;
                }

                // Computing the new selection and caret position
                VirtualSnapshotPoint newStartPoint = TextSelection.Start;
                VirtualSnapshotPoint newEndPoint = TextSelection.End;

                if (!isBoxSelection && (startAtStartLineWhitespace || endAtEndLineWhitespace))
                {
                    // After indent selection span should start at the start of startLine and end at the start of endLine
                    if (startAtStartLineWhitespace)
                    {
                        newStartPoint = new VirtualSnapshotPoint(AdvancedTextRange.Snapshot, oldStartPoint.Position.Position);
                    }

                    if (endAtEndLineWhitespace && oldEndPoint.Position.Position != endLine.Start && endLine.Length != 0)
                    {
                        int insertedTextSize = _editorOptions.IsConvertTabsToSpacesEnabled() ? _editorOptions.GetTabSize() : 1;
                        newEndPoint = new VirtualSnapshotPoint(AdvancedTextRange.Snapshot, newEndPoint.Position.Position - insertedTextSize);
                    }

                    if (!isReversed)
                        TextSelection.Select(newStartPoint, newEndPoint);
                    else
                        TextSelection.Select(newEndPoint, newStartPoint);

                    TextView.AdvancedTextView.Caret.MoveTo(TextSelection.ActivePoint, PositionAffinity.Successor);
                }
            }
            TextView.AdvancedTextView.Caret.EnsureVisible();
            return true;
        }

        /// <summary>
        /// Indent the given box selection
        /// </summary>
        /// <remarks>
        /// This is fairly close to the normal text range indenting logic, except that it also
        /// indents an empty selection at the endline, which the normal text range ignores.
        /// </remarks>
        private bool BoxIndent()
        {
            string textToInsert = _editorOptions.IsConvertTabsToSpacesEnabled() ? new string(' ', _editorOptions.GetTabSize()) : "\t";
            
            using (ITextEdit edit = TextBuffer.AdvancedTextBuffer.CreateEdit())
            {
                ITextSnapshot snapshot = TextBuffer.AdvancedTextBuffer.CurrentSnapshot;
                int startLineNumber = GetStartPoint().LineNumber;
                int endLineNumber = GetEndPoint().LineNumber;

                for (int i = startLineNumber; i <= endLineNumber; i++)
                {
                    ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);
                    if (line.Length > 0)
                    {
                        if (!edit.Insert(line.Start, textToInsert))
                            return false;
                    }
                }

                edit.Apply();

                if (edit.Canceled)
                    return false;
            }

            return true;
        }

        public override bool Unindent()
        {
            if (GetStartPoint().LineNumber != GetEndPoint().LineNumber &&
                AdvancedSelection.Mode == TextSelectionMode.Box)
            {
                if (!this.BoxUnindent())
                    return false;
            }
            else
            {
                if (!TextRange.Unindent())
                    return false;
            }

            TextView.AdvancedTextView.Caret.EnsureVisible();
            return true;
        }

        /// <summary>
        /// Unindent the given box selection
        /// </summary>
        /// <remarks>
        /// This is fairly close to the normal text range unindenting logic, except that it also
        /// unindents an empty selection at the endline, which the normal text range ignores.
        /// </remarks>
        private bool BoxUnindent()
        {
            using (ITextEdit edit = TextBuffer.AdvancedTextBuffer.CreateEdit())
            {
                ITextSnapshot snapshot = TextBuffer.AdvancedTextBuffer.CurrentSnapshot;
                int startLineNumber = GetStartPoint().LineNumber;
                int endLineNumber = GetEndPoint().LineNumber;

                for (int i = startLineNumber; i <= endLineNumber; i++)
                {
                    ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);
                    if (line.Length > 0)
                    {
                        if (snapshot[line.Start] == '\t')
                        {
                            if (!edit.Delete(new Span(line.Start, 1)))
                                return false;
                        }
                        else
                        {
                            int spacesToRemove = 0;
                            for (; (line.Start + spacesToRemove < snapshot.Length) && (spacesToRemove < _editorOptions.GetTabSize());
                                spacesToRemove++)
                            {
                                if (snapshot[line.Start + spacesToRemove] != ' ')
                                {
                                    break;
                                }
                            }

                            if (spacesToRemove > 0)
                            {
                                if (!edit.Delete(new Span(line.Start, spacesToRemove)))
                                    return false;
                            }
                        }
                    }
                }

                edit.Apply();

                if (edit.Canceled)
                    return false;
            }

            return true;
        }

        public override TextRange Find(string pattern)
        {
            return TextRange.Find(pattern);
        }

        public override TextRange Find(string pattern, FindOptions findOptions)
        {
            return TextRange.Find(pattern, findOptions);
        }

        public override Collection<TextRange> FindAll(string pattern)
        {
            return TextRange.FindAll(pattern);
        }

        public override Collection<TextRange> FindAll(string pattern, FindOptions findOptions)
        {
            return TextRange.FindAll(pattern, findOptions);
        }

        public override bool ReplaceText(string newText)
        {
            // We use *int* and *Span* here because we don't want them to track
            int startPosition = TextRange.GetDisplayStartPoint().CurrentPosition;
            Span newSelectionSpan = new Span(startPosition, newText.Length);
            bool isReversed = IsReversed;
            if (!TextRange.ReplaceText(newText))
                return false;
            TextSelection.Select(new SnapshotSpan(_textView.AdvancedTextView.TextSnapshot, newSelectionSpan), isReversed);
            return true;
        }

        public override string GetText()
        {
            return TextRange.GetText();
        }

        public override void SetStart(TextPoint startPoint)
        {
            this.SelectRange(startPoint.CurrentPosition, GetDisplayEndPoint().CurrentPosition);
        }

        public override void SetEnd(TextPoint endPoint)
        {
            this.ExtendSelection(endPoint);
        }

        public override void MoveTo(TextRange newRange)
        {
            this.SelectRange(newRange);
        }

        protected override IEnumerator<TextPoint> GetEnumeratorInternal()
        {
            return ((TextRange)TextRange).GetEnumerator();
        }
    }
}
