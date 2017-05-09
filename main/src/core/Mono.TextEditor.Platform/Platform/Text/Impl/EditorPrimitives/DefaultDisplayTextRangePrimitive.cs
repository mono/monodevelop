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

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;      
    using Microsoft.VisualStudio.Text.Operations;

    internal sealed class DefaultDisplayTextRangePrimitive : DisplayTextRange
    {
        private TextView _textView;
        private TextRange _bufferRange;

        internal DefaultDisplayTextRangePrimitive(TextView textView, TextRange bufferRange)
        {
            _textView = textView;
            _bufferRange = bufferRange.Clone();

            MoveTo(bufferRange);
        }

        public override TextView TextView
        {
            get { return _textView; }
        }

        public override DisplayTextPoint GetDisplayStartPoint()
        {
            return TextView.GetTextPoint(_bufferRange.GetStartPoint());
        }

        public override DisplayTextPoint GetDisplayEndPoint()
        {
            return TextView.GetTextPoint(_bufferRange.GetEndPoint());
        }

        public override VisibilityState Visibility
        {
            get 
            {
                ITextViewLineCollection renderedTextLines = TextView.AdvancedTextView.TextViewLines;

                SnapshotSpan range = new SnapshotSpan(GetStartPoint().AdvancedTextPoint, 
                                                      GetEndPoint().AdvancedTextPoint);
                SnapshotSpan? overlap = renderedTextLines.FormattedSpan.Overlap(range);

                if (overlap.HasValue)
                {
                    ITextViewLine startLine = renderedTextLines.GetTextViewLineContainingBufferPosition(overlap.Value.Start);
                    ITextViewLine endLine = renderedTextLines.GetTextViewLineContainingBufferPosition(overlap.Value.End);

                    VisibilityState startVisibility = startLine.VisibilityState;

                    if (startLine == endLine)
                    {
                        //Only a single line: visibility is whatever that line is.
                        return startVisibility;
                    }

                    VisibilityState endVisibility = endLine.VisibilityState;

                    if ((startVisibility == VisibilityState.FullyVisible) && 
                        (endVisibility == VisibilityState.FullyVisible))
                    {
                        //Both lines are fully visible and so is everything in between them.
                        return VisibilityState.FullyVisible;
                    }

                    if ((startVisibility != VisibilityState.Hidden) ||
                        (endVisibility != VisibilityState.Hidden) ||
                        (startLine.EndIncludingLineBreak != endLine.Start))
                    {
                        //Either one of the lines is not hidden (so the range is partially visible) or
                        //there are lines between the start that can't be hidden.
                        return VisibilityState.PartiallyVisible;
                    }
                }

                return VisibilityState.Hidden;
            }
        }

        public override bool IsEmpty
        {
            get { return _bufferRange.IsEmpty; }
        }

        protected override DisplayTextRange CloneDisplayTextRangeInternal()
        {
            return new DefaultDisplayTextRangePrimitive(_textView, _bufferRange);
        }

        protected override IEnumerator<DisplayTextPoint> GetDisplayPointEnumeratorInternal()
        {
            DisplayTextPoint displayTextPoint = GetDisplayStartPoint();
            DisplayTextPoint endPoint = GetDisplayEndPoint();
            while (displayTextPoint.CurrentPosition <= endPoint.CurrentPosition)
            {
                yield return displayTextPoint;

                if (displayTextPoint.CurrentPosition == displayTextPoint.AdvancedTextPoint.Snapshot.Length)
                {
                    break;
                }
                displayTextPoint = displayTextPoint.Clone();
                displayTextPoint.MoveToNextCharacter();
            }
        }

        public override TextPoint GetStartPoint()
        {
            return _bufferRange.GetStartPoint();
        }

        public override TextPoint GetEndPoint()
        {
            return _bufferRange.GetEndPoint();
        }

        public override TextBuffer TextBuffer
        {
            get { return _textView.TextBuffer; }
        }

        public override SnapshotSpan AdvancedTextRange
        {
            get { return _bufferRange.AdvancedTextRange; }
        }

        public override bool MakeUppercase()
        {
            return _bufferRange.MakeUppercase();
        }

        public override bool MakeLowercase()
        {
            return _bufferRange.MakeLowercase();
        }

        public override bool Capitalize()
        {
            return _bufferRange.Capitalize();
        }

        public override bool ToggleCase()
        {
            return _bufferRange.ToggleCase();
        }

        public override bool Delete()
        {
            return _bufferRange.Delete();
        }

        public override bool Indent()
        {
            return _bufferRange.Indent();
        }

        public override bool Unindent()
        {
            return _bufferRange.Unindent();
        }

        public override TextRange Find(string pattern)
        {
            return _bufferRange.Find(pattern);
        }

        public override TextRange Find(string pattern, FindOptions findOptions)
        {
            return _bufferRange.Find(pattern, findOptions);
        }

        public override Collection<TextRange> FindAll(string pattern)
        {
            return _bufferRange.FindAll(pattern);
        }

        public override Collection<TextRange> FindAll(string pattern, FindOptions findOptions)
        {
            return _bufferRange.FindAll(pattern, findOptions);
        }

        public override bool ReplaceText(string newText)
        {
            return _bufferRange.ReplaceText(newText);
        }

        public override string GetText()
        {
            return _bufferRange.GetText();
        }

        public override void SetStart(TextPoint startPoint)
        {
            _bufferRange.SetStart(TextView.GetTextPoint(startPoint));
        }

        public override void SetEnd(TextPoint endPoint)
        {
            _bufferRange.SetEnd(TextView.GetTextPoint(endPoint));
        }

        public override void MoveTo(TextRange newRange)
        {
            SetStart(newRange.GetStartPoint());
            SetEnd(newRange.GetEndPoint());
        }

        protected override IEnumerator<TextPoint> GetEnumeratorInternal()
        {
            return _bufferRange.GetEnumerator();
        }
    }
}
