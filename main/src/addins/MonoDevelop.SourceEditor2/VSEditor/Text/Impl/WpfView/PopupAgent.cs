//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Outlining;
    using Microsoft.VisualStudio.Text.Utilities;
    using System.Windows.Input;
    using System.Collections.Generic;
    using Xwt;
    using Mono.TextEditor;
    using MonoDevelop.Components;
    using Rect = Xwt.Rectangle;

    internal class PopupAgent : ISpaceReservationAgent
    {
		internal readonly Mono.TextEditor.MonoTextEditor _textView;
        internal readonly ISpaceReservationManager _manager;
        internal ITrackingSpan _visualSpan;
        internal PopupStyles _style;
        internal Widget _mouseContainer;
        internal readonly PopupOrWindowContainer _popup;
        private const int MaxPopupCacheSize = 10;
        private const double BelowTheLineBufferHint = 3.0;

		public PopupAgent(Mono.TextEditor.MonoTextEditor textView, ISpaceReservationManager manager, ITrackingSpan visualSpan, PopupStyles style, Widget content)
        {
            if (textView == null)
                throw new ArgumentNullException("textView");
            if (manager == null)
                throw new ArgumentNullException("manager");
            if (visualSpan == null)
                throw new ArgumentNullException("visualSpan");
            if (((int)style & ~(0xff)) != 0)        //Union of all the legal style bits.
                throw new ArgumentOutOfRangeException("style");
            if (content == null)
                throw new ArgumentNullException("content");
            if ((style & PopupStyles.DismissOnMouseLeaveText) != 0 && (style & PopupStyles.DismissOnMouseLeaveTextOrContent) != 0)
                throw new ArgumentException("Can't specify both PopupStyles.DismissOnMouseLeaveText and PopupStyles.DismissOnMouseLeaveTextOrContent", "style");

            _textView = textView;
            _manager = manager;
            _visualSpan = visualSpan;
            _style = style;

            var popupCache = textView.Properties.GetOrCreateSingletonProperty<Dictionary<WeakReferenceForDictionaryKey, PopupOrWindowContainer>>(
                                                                              () => new Dictionary<WeakReferenceForDictionaryKey, PopupOrWindowContainer>(MaxPopupCacheSize));

            if (!popupCache.TryGetValue(new WeakReferenceForDictionaryKey(content), out _popup))
            {
                _popup = PopupOrWindowContainer.Create(content, _textView.VisualElement);

                if (popupCache.Count == MaxPopupCacheSize)
                {
                    popupCache.Clear();
                }
                popupCache.Add(new WeakReferenceForDictionaryKey(content), _popup);
            }
        }

        public void SetVisualSpan(ITrackingSpan visualSpan)
        {
            _visualSpan = visualSpan;
        }

        #region ISpaceReservationAgent Members
        public Geometry PositionAndDisplay(Geometry reservedSpace)
        {
            //This method should only be called from the popup manager (which should never call it when the containing
            //view is hidden or in the middle of a layout).

            //This method does not support virtual whitespace positioning.  An attempt to support it by using caret position was introduced, but
            //regressed the behavior that popup should stay in place as the caret moves.  If in the future we need to support virtual whitespace,
            //consider using virtual span instead.

            //Update the visual span to the current snapshot.
            SnapshotSpan visualSpan = _visualSpan.GetSpan(_textView.TextSnapshot);

            // If the style indicates that we should dismiss when the mouse leaves the span of the text, then we should make an
            // initial check to make sure the mouse starts-off in the span.  If not, we should fail to position.
            if ((_style & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) != 0)
            {
                _textView.VisualElement.GetPointer(out int x, out int y);
                if (this.ShouldClearToolTipOnMouseMove(new Point(x, y)))
                    return null;
            }

            Rect? spanRectangle = null;
            if (visualSpan.Length > 0)
            {
                double left = double.MaxValue;
                double top = double.MaxValue;
                double right = double.MinValue;
                double bottom = double.MinValue;

                var bounds = _textView.TextViewLines.GetNormalizedTextBounds(visualSpan);
                foreach (var bound in bounds)
                {
                    left = Math.Min(left, bound.Left);
                    top = Math.Min(top, bound.TextTop);
                    right = Math.Max(right, bound.Right);
                    bottom = Math.Max(bottom, bound.TextBottom);
                }

                // If the start of the span lies within the view, use that instead of the left-bound of the span as a whole.
                // This will cause popups to be left-aligned with the start of their span, if at all possible.
                var startLine = _textView.TextViewLines.GetTextViewLineContainingBufferPosition(visualSpan.Start);
                if (startLine != null)
                {
                    var startPointBounds = startLine.GetExtendedCharacterBounds(visualSpan.Start);
                    if ((startPointBounds.Left < right) && (startPointBounds.Left >= _textView.ViewportLeft) && (startPointBounds.Left < _textView.ViewportRight))
                    {
                        left = startPointBounds.Left;
                    }
                }

                //Special case handling for when the end of the span is at the start of a line.
                ITextViewLine line = _textView.TextViewLines.GetTextViewLineContainingBufferPosition(visualSpan.End);
                if ((line != null) && (line.Start == visualSpan.End))
                {
                    bottom = Math.Max(bottom, line.TextBottom);
                }

                if (left < right)
                {
                    spanRectangle = new Rect(left, top, right - left, bottom - top);
                }
            }
            else
            {
                //visualSpan is zero length so the default MarkerGeometry will be null. Create a custom marker geometry based on the location.
                ITextViewLine line = _textView.TextViewLines.GetTextViewLineContainingBufferPosition(visualSpan.Start);
                if (line != null)
                {
                    TextBounds bounds = line.GetCharacterBounds(visualSpan.Start);
                    spanRectangle = new Rect(bounds.Left, bounds.TextTop, 0.0, bounds.TextHeight);
                }
            }

            if (spanRectangle.HasValue)
            {
                //Get the portion of the span geometry that is inside the view.
                Rect viewRect = new Rect(_textView.ViewportLeft, _textView.ViewportTop, _textView.ViewportWidth, _textView.ViewportHeight);

                Rect spanRect = spanRectangle.Value;
                spanRect = spanRect.Intersect(viewRect);

				if (spanRect != default(Rect))
                {
                    // Determine two different rectangles for the span.  One is the span in its raw form.  The other is a "guess" at
                    // what the already-reserved space around the span will be.  We have a very-prevalent space reservation agent (the
                    // current line agent) that reserves the current line plus a 3-pixel buffer below the line.  We'll optimistically
                    // guess that this agent might be in-play and attempt to avoid it.
                    Rect spanRectWithBuffer = new Rect(spanRect.Left, spanRect.Top, spanRect.Right - spanRect.Left, spanRect.Bottom - spanRect.Top + BelowTheLineBufferHint);

                    //Some of the text associated with the popup is visible, show the popup.
                    Rect spanRectInScreenCoordinates = new Rect(this.GetScreenPointFromTextXY(spanRect.Left, spanRect.Top),
                                                                this.GetScreenPointFromTextXY(spanRect.Right, spanRect.Bottom));
                    Rect spanRectWithBufferInScreenCoordinates = new Rect(this.GetScreenPointFromTextXY(spanRectWithBuffer.Left, spanRectWithBuffer.Top),
                                                                          this.GetScreenPointFromTextXY(spanRectWithBuffer.Right, spanRectWithBuffer.Bottom));
                    Rect screenRect = Xwt.Desktop.GetScreenAtLocation(spanRectInScreenCoordinates.TopLeft).Bounds;//TODO: Check if we should use VisualBounds

                    Size desiredSize = _popup.Size;
                    //The popup size specified in deivice pixels. Convert these to logical
                    //pixels for purposes of calculating the actual size of the popup.
                    //TODO desiredSize = new Size (desiredSize.Width / WpfHelper.DeviceScaleX, desiredSize.Height / WpfHelper.DeviceScaleY);
                    desiredSize = new Size(desiredSize.Width, desiredSize.Height);

                    PopupStyles alternateStyle = _style ^ PopupStyles.PreferLeftOrTopPosition;

                    Rect reservedRect = reservedSpace.Bounds;
                    Point topLeft = new Point(Math.Min(spanRectInScreenCoordinates.Left, reservedRect.Left),
                                              Math.Min(spanRectInScreenCoordinates.Top, reservedRect.Top));
                    Point bottomRight = new Point(Math.Max(spanRectInScreenCoordinates.Right, reservedRect.Right),
                                                  Math.Max(spanRectInScreenCoordinates.Bottom, reservedRect.Bottom));
                    reservedRect = new Rect(topLeft, bottomRight);

                    //There are 6 possible locations for the popup.  The order of preference is determined by the presence of the
                    //'PositionClosest' PopupStyle.
                    //
                    //  Without 'PositionClosest':
                    //  1 .. On the desired side of the span.
                    //  2 .. On the desired side of the span with a bit of buffer.
                    //  3 .. on the desired side of the reserved rectangle.
                    //  4 .. On the alternate side of the span.
                    //  5 .. On the alternate side of the span with a bit of buffer.
                    //  6 .. on the alternate side of the reserved rectangle.
                    //
                    //  With 'PositionClosest':
                    //  1 .. On the desired side of the span.
                    //  2 .. On the desired side of the span with a bit of buffer.
                    //  3 .. On the alternate side of the span.
                    //  4 .. On the alternate side of the span with a bit of buffer.
                    //  5 .. on the desired side of the reserved rectangle.
                    //  6 .. on the alternate side of the reserved rectangle.
                    //
                    //Try each location till we get a winner.
                    //  A location is a winner if it is disjoint from the original reserved rect and
                    //  the edges of the screen.

                    Tuple<PopupStyles, Rect>[] positionChoices;
					if (reservedRect != default(Rect))
                    {
                        if ((_style & PopupStyles.PositionClosest) == 0)
                        {
                            positionChoices = new Tuple<PopupStyles, Rect>[]
                            {
                                new Tuple<PopupStyles,Rect>(_style, spanRectInScreenCoordinates),
                                new Tuple<PopupStyles,Rect>(_style, spanRectWithBufferInScreenCoordinates),
                                new Tuple<PopupStyles,Rect>(_style, reservedRect),
                                new Tuple<PopupStyles,Rect>(alternateStyle, spanRectInScreenCoordinates),
                                new Tuple<PopupStyles,Rect>(alternateStyle, spanRectWithBufferInScreenCoordinates),
                                new Tuple<PopupStyles,Rect>(alternateStyle, reservedRect),
                            };
                        }
                        else
                        {
                            positionChoices = new Tuple<PopupStyles, Rect>[]
                            {
                                new Tuple<PopupStyles,Rect>(_style, spanRectInScreenCoordinates),
                                new Tuple<PopupStyles,Rect>(_style, spanRectWithBufferInScreenCoordinates),
                                new Tuple<PopupStyles,Rect>(alternateStyle, spanRectInScreenCoordinates),
                                new Tuple<PopupStyles,Rect>(alternateStyle, spanRectWithBufferInScreenCoordinates),
                                new Tuple<PopupStyles,Rect>(_style, reservedRect),
                                new Tuple<PopupStyles,Rect>(alternateStyle, reservedRect),
                            };
                        }
                    }
                    else
                    {
                        positionChoices = new Tuple<PopupStyles, Rect>[]
                        {
                            new Tuple<PopupStyles,Rect>(_style, spanRectInScreenCoordinates),
                            new Tuple<PopupStyles,Rect>(_style, spanRectWithBufferInScreenCoordinates),
                            new Tuple<PopupStyles,Rect>(alternateStyle, spanRectInScreenCoordinates),
                            new Tuple<PopupStyles,Rect>(alternateStyle, spanRectWithBufferInScreenCoordinates),
                        };
                    }

                    Rect location = Rect.Zero;
                    foreach (var choice in positionChoices)
                    {
                        Rect locationToTry = GetLocation(choice.Item1, desiredSize, spanRectInScreenCoordinates, choice.Item2, screenRect);
                        if (DisjointWithPadding(reservedSpace, locationToTry) && ContainsWithPadding(screenRect, locationToTry))
                        {
                            location = locationToTry;
                            _style = choice.Item1;
                            break;
                        }
                    }

                    // If we couldn't locate a place to live, tell the manager we want to go away.
                    if (location == Rect.Zero)
                        return null;

                    if (!_popup.IsVisible)
                        this.RegisterForEvents();

                    _popup.DisplayAt(location.TopLeft);

                    GeometryGroup requestedSpace = new GeometryGroup();
                    requestedSpace.Children.Add(new RectangleGeometry(spanRectInScreenCoordinates));
                    requestedSpace.Children.Add(new RectangleGeometry(location));

                    return requestedSpace;
                }
            }

            //The text associated with the popup visualSpan is not visible: tell the manager we want to go away.
            return null;
        }

        public bool IsMouseOver
        {
            get
            {
                Gdk.Display.Default.GetPointer(out int x, out int y);
                return _popup.IsVisible ? _popup.Content.ScreenBounds.Contains(x, y) : false;
            }
        }

        public void Hide()
        {
            if (_popup.IsVisible)
            {
                _popup.Hide();

                this.UnregisterForEvents();
            }
        }

        public bool HasFocus
        {
            get
            {
                return _popup.IsVisible && _popup.IsKeyboardFocusWithin;
            }
        }

        public event EventHandler LostFocus;
        public event EventHandler GotFocus;

        #endregion

        private void RegisterForEvents()
        {
            // For tooltips with style DismissOnMouseLeave
            if ((_style & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) != 0)
            {
                _textView.VisualElement.MotionNotifyEvent += this.OnMouseMove;

                //TODO: This used to be Mouse.DirectlyOver, is it ok just use popup.Content?
                _mouseContainer = _popup.Content;
                if (_mouseContainer != null)
                {
                    _mouseContainer.MouseExited += this.OnMouseLeave;
                }
            }

            _textView.LostAggregateFocus += this.OnViewFocusLost;
            _popup.Content.LostFocus += this.OnContentLostFocus;
            _popup.Content.GotFocus += this.OnContentGotFocus;

            var content = _popup.Content as Widget;
            if (content != null)
            {
                content.BoundsChanged += this.OnContentSizeChanged;
            }

            // So we can dismiss the tooltip on window move / sizing.
            var hostWindow = MonoDevelop.Ide.IdeApp.Workbench.RootWindow;
            if (hostWindow != null) // for tests
            {
                hostWindow.ConfigureEvent += OnLocationChanged;
            }

            // Register to be notified when outlining regions are collapsed.
            if (this.OutliningManager != null)
            {
                this.OutliningManager.RegionsCollapsed += this.OnOutliningManager_RegionsCollapsed;
            }
        }

        private void UnregisterForEvents()
        {
            // For tooltips with style DismissOnMouseLeave
            if ((_style & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) != 0)
            {
                _textView.VisualElement.MotionNotifyEvent -= this.OnMouseMove;
                if (_mouseContainer != null)
                {
                    _mouseContainer.MouseExited -= this.OnMouseLeave;
                }
            }

            _textView.LostAggregateFocus -= this.OnViewFocusLost;
            _popup.Content.LostFocus -= this.OnContentLostFocus;
            _popup.Content.GotFocus -= this.OnContentGotFocus;

            var content = _popup.Content as Xwt.Widget;
            if (content != null)
            {
                content.BoundsChanged -= this.OnContentSizeChanged;
            }

            var hostWindow = MonoDevelop.Ide.IdeApp.Workbench.RootWindow;
            if (hostWindow != null) // for tests
            {
                hostWindow.ConfigureEvent -= OnLocationChanged;
            }

            if (this.OutliningManager != null)
            {
                this.OutliningManager.RegionsCollapsed -= this.OnOutliningManager_RegionsCollapsed;
            }
        }

        private IOutliningManager OutliningManager
        {
            get
            {
                if ((_textView.TextViewModel != null) && (_textView.ComponentContext.OutliningManagerService != null))
                {
                    return _textView.ComponentContext.OutliningManagerService.GetOutliningManager(_textView);
                }

                return null;
            }
        }

        #region Event handlers
        void OnMouseMove(object sender, Gtk.MotionNotifyEventArgs e)
        {
            if (_popup.IsVisible)
            {
                Point mousePt = new Point(e.Event.X, e.Event.Y); //TODO: Check if we have to move to screen cordinate system

                if (this.ShouldClearToolTipOnMouseMove(mousePt))
                {
                    _manager.RemoveAgent(this);
                }
            }
        }

        void OnMouseLeave(object sender, EventArgs e)
        {
            //TODO: This method, well whole MouseLeave logic is much simplefied in our case
            //We just support on Popup mouse leave while WPF supports also DismissOnMouseLeaveText
            if (_mouseContainer != null)
            {
                _mouseContainer.MouseExited -= this.OnMouseLeave;
                _mouseContainer = null;
            }

            Widget newContainer = null;//e.MouseDevice.Target;
            bool shouldRemoveAgent = false;

            // First, check to see if the mouse left the view entirely.
            if ((newContainer == null) || !_textView.IsMouseOverViewOrAdornments)
            {
                //Mouse moved over a non-WPF element or a WPF element not associated with the view.
                //Remove the agent.
                shouldRemoveAgent = true;
            }
            else if ((_style & PopupStyles.DismissOnMouseLeaveText) != 0)
            {
                // The mouse left the element over which it was originally positioned.  This may or
                // may not mean that the mouse left the span of text to which the popup is bound.
                _textView.VisualElement.GetPointer (out int x, out int y);
                if (this.ShouldClearToolTipOnMouseMove(new Point(x,y)))
                {
                    shouldRemoveAgent = true;
                }
            }

            // If we determined that we should remove the popup agent for any reason, do so.
            // Otherwise, re-subscribe to MouseLeave events on the new mouse-over target.
            if (shouldRemoveAgent)
            {
                _manager.RemoveAgent(this);
            }
            else
            {
                _mouseContainer = newContainer;
                _mouseContainer.MouseExited += this.OnMouseLeave;
            }
        }

        void OnContentLostFocus(object sender, EventArgs e)
        {
            EventHandler lostFocus = this.LostFocus;
            if (lostFocus != null)
                lostFocus(sender, e);
        }

        void OnContentGotFocus(object sender, EventArgs e)
        {
            EventHandler gotFocus = this.GotFocus;
            if (gotFocus != null)
                gotFocus(sender, e);
        }

        void OnContentSizeChanged(object sender, EventArgs e)
        {
            _textView.QueueSpaceReservationStackRefresh();
        }

        /// <summary>
        /// Handle focus change events for TextView and tooltip popup window.  If they both lose
        /// focus, then dismiss the tooltip.
        /// </summary>
        void OnViewFocusLost(object sender, EventArgs e)
        {
            if (_popup.IsVisible)
            {
                _manager.RemoveAgent(this);
            }
        }

        /// <summary>
        /// Handle the LocationChanged event for the window hosting the view and dismiss the
        /// tooltip.
        /// </summary>
        void OnLocationChanged(object sender, EventArgs e)
        {
            _manager.RemoveAgent(this);
        }

        void OnOutliningManager_RegionsCollapsed(object sender, RegionsCollapsedEventArgs e)
        {
            if (_popup.IsVisible)
            {
                foreach (ICollapsed collapsed in e.CollapsedRegions)
                {
                    if (_visualSpan.TextBuffer != collapsed.Extent.TextBuffer)
                        continue;

                    ITextSnapshot snapshot = _visualSpan.TextBuffer.CurrentSnapshot;
                    SnapshotSpan visualSnapSpan = _visualSpan.GetSpan(snapshot);
                    SnapshotSpan collapsedSnapSpan = collapsed.Extent.GetSpan(snapshot);
                    if (visualSnapSpan.IntersectsWith(collapsedSnapSpan))
                    {
                        _manager.RemoveAgent(this);
                    }
                }
            }
        }

        #endregion

        internal bool ShouldClearToolTipOnMouseMove(Point mousePt)
        {
            _textView.VisualElement.GetPointer(out int x, out int y);
            var topLeft = _textView.VisualElement.GetScreenCoordinates(new Gdk.Point(0, 0));
            var bottomRight = _textView.VisualElement.GetScreenCoordinates(new Gdk.Point(_textView.VisualElement.WidthRequest, _textView.VisualElement.HeightRequest));
            //TODO: Test if this is correct
            if (!new Rect(topLeft.ToXwtPoint(), bottomRight.ToXwtPoint()).Contains(x, y))
                return true;

            return this.InnerShouldClearToolTipOnMouseMove(mousePt);
        }

        internal bool InnerShouldClearToolTipOnMouseMove(Point mousePt)
        {
            if ((mousePt.X >= 0.0) && (mousePt.X < _textView.ViewportWidth) &&
                (mousePt.Y >= 0.0) && (mousePt.Y < _textView.ViewportHeight))
            {
                ITextViewLine line = _textView.TextViewLines.GetTextViewLineContainingYCoordinate(mousePt.Y + _textView.ViewportTop);
                if (line != null)
                {
                    SnapshotSpan span = _visualSpan.GetSpan(_textView.TextSnapshot);
                    if (span.IntersectsWith(line.ExtentIncludingLineBreak))
                    {
                        double x = mousePt.X + _textView.ViewportLeft;

                        //The mouse could be over text in the line ... see if it is over the tip
                        //This code essentially duplicates the logic in WpfTextView with respect
                        //to determining the logical position of the hover event.
                        int? position = line.GetBufferPositionFromXCoordinate(x, true);
                        if ((!position.HasValue) && (line.LineBreakLength == 0) && line.IsLastTextViewLineForSnapshotLine)
                        {
                            //For purposes of clearing tips, pretend the last line in the buffer
                            //actually is padded by the EndOfLineWidth (even though it is not).
                            if ((line.TextRight <= x) && (x < line.TextRight + line.EndOfLineWidth))
                            {
                                //position is at the end of the buffer. Return true if the span
                                //doesn't extend to the end of the buffer.
                                return (span.End < _textView.TextSnapshot.Length);
                            }
                        }

                        if (position.HasValue)
                        {
                            //A span that ends at the end of the buffer contains a position at the end of the buffer.
                            return !span.Contains(position.Value);
                        }
                    }
                }
            }

            return true;
        }

        internal Point GetScreenPointFromTextXY(double x, double y)
        {
            return _textView.VisualElement.GetScreenCoordinates(new Gdk.Point((int)(x - _textView.ViewportLeft), (int)(y - _textView.ViewportTop))).ToXwtPoint();
        }

        #region Static positioning helpers
        internal static Rect GetLocation(PopupStyles style, Size desiredSize, Rect spanRectInScreenCoordinates, Rect reservedRect, Rect screenRect)
        {
            if ((style & PopupStyles.PositionLeftOrRight) != 0)
            {
                //Position left or right
                double xPosition = ((style & PopupStyles.PreferLeftOrTopPosition) != 0)
                                   ? (reservedRect.Left - desiredSize.Width)                    //To the left
                                   : reservedRect.Right;                                        //To the right

                double yPosition = ((style & PopupStyles.RightOrBottomJustify) != 0)
                                   ? (spanRectInScreenCoordinates.Bottom - desiredSize.Height)  //Bottom justified
                                   : spanRectInScreenCoordinates.Top;                           //Top justified

                return ShiftVerticallyToFitScreen(xPosition, yPosition,
                                                  desiredSize, screenRect);
            }
            else
            {
                //Position above or below.
                double xPosition = ((style & PopupStyles.RightOrBottomJustify) != 0)
                                   ? (spanRectInScreenCoordinates.Right - desiredSize.Width)    //Right justified  
                                   : spanRectInScreenCoordinates.Left;                          //Left justified

                double yPosition = ((style & PopupStyles.PreferLeftOrTopPosition) != 0)
                                   ? (reservedRect.Top - desiredSize.Height)                    //Above
                                   : reservedRect.Bottom;                                       //Below

                return ShiftHorizontallyToFitScreen(xPosition, yPosition,
                                                    desiredSize, screenRect);
            }
        }

        internal static Rect ShiftHorizontallyToFitScreen(double x, double y, Size desiredSize, Rect screenRect)
        {
            if ((x + desiredSize.Width) > screenRect.Right)
                x = screenRect.Right - desiredSize.Width;

            if (x < screenRect.Left)
                x = screenRect.Left;

            return new Rect(x, y, desiredSize.Width, desiredSize.Height);
        }

        internal static Rect ShiftVerticallyToFitScreen(double x, double y, Size desiredSize, Rect screenRect)
        {
            if ((y + desiredSize.Height) > screenRect.Bottom)
                y = screenRect.Bottom - desiredSize.Height;

            if (y < screenRect.Top)
                y = screenRect.Top;

            return new Rect(x, y, desiredSize.Width, desiredSize.Height);
        }

        internal static bool DisjointWithPadding(Geometry reserved, Rect location)
        {
            double left = location.Left + 0.1;
            double top = location.Top + 0.1;
            double width = location.Right - 0.1 - left;
            double height = location.Bottom - 0.1 - top;

            if ((width > 0.0) && (height > 0.0))
            {
                Geometry insetLocation = new RectangleGeometry(new Rect(left, top, width, height));
                return !reserved.Bounds.IntersectsWith(insetLocation.Bounds);//TODO: This was simpliefied
            }
            else
                return true;
        }

        internal static bool ContainsWithPadding(Rect outer, Rect inner)
        {
            return (outer.Left - 0.01 <= inner.Left) && (inner.Right <= outer.Right + 0.01) &&
                   (outer.Top - 0.01 <= inner.Top) && (inner.Bottom <= outer.Bottom + 0.01);
        }
        #endregion

        internal abstract class PopupOrWindowContainer
        {
            private Widget _content;
			protected Gtk.Container _placementTarget;

			public static PopupOrWindowContainer Create(Widget content, Gtk.Container placementTarget)
            {
                return new PopUpContainer(content, placementTarget);
            }

			public PopupOrWindowContainer(Widget content, Gtk.Container placementTarget)
            {
                _content = content;
                _placementTarget = placementTarget;
            }

            public abstract bool IsVisible { get; }

            public Widget Content { get { return _content; } }

            public abstract bool IsKeyboardFocusWithin { get; }

            public abstract void DisplayAt(Point point);
            public abstract void Hide();
            public abstract Size Size { get; }
        }

		internal class PopUpContainer : PopupOrWindowContainer
        {
#if WINDOWS
            private class NoTopmostPopup : XwtThemedPopup
            {
                protected override void OnShown ()
                {
                    WpfHelper.SetNoTopmost(this.Child);
                    base.OnShown ();
                }
            }

        public static void SetNoTopmost(Visual visual)
        {
            if (visual != null)
            {
                HwndSource source = PresentationSource.FromVisual(visual) as HwndSource;
                if (source != null)
                {
                    const int SWP_NOMOVE = 0x02;
                    const int SWP_NOSIZE = 0x01;
                    const int SWP_NOACTIVATE = 0x10;
                    const int HWND_NOTOPMOST = -2;
                    NativeMethods.SetWindowPos(source.Handle, (IntPtr)HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
        }
			internal XwtThemedPopup _popup = new NoTopmostPopup ();
#else
			internal XwtThemedPopup _popup = new XwtThemedPopup();
#endif

            // WPF popup doesn't detach its child from the visual tree when the popup is not open, 
            // even if we assign Child property to null. That prevents reusing of the popup content.
            // And assiging popup's Child to null when the popup is still open is expensive
            // as it requires full subtree traversal to update relevant properties, see 
            // FrameworkElement.ChangeLogicalParent() sources.
            // The solution is to have a neutral control as the popup's child and attach the real content
            // to that control instead of popup itself. Then we can safely (and more effectively)
            // detach popup content when popup has been closed and so significantly speed up
            // popup closing.
            FrameBox _popupContentContainer = new FrameBox();

			public PopUpContainer(Widget content, Gtk.Container placementTarget)
                : base(content, placementTarget)
            {
                WindowTransparencyDecorator.Attach(_popup);//TODO: not sure we want this on all popus?
                _popup.Content = _popupContentContainer;
                _popup.Hidden += OnPopupClosed;
            }

            private void OnPopupClosed(object sender, EventArgs e)
            {
                _popupContentContainer.Content = null;
            }

            public override void DisplayAt(Point point)
            {
                //The horizontal and verical offsets are specified in terms of device pixels
                //so convert logical pixel position in point to device pixels.
                //_popup.HorizontalOffset = point.X * WpfHelper.DeviceScaleX;
                //_popup.VerticalOffset = point.Y * WpfHelper.DeviceScaleY;
                _popup.Location = point;

                if (base.Content != _popupContentContainer.Content)
                {
                    if (base.Content.Parent == null)
                    {
                        _popupContentContainer.Content = base.Content;
                        _popup.Show();
                    }
                    else
                    {
                        //The intended content still has a parent. Clear out the old content and hope things are better next time around.
                        _popupContentContainer.Content = null;
                    }
                }
            }

            public override void Hide()
            {
                _popup.Hide();
            }

            public override bool IsVisible
            {
                get
                {
                    return _popupContentContainer.Content != null;
                }
            }

            public override Size Size
            {
                get
                {
                    return ((IWidgetSurface)base.Content).GetPreferredSize();
                }
            }

            public override bool IsKeyboardFocusWithin
            {
                get
                {
                    return _popup.HasFocus;
                }
            }
        }
    }
}