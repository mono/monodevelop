//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;

    internal sealed class DefaultTextViewPrimitive : TextView
    {
        private ITextView _textView;
        private Caret _caret;
        private Selection _selection;
        private TextBuffer _textBuffer;
        private IViewPrimitivesFactoryService _viewPrimitivesFactory;

        internal DefaultTextViewPrimitive(ITextView textView, IViewPrimitivesFactoryService viewPrimitivesFactory, IBufferPrimitivesFactoryService bufferPrimitivesFactory)
        {
            _textView = textView;

            _viewPrimitivesFactory = viewPrimitivesFactory;

            _textBuffer = bufferPrimitivesFactory.CreateTextBuffer(textView.TextBuffer);

            _caret = _viewPrimitivesFactory.CreateCaret(this);
            _selection = _viewPrimitivesFactory.CreateSelection(this);
        }

        public override void MoveLineToTop(int lineNumber)
        {
            ITextSnapshotLine line = _textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber);
            _textView.DisplayTextLineContainingBufferPosition(line.Start, 0.0, ViewRelativePosition.Top);
        }

        public override void MoveLineToBottom(int lineNumber)
        {
            ITextSnapshotLine line = _textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber);
            _textView.DisplayTextLineContainingBufferPosition(line.Start, 0.0, ViewRelativePosition.Bottom);
        }

        public override void ScrollUp(int lines)
        {
            _textView.ViewScroller.ScrollViewportVerticallyByPixels(((double)lines) * _textView.LineHeight);
        }

        public override void ScrollDown(int lines)
        {
            _textView.ViewScroller.ScrollViewportVerticallyByPixels(- ((double)lines) * _textView.LineHeight);
        }

        public override void ScrollPageDown()
        {
            _textView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Down);
        }

        public override void ScrollPageUp()
        {
            _textView.ViewScroller.ScrollViewportVerticallyByPage(ScrollDirection.Up);
        }

        public override bool Show(DisplayTextPoint point, HowToShow howToShow)
        {
            if (howToShow == HowToShow.AsIs)
            {
                _textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(point.AdvancedTextPoint, 0), EnsureSpanVisibleOptions.MinimumScroll);
            }
            else if (howToShow == HowToShow.Centered)
            {
                _textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(point.AdvancedTextPoint, 0), EnsureSpanVisibleOptions.AlwaysCenter);
            }
            else if (howToShow == HowToShow.OnFirstLineOfView)
            {
                _textView.DisplayTextLineContainingBufferPosition(point.AdvancedTextPoint, 0.0, ViewRelativePosition.Top);
            }
            return point.IsVisible;
        }

        public override VisibilityState Show(DisplayTextRange textRange, HowToShow howToShow)
        {
            if (howToShow == HowToShow.AsIs)
            {
                _textView.ViewScroller.EnsureSpanVisible(textRange.AdvancedTextRange, EnsureSpanVisibleOptions.MinimumScroll);
            }
            else if (howToShow == HowToShow.Centered)
            {
                _textView.ViewScroller.EnsureSpanVisible(textRange.AdvancedTextRange, EnsureSpanVisibleOptions.AlwaysCenter);
            }
            else if (howToShow == HowToShow.OnFirstLineOfView)
            {
                _textView.DisplayTextLineContainingBufferPosition(textRange.AdvancedTextRange.Start, 0.0, ViewRelativePosition.Top);
            }

            return textRange.Visibility;
        }

        public override DisplayTextPoint GetTextPoint(int position)
        {
            return _viewPrimitivesFactory.CreateDisplayTextPoint(this, position);
        }

        public override DisplayTextPoint GetTextPoint(TextPoint textPoint)
        {
            return GetTextPoint(textPoint.CurrentPosition);
        }

        public override DisplayTextPoint GetTextPoint(int line, int column)
        {
            ITextSnapshotLine snapshotLine = _textView.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(line);
            return GetTextPoint(snapshotLine.Start + column);
        }

        public override DisplayTextRange GetTextRange(TextPoint startPoint, TextPoint endPoint)
        {
            return GetTextRange(TextBuffer.GetTextRange(startPoint, endPoint));
        }

        public override DisplayTextRange GetTextRange(TextRange textRange)
        {
            return _viewPrimitivesFactory.CreateDisplayTextRange(this, textRange);
        }

        public override DisplayTextRange GetTextRange(int startPosition, int endPosition)
        {
            return GetTextRange(TextBuffer.GetTextPoint(startPosition), TextBuffer.GetTextPoint(endPosition));
        }

        public override DisplayTextRange VisibleSpan
        {
            get { return GetTextRange(_textView.TextViewLines.FirstVisibleLine.Start, _textView.TextViewLines.LastVisibleLine.EndIncludingLineBreak); }
        }

        public override ITextView AdvancedTextView
        {
            get { return _textView; }
        }

        public override Caret Caret
        {
            get { return _caret; }
        }

        public override Selection Selection
        {
            get { return _selection; }
        }

        public override TextBuffer TextBuffer
        {
            get { return _textBuffer; }
        }
    }
}
