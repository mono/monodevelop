// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Provider;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Text;
    using System.Windows.Media;
    using System.Windows.Media.TextFormatting;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Operations;

    using Color = System.Drawing.Color;
    using ColorTranslator = System.Drawing.ColorTranslator;
    using Strings = Microsoft.VisualStudio.Text.Utilities.Strings;

    public class TextRangePatternProvider : PatternProvider, ITextRangeProvider
    {

        #region Private Attributes

        private bool _produceScreenReaderFriendlyText;
        private bool _readLineNumbers;

        internal ITrackingPoint _startPoint, _endPoint; //internal for testing
        private IAutomatedElement _automatedView;
        private ITextSearchService2 _textSearchService;
        private ITextStructureNavigatorSelectorService _textStructureNavigatorSelectorService;
        private bool _outputDebugTrace = false; // control whether or not to output traces for debugging purpose

        #endregion // Private Attributes

        /// <summary>
        /// Constructs a Text Range Provider that's tied to a given text editor
        /// </summary>
        /// <param name="textView">
        /// The text view that this range provider is tied to
        /// </param>
        /// <param name="span">
        /// The span of the text range
        /// </param>
        public TextRangePatternProvider(IWpfTextView textView, IAutomatedElement automatedView, Span span, ITextSearchService2 textSearchService, ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService)
            : this(textView, automatedView,
                 textView.TextSnapshot.CreateTrackingPoint(span.Start, PointTrackingMode.Positive),
                 textView.TextSnapshot.CreateTrackingPoint(span.End, PointTrackingMode.Positive), textSearchService, textStructureNavigatorSelectorService)
        {
        }

        /// <summary>
        /// Constructs a Text Range Provider that's tied to a given text editor
        /// </summary>
        /// <param name="textView">
        /// The text view that this range provider is tied to
        /// </param>
        /// <param name="start">
        /// The starting point of the text range
        /// </param>
        /// <param name="end">
        /// The ending point of the text range
        /// </param>
        public TextRangePatternProvider(IWpfTextView textView, IAutomatedElement automatedView, ITrackingPoint start, ITrackingPoint end, ITextSearchService2 textSearchService, ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService)
            : base(textView)
        {
            if (start == null)
                throw new ArgumentNullException("start");

            if (end == null)
                throw new ArgumentNullException("end");

            if (start.GetPosition(textView.TextSnapshot) > end.GetPosition(textView.TextSnapshot))
                throw new InvalidOperationException("endPoint must be positioned after the startPoint in buffer");

            if (automatedView == null)
                throw new ArgumentNullException("automatedView");

            if (textSearchService == null)
                throw new ArgumentNullException("textSearchService");

            _startPoint = start;
            _endPoint = end;
            _automatedView = automatedView;
            _textSearchService = textSearchService;
            _textStructureNavigatorSelectorService = textStructureNavigatorSelectorService;

            _produceScreenReaderFriendlyText = this.TextView.Options.GetOptionValue<bool>(DefaultTextViewOptions.ProduceScreenReaderFriendlyTextId);
            _readLineNumbers = this.TextView.Options.GetOptionValue<bool>(DefaultTextViewHostOptions.LineNumberMarginId);
        }

        #region ITextRangeProvider Implementation

        /// <summary>
        /// Retrieves a collection of all of the children that fall within the range.
        /// </summary>
        /// <returns>
        /// An enumeration of all children that fall within the range.  Children that overlap with the range 
        /// but are not entirely enclosed by it will also be included in the collection.  If there are no 
        /// children then this can return either null or an empty enumeration.
        /// </returns>
        public IRawElementProviderSimple[] GetChildren()
        {
            //we don't provide a children hierarchy for text ranges
            return null;
        }

        /// <summary>
        /// Retrieves a new range covering an identical span of text.  The new range can be manipulated 
        /// independently from the original.
        /// </summary>
        /// <returns>
        /// The new range.
        /// </returns>
        public ITextRangeProvider Clone()
        {
            return new TextRangePatternProvider(TextView, _automatedView, _startPoint, _endPoint, _textSearchService, _textStructureNavigatorSelectorService);
        }

        /// <summary>
        /// Compares this range with another range.
        /// </summary>
        /// <param name="range">
        /// A range to compare.  The range must have come from the same text provider or an 
        /// InvalidArgumentException will be thrown.
        /// </param>
        /// <returns>
        /// true if both ranges span the same text.
        /// </returns>
        public bool Compare(ITextRangeProvider range)
        {
            // Validate
            if (range == null)
                throw new ArgumentNullException("range");

            TextRangePatternProvider textRangeProvider = range as TextRangePatternProvider;
            if (textRangeProvider == null)
                return false;

            // Now, compare
            return textRangeProvider.TextView == TextView &&
                this.CompareEndpoints(TextPatternRangeEndpoint.Start, textRangeProvider, TextPatternRangeEndpoint.Start) == 0 &&
                this.CompareEndpoints(TextPatternRangeEndpoint.End, textRangeProvider, TextPatternRangeEndpoint.End) == 0;
        }

        /// <summary>
        /// Compares the endpoint of this range with the endpoint of another range.
        /// </summary>
        /// <param name="endpoint">
        /// The endpoint of this range to compare.
        /// </param>
        /// <param name="targetRange">
        /// The range with the other endpoint to compare.  The range must have come from the same text provider 
        /// or an InvalidArgumentException will be thrown.
        /// </param>
        /// <param name="targetEndpoint">
        /// The endpoint on the other range to compare.
        /// </param>
        /// <returns>
        /// Returns &lt;0 if this endpoint occurs earlier in the text than the target endpoint. 
        /// Returns 0 if this endpoint is at the same location as the target endpoint. 
        /// Returns &gt;0 if this endpoint occurs later in the text than the target endpoint.
        /// </returns>
        public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            // Validate
            if (targetRange == null)
                throw new ArgumentNullException("targetRange");

            TextRangePatternProvider textRangeProvider = targetRange as TextRangePatternProvider;
            if (textRangeProvider == null)
                throw new ArgumentException(Strings.TargetRangeNotValid);

            // What do we want to compare against?
            int thisPosition = (endpoint == TextPatternRangeEndpoint.Start ? _startPoint : _endPoint).GetPosition(TextView.TextSnapshot);
            int targetPosition = (targetEndpoint == TextPatternRangeEndpoint.Start ? textRangeProvider.StartPoint : textRangeProvider.EndPoint).GetPosition(TextView.TextSnapshot);

            return thisPosition.CompareTo(targetPosition);
        }

        /// <summary>
        /// Normalizes the range to an integral number of enclosing units. This could be used, for example,
        /// to guarantee that a range endpoint is not in the middle of a word.  If the range is already an
        /// integral number of the specified units then it remains unchanged.
        /// </summary>
        /// <remarks>
        /// This method should really be called normalize range.
        /// </remarks>
        /// <param name="unit">
        /// The textual unit.
        /// </param>
        public void ExpandToEnclosingUnit(TextUnit unit)
        {
            OutputDebugTrace("TextRangePatternProvider.ExpandToEnclosingUnit, TextUnit: " + unit);

            unit = FixTextUnit(unit);

            // the algorithm is to first snap the start point to the beginning of the range, then snap the endpoint to the 
            // corresponding end point to the new start point
            var span = this.GetTextUnitBounds(_startPoint.GetPoint(TextView.TextSnapshot), unit);

            _startPoint = TextView.TextSnapshot.CreateTrackingPoint(span.Start, PointTrackingMode.Positive);
            _endPoint = TextView.TextSnapshot.CreateTrackingPoint(span.End, PointTrackingMode.Positive);
        }

        /// <summary>
        /// Searches for a subrange of text that has the specified attribute.  To search the entire document 
        /// use the text provider's document range.
        /// </summary>
        /// <param name="attribute">
        /// The attribute to search for.
        /// </param>
        /// <param name="value">
        /// The value of the specified attribute to search for.
        /// </param>
        /// <param name="backward">
        /// true if the last occurring range should be returned instead of the first.
        /// </param>
        /// <returns>
        /// A subrange with the specified attribute, or null if no such subrange exists.
        /// </returns>
        public ITextRangeProvider FindAttribute(int attribute, object value, bool backward)
        {
            //we don't support search based on format attributes
            throw new NotImplementedException(Strings.UnsupportedSearchBasedOnTextFormatted);
        }

        /// <summary>
        /// Searches for an occurrence of text within the range.
        /// </summary>
        /// <param name="text">
        /// The text to search for.
        /// </param>
        /// <param name="backward">
        /// true if the last occurring range should be returned instead of the first.
        /// </param>
        /// <param name="ignoreCase">
        /// true if case should be ignored for the purposes of comparison.
        /// </param>
        /// <returns>
        /// A subrange with the specified text, or null if no such subrange exists.
        /// </returns>
        public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase)
        {
            // Validate
            if (text == null)
                throw new ArgumentNullException("text");

            //specify search options
            FindOptions findOptions = FindOptions.None;
            if (backward)
            {
                findOptions = FindOptions.SearchReverse;
            }
            if (!ignoreCase)
            {
                findOptions |= FindOptions.MatchCase;
            }

            var start = _startPoint.GetPoint(TextView.TextSnapshot);
            var end = _endPoint.GetPoint(TextView.TextSnapshot);
            var match = _textSearchService.Find(new SnapshotSpan(start, end),
                                                backward ? end : start, text, findOptions);

            if (match.HasValue)
            {
                return new TextRangePatternProvider(TextView, _automatedView, match.Value, _textSearchService, _textStructureNavigatorSelectorService);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves the value of a text attribute over the entire range.
        /// </summary>
        /// <param name="attribute">
        /// The attribute.
        /// </param>
        /// <returns>
        /// The value of the attribute across the range. 
        /// If the attribute's value varies over the range then the value is TextPattern.MixedAttributeValue
        /// </returns>
        public object GetAttributeValue(int attribute)
        {
#if false
            string attributeName = AutomationTextAttribute.LookupById(attribute)?.ProgrammaticName;
            Debug.WriteLine("TextRangePatternProvider.GetAttributeValue for: " + (string.IsNullOrWhiteSpace(attributeName) ? attribute.ToString() : attributeName));
#endif
            object toBeReturnedAttributeValue = AutomationElement.NotSupported;

            bool isFormattingAttribute =
                attribute == TextPattern.BackgroundColorAttribute.Id ||
                attribute == TextPattern.CultureAttribute.Id ||
                attribute == TextPattern.FontNameAttribute.Id ||
                attribute == TextPattern.FontSizeAttribute.Id ||
                attribute == TextPattern.FontWeightAttribute.Id ||
                attribute == TextPattern.ForegroundColorAttribute.Id ||
                attribute == TextPattern.IsItalicAttribute.Id;

            // We only support formatting attributes and the read-only attribute
            if (!(isFormattingAttribute || attribute == TextPattern.IsReadOnlyAttribute.Id))
            {
                toBeReturnedAttributeValue = AutomationElement.NotSupported;
            }
            else if (attribute == TextPattern.IsReadOnlyAttribute.Id)
            {
                toBeReturnedAttributeValue = TextView.TextSnapshot.TextBuffer.IsReadOnly(Span.FromBounds(_startPoint.GetPosition(TextView.TextSnapshot), _endPoint.GetPosition(TextView.TextSnapshot)));
            }
            else // isFormatting == true
            {
                toBeReturnedAttributeValue = GetFormattingAttributeValue(attribute);
            }

#if false
            if (toBeReturnedAttributeValue.Equals(TextPattern.MixedAttributeValue))
            {
                Debug.WriteLine("TextRangePatternProvider.GetAttributeValue returns: TextPattern.MixedAttributeValue");
            }
            else if (toBeReturnedAttributeValue.Equals(AutomationElement.NotSupported))
            {
                Debug.WriteLine("TextRangePatternProvider.GetAttributeValue returns: AutomationElement.NotSupported");
            }
            else
            {
                Debug.WriteLine("TextRangePatternProvider.GetAttributeValue returns: " + toBeReturnedAttributeValue.ToString());
            }
#endif

            return toBeReturnedAttributeValue;
        }

        /// <summary>
        /// Retrieves the bounding rectangles for viewable lines of the range.
        /// </summary>
        /// <returns>
        /// A double array that contains one of the following: 
        /// 
        /// a) An array of bounding rectangles for each visible portion of text in the text range. 
        /// Line feeds in the mentioned text portions are to be presented by a rectangle with the height 
        /// of the containing line and the width of a single space character in the containing line’s 
        /// text font. 
        /// 
        /// b) A single rectangle for the degenerate range representing the caret (selection) having the 
        /// height of the containing line and the width of the system’s caret (SPI_GETCARETWIDTH system 
        /// parameter). An empty array for all other degenerate ranges.
        /// 
        /// c) An empty array for a text range that has screen coordinates placing it completely off-screen 
        /// or scrolled out of view.
        /// 
        /// In every case, the rectangles are given in physical screen coordinates.  The bounding rectangles
        /// should not include any text that is not in the text range.
        /// </returns>
        public double[] GetBoundingRectangles()
        {
            double[] boundingRectangles = null;
            SnapshotSpan rangeSpan = new SnapshotSpan(_startPoint.GetPoint(TextView.TextSnapshot), _endPoint.GetPoint(TextView.TextSnapshot));
#if false
            var startLine = rangeSpan.Start.GetContainingLine();
            var endLine = rangeSpan.End.GetContainingLine();

            Debug.WriteLine("TextRangePatternProvider.GetBoundingRectangles for ({0}, {1}) to ({2}, {3})",
                            startLine.LineNumber + 1, rangeSpan.Start.Position - startLine.Start.Position + 1,
                            endLine.LineNumber + 1, rangeSpan.End.Position - endLine.Start.Position + 1);
#endif
            // Case (b)
            if (rangeSpan.Length == 0)
            {
                // Degenerate range
                // We need to return the caret bounds if we fall on the caret, otherwise we need to return nothing.
                ITextCaret caret = base.TextView.Caret;
                if (caret.Position.BufferPosition == rangeSpan.Start)
                {
                    boundingRectangles = new double[] { caret.Left, caret.Top, caret.Width, caret.Height };
                }
            }
            // Case (a) or (c)
            else
            {
                // Non-degenerate range
                // We need to return visible portions of the range.

                // Get the bounds for the view lines and clip them to the viewport.
                Collection<Microsoft.VisualStudio.Text.Formatting.TextBounds> bounds = TextView.TextViewLines.GetNormalizedTextBounds(rangeSpan);

                if (bounds.Count > 0)
                {
                    List<double> trimmedBounds = new List<double>(bounds.Count * 4);

                    foreach (Microsoft.VisualStudio.Text.Formatting.TextBounds bound in bounds)
                    {
                        bool outsideView = (bound.Left > TextView.ViewportRight || bound.Right < TextView.ViewportLeft ||
                                            bound.Top > TextView.ViewportBottom || bound.Bottom < TextView.ViewportTop);

                        if (!outsideView)
                        {
                            double trimmedLeft = Math.Max(bound.Left, TextView.ViewportLeft);
                            double trimmedRight = Math.Min(bound.Right, TextView.ViewportRight);
                            double trimmedTop = Math.Max(bound.Top, TextView.ViewportTop);
                            double trimmedBottom = Math.Min(bound.Bottom, TextView.ViewportBottom);

                            // Each bound is returned as a collection of 4 doubles in this order: Left, Top, Width, Height
                            trimmedBounds.Add(trimmedLeft);
                            trimmedBounds.Add(trimmedTop);
                            trimmedBounds.Add(trimmedRight - trimmedLeft);
                            trimmedBounds.Add(trimmedBottom - trimmedTop);
                        }
                    }

                    boundingRectangles = trimmedBounds.ToArray();
                }
            }

            if (boundingRectangles != null)
            {
                // Convert calculated coordinates to screen coordinates
                Point topLeft, bottomRight;

                for (int i = 0; i < boundingRectangles.Length; i += 4)
                {
                    // Calculate coordinates relative to left top hand corner of the view's visual element
                    topLeft = new Point(boundingRectangles[i] - TextView.ViewportLeft, boundingRectangles[i + 1] - TextView.ViewportTop);
                    bottomRight = new Point(topLeft.X + boundingRectangles[i + 2], topLeft.Y + boundingRectangles[i + 3]);

                    // Convert positions to screen coordinates
                    topLeft = TextView.VisualElement.PointToScreen(topLeft);
                    bottomRight = TextView.VisualElement.PointToScreen(bottomRight);

                    // Finally, set back the converted values
                    boundingRectangles[i] = topLeft.X;
                    boundingRectangles[i + 1] = topLeft.Y;
                    boundingRectangles[i + 2] = bottomRight.X - topLeft.X;
                    boundingRectangles[i + 3] = bottomRight.Y - topLeft.Y;
                }

                return boundingRectangles;
            }
            else
            {
                // No result was calculated. The range is either degenerate or no parts of it are visible on the view.
                return new double[0];
            }
        }

        /// <summary>
        /// Retrieves the innermost element that encloses this range.
        /// </summary>
        /// <returns>
        /// An element.  Usually this element will be the one that supplied this range.
        /// However, if the text provider supports child elements such as tables or hyperlinks, then the
        /// enclosing element could be a descendant element of the text provider.
        /// </returns>
        public IRawElementProviderSimple GetEnclosingElement()
        {
            return _automatedView.AutomationAdapter.AutomationProvider;
        }

        /// <summary>
        /// Retrieves the text of the range.
        /// </summary>
        /// <param name="maxLength">
        /// Specifies the maximum length of the string to return or -1 if no limit is requested.
        /// </param>
        /// <returns>
        /// The text of the range possibly truncated to the specified limit.
        /// </returns>
        public string GetText(int maxLength)
        {
            // Validate
            if (maxLength < -1)
            {
                throw new ArgumentOutOfRangeException("maxLength");
            }
            if (maxLength == 0)
            {
                return string.Empty;
            }

            // base everything off of the visual snapshot so that only visible text is returned
            ITextSnapshot snapshot = this.TextView.VisualSnapshot;
            SnapshotPoint start = this.TextView.TextViewModel.GetNearestPointInVisualSnapshot(_startPoint.GetPoint(this.TextView.TextSnapshot), snapshot, PointTrackingMode.Negative);
            SnapshotPoint end = this.TextView.TextViewModel.GetNearestPointInVisualSnapshot(_endPoint.GetPoint(this.TextView.TextSnapshot), snapshot, PointTrackingMode.Positive);

            OutputDebugTrace(string.Format("TextRangePatternProvider.GetText, requested text length: {0}, actual text length: {1}", maxLength, end.Position - start.Position));

            SnapshotSpan textRange;

            if (maxLength == -1)
            {
                textRange = new SnapshotSpan(start, end);
            }
            else
            {
                textRange = new SnapshotSpan(start, Math.Min(maxLength, end.Position - start.Position));
            }

            var returnedText = _produceScreenReaderFriendlyText ?
                ScreenReaderTranslator.Translate(textRange, TextView.TextViewModel.DataBuffer.ContentType) : snapshot.GetText(textRange);

            if (_readLineNumbers)
            {
                // Prepend line number to the text so screen readers can read line number before reading the content of the whole line.
                var startLine = start.GetContainingLine();

                // Screen readers (e.g. Narrator) may send a series of requests to fetch the content of line with each request only fetches a few words.
                // Only the first request which starts at the beginning of the line should be prepended with line number
                // Also, need to exclude the case where only the first character of the line is retrieved (e.g. user press right arrow key at the end of previous line).
                if (start.Position == startLine.Start.Position &&
                    end <= startLine.EndIncludingLineBreak &&
                    (end.Position - start.Position) > 1)
                {
                    returnedText = (startLine.LineNumber + 1).ToString() + " " + returnedText ?? string.Empty;
                }
            }

            OutputDebugTrace(string.Format("returned text: '{0}'", returnedText.Replace("\r", "\\r").Replace("\n", "\\n")));
            return returnedText;
        }

        /// <summary>
        /// <para>
        /// Moves the range the specified number of units in the text.  Note that the text is not altered.  
        /// Instead the range spans a different part of the text.
        /// </para><para>
        /// If the range is degenerate (has a length of 0), this method will move it count times based on the text unit.
        /// </para><para>
        /// If the range is nondegenerate, then the method will first normalize the range based on the text unit so that both boundaries
        /// expand to the enclosing unit (if they're not already).
        /// </para><para>
        /// If count is positive, then the range will be collapsed to its end, and moved by unit count - 1 times.
        /// </para><para>
        /// If count is negative, then the range will be collapsed to its start, and moved by unit |count| - 1 times.
        /// </para><para>
        /// Thus, in both cases, collapsing a nondegenerate range, whether or not moving to the start or end of 
        /// the unit following the collapse, counts as a unit.
        /// </para>
        /// </summary>
        /// <param name="unit">
        /// The textual unit for moving. Can only be character, word, line or document.
        /// </param>
        /// <param name="count">
        /// The number of units to move.  A positive count moves the range forward.  
        /// A negative count moves backward. A count of 0 has no effect.
        /// </param>
        /// <returns>
        /// The number of units actually moved, which can be less than the number requested if 
        /// moving the range runs into the beginning or end of the document.
        /// </returns>
        public int Move(TextUnit unit, int count)
        {
#if false
            Debug.WriteLine("TextRangePatternProvider.Move, TextUnit: {0}; count: {1}", unit, count);
#endif
            unit = FixTextUnit(unit);

            // handle boundary cases
            if (count == 0)
            {
                return 0;
            }

            bool wasDegenerate = this.IsDegenerate;

            //Move the start point by the appropriate amount.
            int result = this.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, unit, count);

            if (wasDegenerate)
            {
                //The range was degenerate ... keep it that way.
                _endPoint = _startPoint;
            }
            else
            {
                //The start point should already be aligned, move the endpoint to the end of the enclosing unit).
                this.ExpandToEnclosingUnit(unit);
            }

            return result;
        }

        /// <summary>
        /// Moves an endpoint of this range to coincide with the endpoint of another range.
        /// </summary>
        /// <param name="endpoint">
        /// The endpoint to move.
        /// </param>
        /// <param name="targetRange">
        /// Another range from the same text provider.
        /// </param>
        /// <param name="targetEndpoint">
        /// An endpoint on the other range.
        /// </param>
        public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            // Validate
            if (targetRange == null)
                throw new ArgumentNullException("targetRange");

            TextRangePatternProvider textRangeProvider = targetRange as TextRangePatternProvider;
            if (textRangeProvider == null)
                throw new ArgumentException(Strings.TargetRangeNotValid);

            int moveTo = (targetEndpoint == TextPatternRangeEndpoint.Start ? textRangeProvider.StartPoint.GetPosition(TextView.TextSnapshot) : textRangeProvider.EndPoint.GetPosition(TextView.TextSnapshot));
            if (endpoint == TextPatternRangeEndpoint.Start)
            {
                _startPoint = TextView.TextSnapshot.CreateTrackingPoint(moveTo, PointTrackingMode.Positive);

                // Verify our end point is always greater than the start point
                if (moveTo > _endPoint.GetPosition(TextView.TextSnapshot))
                    _endPoint = TextView.TextSnapshot.CreateTrackingPoint(moveTo, PointTrackingMode.Positive);
            }
            else
            {
                _endPoint = TextView.TextSnapshot.CreateTrackingPoint(moveTo, PointTrackingMode.Positive);

                // Verify our end point is always greater than the start point
                if (moveTo < _startPoint.GetPosition(TextView.TextSnapshot))
                    _startPoint = TextView.TextSnapshot.CreateTrackingPoint(moveTo, PointTrackingMode.Positive);
            }
        }

        /// <summary>
        /// Moves one endpoint of the range the specified number of units in the text.  If the endpoint being 
        /// moved crosses the other endpoint then the other endpoint is moved along too resulting in a 
        /// degenerate range and ensuring the correct ordering of the endpoints. (i.e. always Start&lt;=End)
        /// </summary>
        /// <param name="endpoint">
        /// The endpoint to move.
        /// </param>
        /// <param name="unit">
        /// The textual unit for moving.
        /// </param>
        /// <param name="count">
        /// The number of units to move.  A positive count moves the endpoint forward.  A negative count moves 
        /// backward. A count of 0 has no effect.
        /// </param>
        /// <returns>
        /// The number of units actually moved, which can be less than the number requested if moving the 
        /// endpoint runs into the beginning or end of the document.
        /// </returns>
        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
#if false
            Debug.WriteLine("TextRangePatternProvider.MoveEndpointByUnit, TextUnit: {0}; count: {1}", unit, count);
#endif
            unit = FixTextUnit(unit);

            //handle boundary cases
            if (count == 0)
            {
                return 0;
            }

            SnapshotPoint point = (endpoint == TextPatternRangeEndpoint.Start ? _startPoint : _endPoint).GetPoint(base.TextView.TextSnapshot);
            int result = 0;

            bool moveForward = count > 0;

            for (int i = Math.Abs(count); (i > 0); --i)
            {

                if (moveForward)
                {
                    if (point == point.Snapshot.Length)
                    {
                        //Reached the end of the buffer. We're done.
                        break;
                    }

                    var span = this.GetTextUnitBounds(point, unit);
                    point = span.End;
                }
                else
                {
                    if (point == 0)
                    {
                        //Reached the start of the buffer. We're done.
                        break;
                    }

                    var span = this.GetTextUnitBounds(point - 1, unit);
                    point = span.Start;
                }

                result += (moveForward ? 1 : -1);
            }

            if (endpoint == TextPatternRangeEndpoint.Start)
            {
                _startPoint = point.Snapshot.CreateTrackingPoint(point, PointTrackingMode.Positive);
                var end = _endPoint.GetPoint(point.Snapshot);

                if (end < point)
                    _endPoint = _startPoint;
            }
            else
            {
                _endPoint = point.Snapshot.CreateTrackingPoint(point, PointTrackingMode.Positive);
                var start = _startPoint.GetPoint(point.Snapshot);

                if (start > point)
                    _startPoint = _endPoint;
            }

            return result;
        }

        /// <summary>
        /// Scrolls the text in the provider so the range is within the viewport.
        /// </summary>
        /// <param name="alignToTop">
        /// true if the provider should be scrolled so the range is flush with the top of the viewport.
        /// false if the provider should be scrolled so the range is flush with the bottom.
        /// </param>
        public void ScrollIntoView(bool alignToTop)
        {
            if (alignToTop)
            {
                // We need to bring the start of the range to the top of the viewport
                TextView.DisplayTextLineContainingBufferPosition(_startPoint.GetPoint(TextView.TextSnapshot), 0.0, ViewRelativePosition.Top);
            }
            else
            {
                // We need to bring the end of the range to the bottom of the viewport
                TextView.DisplayTextLineContainingBufferPosition(_endPoint.GetPoint(TextView.TextSnapshot), 0.0, ViewRelativePosition.Bottom);
            }
        }

        /// <summary>
        /// Selects the text of the range within the provider.  If the provider does not have a concept of 
        /// selection then it should return true for ITextInteropProvider.SupportsTextSelection property and 
        /// throw an InvalidOperation exception for this method.
        /// </summary>
        /// <remarks>
        /// If a degenerate text range is provided, the text insertion point will move to the start point of the range.
        /// </remarks>
        public void Select()
        {
#if false
            var start = _startPoint.GetPoint(TextView.TextSnapshot);
            var startLine = start.GetContainingLine();
            Debug.WriteLineIf(this.IsDegenerate,
                              string.Format("TextRangePatternProvider.Select on a degenerate range at position ({0}, {1})",
                                            startLine.LineNumber + 1, 
                                            start.Position - startLine.Start.Position + 1)
                             );
#endif
            base.TextView.Selection.Select(new VirtualSnapshotPoint(_startPoint.GetPoint(TextView.TextSnapshot), 0),
                                           new VirtualSnapshotPoint(_endPoint.GetPoint(TextView.TextSnapshot), 0));

            if (this.IsDegenerate)
            {
                TextView.Caret.MoveTo(new VirtualSnapshotPoint(_endPoint.GetPoint(TextView.TextSnapshot), 0));
            }
        }

        /// <summary>
        /// Adds to the collection of highlighted text in a text container that supports multiple, disjoint selections.
        /// </summary>
        public void AddToSelection()
        {
            // We only support single selection.
        }

        /// <summary>
        /// Removes a highlighted section of text, corresponding to the caller's Start and End endpoints, from the 
        /// collection of highlighted text in a text container that supports multiple, disjoint selections.
        /// </summary>
        public void RemoveFromSelection()
        {
            // We only support single selection.
        }

        #endregion // ITextRangeProvider Implementation

        #region Internal Members

        /// <summary>
        /// Gets the start point of the text range
        /// </summary>
        internal ITrackingPoint StartPoint
        {
            get
            {
                return _startPoint;
            }
        }

        /// <summary>
        /// Gets the end point of the text range
        /// </summary>
        internal ITrackingPoint EndPoint
        {
            get
            {
                return _endPoint;
            }
        }

        #endregion // Internal Members

        #region Private Helpers

        private bool IsDegenerate
        {
            get
            {
                int startPosition = _startPoint.GetPosition(TextView.TextSnapshot);
                int endPosition = _endPoint.GetPosition(TextView.TextSnapshot);

                return startPosition == endPosition;
            }
        }

        SnapshotSpan GetTextUnitBounds(SnapshotPoint position, TextUnit unit)
        {
            SnapshotSpan span;
            switch (unit)
            {
                case TextUnit.Character:
                    {
                        span = base.TextView.GetTextElementSpan(position);
                    }
                    break;

                case TextUnit.Format:
                    {
                        if (position == position.Snapshot.Length)
                        {
                            //Degenerate case at the end of the buffer
                            span = new SnapshotSpan(position.Snapshot, position.Snapshot.Length, 0);
                        }
                        else
                        {
                            var line = base.TextView.GetTextViewLineContainingBufferPosition(position);
                            var formatting = line.GetCharacterFormatting(position);

                            var start = position;
                            while (true)
                            {
                                if (start == 0)
                                    break;

                                var next = start - 1;
                                if (next < line.Start)
                                {
                                    //Don't allow the span to grow past the containing text view line (otherwise we might
                                    //format the entire buffer if the buffer is all plain text).
                                    break;
                                }

                                var f = line.GetCharacterFormatting(next);
                                if (f != formatting)
                                {
                                    break;  //Found different formatting
                                }
                                start = next;
                            }

                            var end = position;
                            while (true)
                            {
                                end = end + 1;                               //We've already checked that position is not at the end of the buffer.
                                if (end >= line.EndIncludingLineBreak)
                                {
                                    //Don't allow the extent to grow beyond the line. This also handles the end of buffer case.
                                    break;
                                }

                                var f = line.GetCharacterFormatting(end);
                                if (f != formatting)
                                {
                                    break;  //Found different formatting
                                }
                            }

                            span = new SnapshotSpan(start, end);
                        }
                    }
                    break;

                case TextUnit.Word:
                    {
                        var navigator = _textStructureNavigatorSelectorService.GetTextStructureNavigator(base.TextView.TextBuffer);
                        var extent = navigator.GetExtentOfWord(position);
                        span = extent.Span;

                        if (extent.IsSignificant)
                        {
                            //We're in a "significant" span. Check the next word (& add it to this span if it isn't).
                            if (extent.Span.End < position.Snapshot.Length)
                            {
                                var nextExtent = navigator.GetExtentOfWord(extent.Span.End);
                                if (!nextExtent.IsSignificant)
                                {
                                    span = new SnapshotSpan(extent.Span.Start, nextExtent.Span.End);
                                }
                            }
                        }
                        else
                        {
                            //We're in whitespace so grow the span to include the previous token
                            if (extent.Span.Start != 0)
                            {
                                var previousExtent = navigator.GetExtentOfWord(extent.Span.Start - 1);
                                span = new SnapshotSpan(previousExtent.Span.Start, extent.Span.End);
                            }
                        }
                    }
                    break;

                case TextUnit.Line:
                    {
                        var line = base.TextView.GetTextViewLineContainingBufferPosition(position);
                        span = line.ExtentIncludingLineBreak;
                    }
                    break;

                default:
                    {
                        span = new SnapshotSpan(position.Snapshot, 0, position.Snapshot.Length);
                    }
                    break;
            }

            return span;
        }

        /// <summary>
        /// Gets a list of all text view lines that overlap with the passed <see cref="SnapshotSpan"/>
        /// irrespective of their visibility state.
        /// </summary>
        private List<IWpfTextViewLine> GetOverlappingTextLines(SnapshotSpan rangeSpan)
        {
            SnapshotPoint start = rangeSpan.Start;
            SnapshotPoint end = rangeSpan.End;

            List<IWpfTextViewLine> overlappingLines = new List<IWpfTextViewLine>();
            IWpfTextViewLine lineIterator = null;

            do
            {
                lineIterator = TextView.GetTextViewLineContainingBufferPosition(start);
                start = lineIterator.EndIncludingLineBreak;
                overlappingLines.Add(lineIterator);
            }
            while (!lineIterator.ContainsBufferPosition(end));

            return overlappingLines;
        }

        /// <summary>
        /// Gets the value for the specified attribute from the TextRunProperties.
        /// AutomationElement.NotSupported is returned if the TextRunProperties is null or doesn't have a value for the attribute.
        /// </summary>
        private object GetAttributeValueFromTextRunProperties(int attribute, TextRunProperties t)
        {
            if (t == null)
            {
                return AutomationElement.NotSupported;
            }

            object value = AutomationElement.NotSupported;

            if (attribute == TextPattern.CultureAttribute.Id)
            {
                CultureInfo culture = t.CultureInfo;
                if (culture != null)
                {
                    value = culture.LCID;
                }
            }
            else if (attribute == TextPattern.BackgroundColorAttribute.Id)
            {
                SolidColorBrush background = t.BackgroundBrush as SolidColorBrush;
                if (background != null)
                {
                    value = ColorTranslator.ToOle(Color.FromArgb(background.Color.A, background.Color.R, background.Color.G, background.Color.B));
                }
            }
            else if (attribute == TextPattern.ForegroundColorAttribute.Id)
            {
                SolidColorBrush foreground = t.ForegroundBrush as SolidColorBrush;
                if (foreground != null)
                {
                    value = ColorTranslator.ToOle(Color.FromArgb(foreground.Color.A, foreground.Color.R, foreground.Color.G, foreground.Color.B));
                }
            }
            else if (attribute == TextPattern.FontNameAttribute.Id)
            {
                value = t.Typeface.FontFamily.ToString();
            }
            else if (attribute == TextPattern.FontSizeAttribute.Id)
            {
                // the rendering size is in device independent pixels, we need to convert it to points
                // pt = 1 / 72 inch
                // dip = 1 / 96 inch
                value = Math.Round(t.FontRenderingEmSize * 72 / 96);
            }
            else if (attribute == TextPattern.FontWeightAttribute.Id)
            {
                value = t.Typeface.Weight.ToOpenTypeWeight();
            }
            else if (attribute == TextPattern.IsItalicAttribute.Id)
            {
                value = t.Typeface.Style == FontStyles.Italic;
            }
            else
            {
                Debug.Fail("Hit unexpected case when returning text attributes");
            }

            return value;
        }

        /// <summary>
        /// Gets an object representing a value on the formatting attribute from the TextRunProperties in this text range.
        /// Returns AutomationElement.NotSupported if this range doesn't support the attribute.
        /// Returns TextPattern.MixedAttributeValue if the value on the attribute varies across this text range.
        /// </summary>
        private object GetFormattingAttributeValue(int attribute)
        {
            //Span for the range
            SnapshotSpan rangeSpan = new SnapshotSpan(_startPoint.GetPoint(TextView.TextSnapshot), _endPoint.GetPoint(TextView.TextSnapshot));
            TextRunProperties textRunProperties = null;

            foreach (IWpfTextViewLine formattedLine in this.GetOverlappingTextLines(rangeSpan))
            {
                SnapshotSpan? overlap = rangeSpan.Overlap(formattedLine.ExtentIncludingLineBreak);
                if (overlap.HasValue)
                {
                    // Walk through the TextRunProperties to check if attribute value changes across the overlapped range over this text line
                    for (int i = 0; i < overlap.Value.Length; i++)
                    {
                        TextRunProperties newProperties = formattedLine.GetCharacterFormatting(overlap.Value.Start + i);
                        if (newProperties == null)
                        {
                            return AutomationElement.NotSupported;
                        }

                        // Set up basepoint for attribute value comparison
                        if (textRunProperties == null)
                        {
                            textRunProperties = newProperties;
                        }
                        // TextRunProperties objects are interned in the editor
                        else if (!object.ReferenceEquals(textRunProperties, newProperties))
                        {
                            // These two TextRunProperties objects are different, check if they have same value on specified attribute
                            object value1 = GetAttributeValueFromTextRunProperties(attribute, textRunProperties);
                            object value2 = GetAttributeValueFromTextRunProperties(attribute, newProperties);

                            // TextRunproperties changed at this point, reset comparison basepoint
                            textRunProperties = newProperties;

                            if (!value1.Equals(value2))
                            {
                                return TextPattern.MixedAttributeValue;
                            }
                        }
                    }
                }
            }

            return GetAttributeValueFromTextRunProperties(attribute, textRunProperties);
        }

        /// <summary>
        /// Moves a given text point by the format unit. The format unit consists of a contiguous section of text where
        /// formatting properties of font face, font size, bold, italics, culture info, text decorations and color are identical.
        /// </summary>
        private void MoveByFormat(DisplayTextPoint pointToMove, bool moveForward)
        {
            SnapshotPoint currentPoint = pointToMove.AdvancedTextPoint;
            TextRunProperties currentFormattingProperties = this.TextView.GetTextViewLineContainingBufferPosition(currentPoint).GetCharacterFormatting(currentPoint);
            TextRunProperties formatIterator = currentFormattingProperties;

            // TextRunProperties objects are interned in the editor
            while (object.ReferenceEquals(formatIterator, currentFormattingProperties))
            {
                if (moveForward)
                {
                    if (currentPoint.Position == this.TextView.TextSnapshot.Length)
                        break;

                    pointToMove.MoveToNextCharacter();
                }
                else
                {
                    if (currentPoint.Position == 0)
                        break;

                    pointToMove.MoveToPreviousCharacter();
                }

                currentPoint = pointToMove.AdvancedTextPoint;
                formatIterator = this.TextView.GetTextViewLineContainingBufferPosition(currentPoint).GetCharacterFormatting(currentPoint);
            }
        }

        /// <summary>
        /// Moves the provided point to the bounadries of the format on which the point resides towards the direction specified.
        /// </summary>
        private void NormalizeByFormat(DisplayTextPoint pointToMove, bool moveForward)
        {
            DisplayTextPoint tracker = pointToMove.Clone();
            SnapshotPoint currentPoint = tracker.AdvancedTextPoint;
            TextRunProperties currentFormattingProperties = this.TextView.GetTextViewLineContainingBufferPosition(currentPoint).GetCharacterFormatting(currentPoint);
            TextRunProperties formatIterator = currentFormattingProperties;
            do
            {
                pointToMove.MoveTo(tracker.CurrentPosition);

                if (moveForward)
                {
                    if (currentPoint.Position == this.TextView.TextSnapshot.Length)
                        break;

                    tracker.MoveToNextCharacter();
                }
                else
                {
                    if (currentPoint.Position == 0)
                        break;

                    tracker.MoveToPreviousCharacter();
                }

                currentPoint = tracker.AdvancedTextPoint;
                formatIterator = this.TextView.GetTextViewLineContainingBufferPosition(currentPoint).GetCharacterFormatting(currentPoint);

            } while (formatIterator == currentFormattingProperties);
        }

        /// <summary>
        /// Checks to see if the provided <see cref="TextUnit"/> is a valid movement unit and if not, it changes it to the nearest acceptable unit.
        /// </summary>
        /// <remarks>
        /// The MSDN documentation dictates to "default up to the bigger closest supported TextUnit if the given TextUnit is not supported by the control." 
        /// The order, from smallest to largest is:
        /// Character 
        /// Format 
        /// Word 
        /// Line 
        /// Paragraph 
        /// Page 
        /// Document 
        ///</remarks>
        private static TextUnit FixTextUnit(TextUnit unit)
        {
            // we don't support paragraph or page
            if (unit == TextUnit.Paragraph || unit == TextUnit.Page)
                return TextUnit.Document;
            else
                return unit;
        }

        /// <summary>
        /// Moves a given <see cref="TextPoint"/> count times based on the <see cref="TextUnit"/> provided and takes
        /// into consideration direction of move based on moveForward. Returns the number of times the text point
        /// was actually moved.
        /// </summary>
        private int MovePoint(DisplayTextPoint point, int count, TextUnit unit, bool moveForward)
        {
            // handle boundary cases

            if (count == 0)
            {
                return 0;
            }
            if (moveForward)
            {
                if (point.CurrentPosition == TextView.TextSnapshot.Length)
                {
                    return 0;
                }
            }
            else
            {
                if (point.CurrentPosition == 0)
                {
                    return 0;
                }
            }

            //handle general cases

            int moves = 0;
            for (int i = 0; i < count; ++i)
            {
                if (moveForward)
                {
                    switch (unit)
                    {
                        case TextUnit.Character:
                            point.MoveToNextCharacter();
                            break;
                        case TextUnit.Line:
                            point.MoveToBeginningOfNextViewLine();
                            break;
                        case TextUnit.Word:
                            point.MoveToNextWord();
                            break;
                        case TextUnit.Document:
                            point.MoveToEndOfDocument();
                            break;
                        case TextUnit.Format:
                            this.MoveByFormat(point, moveForward);
                            break;
                        default:
                            Debug.Fail("Shouldn't reach here; invalid text unit has been supplied to method");
                            break;
                    }
                }
                else
                {
                    switch (unit)
                    {
                        case TextUnit.Character:
                            point.MoveToPreviousCharacter();
                            break;
                        case TextUnit.Line:
                            point.MoveToBeginningOfPreviousViewLine();
                            break;
                        case TextUnit.Word:
                            point.MoveToPreviousWord();
                            break;
                        case TextUnit.Document:
                            point.MoveToStartOfDocument();
                            break;
                        case TextUnit.Format:
                            this.MoveByFormat(point, moveForward);
                            break;
                        default:
                            Debug.Fail("Shouldn't reach here; invalid text unit has been supplied to method.");
                            break;
                    }
                }

                ++moves;

                if (moveForward)
                {
                    if (point.CurrentPosition == TextView.TextSnapshot.Length)
                    {
                        break;
                    }
                }
                else
                {
                    if (point.CurrentPosition == 0)
                    {
                        break;
                    }
                }
            }
            return moves;
        }

        [Conditional("DEBUG")]
        private void OutputDebugTrace(string text)
        {
            Debug.WriteLineIf(_outputDebugTrace, text);
        }

        #endregion // Private Helpers
    }
}
