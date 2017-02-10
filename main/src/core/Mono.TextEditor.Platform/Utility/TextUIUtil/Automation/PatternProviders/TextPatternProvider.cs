// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Operations;

    /// <summary>
    /// Represents a provider that supports Text Pattern for our default WPF Editor
    /// </summary>
    public class TextPatternProvider : PatternProvider, ITextProvider
    {

        private IAutomatedElement _automatedView;
        private ITextSearchService2 _textSearchService;
        private ITextStructureNavigatorSelectorService _textStructureNavigatorSelectorService;

        /// <summary>
        /// Constructs a Text pattern provider that's built on top of the given Text Editor
        /// </summary>
        /// <param name="textView">
        /// The text view for which we provide the text pattern
        /// </param>
        public TextPatternProvider(IWpfTextView textView, IAutomatedElement automatedView,
                                   ITextSearchService2 textSearchService, ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService)
            : base(textView) 
        {
            if (automatedView == null)
                throw new ArgumentNullException("automatedView");

            if (textSearchService == null)
                throw new ArgumentNullException("textSearchService");

            _automatedView = automatedView;
            _textSearchService = textSearchService;
            _textStructureNavigatorSelectorService = textStructureNavigatorSelectorService;
        }

        #region ITextProvider Implementation

        /// <summary>
        /// Retrieves the current selection.  For providers that have the concept of
        /// text selection the provider should implement this method and also return
        /// true for the SupportsTextSelection property below.  Otherwise this method
        /// should throw an InvalidOperation exception.
        /// </summary>
        /// <returns>
        /// The range of text that is selected, or a text range provider that is zero 
        /// length at the caret position if the selection is empty.
        /// </returns>
        public ITextRangeProvider[] GetSelection()
        {
            List<ITextRangeProvider> ranges = new List<ITextRangeProvider>();

            //It is possible for a text view to contain an element that has focus and a selection. In that circumstance
            //(without the if below) a screen reader would see two selections and the outermost selection would trump
            //the innermost selection even though the innermost selection is the one that should be read.
            //
            //That problem can be fixed by checking aggregate focus. This method however can be called for views
            //that don't have focus so we need to return something in that case as well.
            if (this.TextView.HasAggregateFocus || !this.TextView.VisualElement.IsKeyboardFocusWithin)
            {
                if (this.TextView.Selection.Mode == TextSelectionMode.Stream)
                {
                    ranges.Add(new TextRangePatternProvider(this.TextView, _automatedView, this.TextView.Selection.StreamSelectionSpan.SnapshotSpan, _textSearchService, _textStructureNavigatorSelectorService));
                }
                else
                {
                    foreach (var span in TextView.Selection.SelectedSpans)
                    {
                        ranges.Add(new TextRangePatternProvider(TextView, _automatedView, span, _textSearchService, _textStructureNavigatorSelectorService));
                    }
                }
            }

            return ranges.ToArray();
        }

        /// <summary>
        /// Retrieves the visible range of text.  That is, it returns a range from the first visible character 
        /// to the last visible character inclusive.
        /// </summary>
        /// <remarks>
        /// UI Automation providers should ensure that they return, at most, the text ranges that are visible within the container. 
        /// Disjoint text ranges may occur when any content of a text container is obscured by an overlapping window or other object, 
        /// or when a text container with a multi-column layout has one or more columns partially scrolled out of view. 
        /// If no text is visible, a degenerate (empty) text range is returned. This empty range can be returned if the text container 
        /// is empty or when all text is scrolled out of view. 
        /// </remarks>
        /// <returns>
        /// The range of text that is visible, or possibly null if there is no visible text whatsoever.  Text 
        /// in the range may still be obscured by an overlapping window.  Also, portions of the range at the 
        /// beginning, in the middle, or at the end may not be visible because they are scrolled off to the 
        /// side.
        /// 
        /// Providers should ensure they return at most a range from the beginning of the first line with 
        /// portions visible through the end of the last line with portions visible.
        /// </returns>
        public ITextRangeProvider[] GetVisibleRanges()
        {
            IWpfTextViewLineCollection textLines = this.TextView.TextViewLines;

            // return a degenerate text if no text is avaialble in the view
            if (textLines == null || textLines.Count == 0)
                return new ITextRangeProvider[] { new TextRangePatternProvider(this.TextView, _automatedView, Span.FromBounds(0, 0), _textSearchService, _textStructureNavigatorSelectorService) };

            List<TextRangePatternProvider> textRangePatterns = new List<TextRangePatternProvider>(textLines.Count);

            ITextAndAdornmentSequencer sequencer = this.TextView.FormattedLineSource.TextAndAdornmentSequencer;

            // for each visible line in the view, obtain the leftmost and rightmost visible characters
            // then use that span to get a collection of text and adornments no the line. Finally, create
            // a text range pattern for each textual element
            foreach (IWpfTextViewLine viewLine in textLines)
            {
                if (viewLine.VisibilityState == VisibilityState.PartiallyVisible || viewLine.VisibilityState == VisibilityState.FullyVisible)
                {
                    SnapshotPoint start = viewLine.GetVirtualBufferPositionFromXCoordinate(this.TextView.ViewportLeft).Position;
                    SnapshotPoint end = viewLine.GetVirtualBufferPositionFromXCoordinate(this.TextView.ViewportRight).Position;

                    if (start == end)
                        continue;

                    start = this.TextView.TextViewModel.GetNearestPointInVisualSnapshot(start, this.TextView.FormattedLineSource.TopTextSnapshot, PointTrackingMode.Negative);
                    end = this.TextView.TextViewModel.GetNearestPointInVisualSnapshot(end, this.TextView.FormattedLineSource.TopTextSnapshot, PointTrackingMode.Negative);

                    foreach (ISequenceElement element in sequencer.CreateTextAndAdornmentCollection(new SnapshotSpan(start, end), this.TextView.FormattedLineSource.SourceTextSnapshot))
                    {
                        // only consider elements that have text
                        if (!element.ShouldRenderText)
                            continue;

                        foreach (SnapshotSpan span in element.Span.GetSpans(this.TextView.TextSnapshot))
                        {
                            textRangePatterns.Add(new TextRangePatternProvider(this.TextView, _automatedView, span, _textSearchService, _textStructureNavigatorSelectorService));
                        }
                    }
                }
            }

            return textRangePatterns.ToArray();
        }

        /// <summary>
        /// Retrieves the range of a child object.
        /// </summary>
        /// <param name="childElement">
        /// The child element.  A provider should check that the passed element is a child of the text 
        /// container, and should throw an InvalidOperationException if it is not.
        /// </param>
        /// <returns>
        /// A range that spans the child element.
        /// </returns>
        public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
        {
            //validate
            if (childElement == null)
                throw new ArgumentNullException("childElement");

            //get the bounding rectangle of the child element and figure out which line it corresponds to, then
            //return an ITextRangeProvider for that line
            Rect? boundingRectangle = childElement.GetPropertyValue(AutomationElementIdentifiers.BoundingRectangleProperty.Id) as Rect?;
            if (boundingRectangle.HasValue)
            {
                ITextViewLine textLine = this.TextView.TextViewLines.GetTextViewLineContainingYCoordinate(boundingRectangle.Value.Top);
                if (textLine != null)
                    return new TextRangePatternProvider(this.TextView, _automatedView, textLine.ExtentIncludingLineBreak, _textSearchService, _textStructureNavigatorSelectorService);
            }

            return null;
        }

        /// <summary>
        /// Finds the degenerate range nearest to a screen coordinate.
        /// </summary>
        /// <param name="screenLocation">
        /// The location in screen coordinates.  
        /// 
        /// The provider should check that the coordinates are within the 
        /// client area of the provider, and should throw an InvalidOperation exception if they are not.
        /// </param>
        /// <returns>
        /// A degenerate range nearest the specified location.
        /// </returns>
        public ITextRangeProvider RangeFromPoint(Point screenLocation)
        {
            //convert supplied coordinates relative to the text view
            Point convertedPoint = TextView.VisualElement.TranslatePoint(screenLocation, TextView.VisualElement);

            //adjust based on view's top and left
            convertedPoint.X += TextView.ViewportLeft;
            convertedPoint.Y += TextView.ViewportTop;
            
            //get nearest line containing character
            ITextViewLine textLine = TextView.TextViewLines.GetTextViewLineContainingYCoordinate(convertedPoint.Y);
            if (textLine == null)
            {
                if (convertedPoint.Y <= this.TextView.TextViewLines.FirstVisibleLine.Top)
                    textLine = this.TextView.TextViewLines.FirstVisibleLine;
                else
                    textLine = this.TextView.TextViewLines.LastVisibleLine;
            }

            //get nearest character
            SnapshotPoint character = textLine.GetVirtualBufferPositionFromXCoordinate(convertedPoint.X).Position;

            return new TextRangePatternProvider(this.TextView, _automatedView, new Span(character, 0), _textSearchService, _textStructureNavigatorSelectorService);
        }

        /// <summary>
        /// True if the text container supports text selection. If the provider returns false then
        /// it should throw InvalidOperation exceptions for ITextInteropProvider.GetSelection and 
        /// ITextRangeProvider.Select.
        /// </summary>
        public SupportedTextSelection SupportedTextSelection 
        { 
            get
            {
                return SupportedTextSelection.Single;
            } 
        }

        /// <summary>
        /// A text range that encloses the main text of the document.  Some auxiliary text such as 
        /// headers, footnotes, or annotations may not be included. 
        /// </summary>
        public ITextRangeProvider DocumentRange
        {
            get
            {
                return new TextRangePatternProvider(this.TextView, _automatedView, new Span(0, TextView.TextSnapshot.Length), _textSearchService, _textStructureNavigatorSelectorService);
            }
        }

        #endregion // ITextProvider Implementation

    }
}
