// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
    using Microsoft.VisualStudio.Text.Formatting;
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using System.Windows.Threading;

    /// <summary>
    /// Provides the core component of the vertical scroll bar (the scroller itself).
    /// </summary>
    internal class VerticalScrollBarMargin : ShiftClickScrollBarMargin, IVerticalScrollBar
    {
        #region Private Members

        readonly ITextView _textView;
        readonly IScrollMap _scrollMap;
        readonly PerformanceBlockMarker _performanceBlockMarker;

        double _trackTop = double.NaN;
        double _trackBottom = double.NaN;

        DispatcherTimer _updateTimer = null;

        int _scrollBarDispatchDelay;
        int _initialScrollBarDispatchDelay;
        bool _scrollShortDistanceSynchronously;

        // Amount of time (in milliseconds) that has to pass before
        // we consider a scroll event "new".
        const int _newScrollSessionThreshold = 500;

        DateTime _lastUpdateUtc;

        private void EnsureTrackTopAndBottom()
        {
            if (double.IsNaN(_trackTop))
            {
                _trackTop = 0.0;
                _trackBottom = this.ActualHeight;

                if (VisualTreeHelper.GetChildrenCount(this) == 1)
                {
                    var child = VisualTreeHelper.GetChild(this, 0);

                    Grid childAsGrid = child as Grid;
                    if (childAsGrid == null)
                    {
                        Border childAsBorder = child as Border;
                        if (childAsBorder != null)
                            childAsGrid = childAsBorder.Child as Grid;
                    }

                    if ((childAsGrid != null) && (childAsGrid.RowDefinitions.Count == 3))
                    {
                        RowDefinition trackRow = childAsGrid.RowDefinitions[1];

                        _trackTop = trackRow.Offset;
                        _trackBottom = _trackTop + trackRow.ActualHeight;
                    }
                }
            }
        }

        #endregion

        internal  VerticalScrollBarMargin(ITextView textView, IScrollMap scrollMap, string name, PerformanceBlockMarker performanceBlockMarker)
                     : base(Orientation.Vertical, name)
        {
            // Validate
            if (textView == null)
                throw new ArgumentNullException("textView");

            _textView = textView;
            _scrollMap = scrollMap;
            _performanceBlockMarker = performanceBlockMarker;

            // Setup the scrollbar
            this.Name = "WpfEditorUIVerticalScrollbar";     //For accessibility, not the name of the margin
            this.Orientation = Orientation.Vertical;
            this.SmallChange = 1.0;

            // ensure that the scroll bar always has a minimum width of 15 pixels
            // this allows other UI elements to assume a certain minimum size for
            // the scroll bar
            this.MinWidth = 15.0;

            // make sure the scrollbar appears in the center of its containing grid
            this.HorizontalAlignment = HorizontalAlignment.Center;

            this.OnOptionsChanged(null, null);
            textView.Options.OptionChanged += this.OnOptionsChanged;

            this.IsVisibleChanged += delegate(object sender, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    if (!_textView.IsClosed)
                    {
                        // Sign up for events from the text editor
                        _textView.LayoutChanged += OnEditorLayoutChanged;

                        this.SizeChanged += OnSizeChanged;
                        this.LeftShiftClick += OnLeftShiftClick;

                        //We do not need to register for ViewportHeightChanged events since those will automatically raise
                        //a layout changed event.

                        // and listen for scroll change events
                        this.Scroll += OnVerticalScrollBarScrolled;

                        //Act as if we had a layout (since one could have happened while the view was hidden).
                        if (!_textView.InLayout)
                        {
                            this.OnEditorLayoutChanged(null, null);
                        }
                    }
                }
                else
                {
                    // Unregister ourselves from events
                    _textView.LayoutChanged -= OnEditorLayoutChanged;

                    this.SizeChanged -= OnSizeChanged;
                    this.LeftShiftClick -= OnLeftShiftClick;
                    this.Scroll -= OnVerticalScrollBarScrolled;
                }
            };
            this.SetResourceReference(FrameworkElement.StyleProperty, typeof(ScrollBar));
        }

        public ITextView TextView { get { return _textView; } }

        #region Event Handlers

        void OnOptionsChanged(object sender, EventArgs e)
        {
            //Use hidden instead of collapsed here because collapsing it sets the ActualHeight to zero (messing up the track span calculation).
            this.Visibility = this.Enabled ? Visibility.Visible : Visibility.Hidden;

            // Depending on the simple graphics setting, tweak the values for how we dispatch the handling of thumb movement events
            if (!_textView.Options.IsSimpleGraphicsEnabled())
            {
                _scrollBarDispatchDelay = 15;
                _initialScrollBarDispatchDelay = 30;
                _scrollShortDistanceSynchronously = true;
            }
            else
            {
                _scrollBarDispatchDelay = 150;
                _initialScrollBarDispatchDelay = 150;
                _scrollShortDistanceSynchronously = true;
            }
        }

        /// <summary>
        /// Handle the vertical scroll bar's scroll event
        /// </summary>
        internal void OnVerticalScrollBarScrolled(object sender, ScrollEventArgs e)
        {
            if (!_textView.IsClosed)
            {
                using (CreateScrollMarker(e.ScrollEventType.ToString()))
                {
                    switch (e.ScrollEventType)
                    {
                        case ScrollEventType.LargeIncrement:
                            {
                                // Page Down
                                _textView.ViewScroller.ScrollViewportVerticallyByPixels(-_textView.ViewportHeight);
                            }
                            break;

                        case ScrollEventType.LargeDecrement:
                            {
                                // Page Up
                                _textView.ViewScroller.ScrollViewportVerticallyByPixels(_textView.ViewportHeight);
                            }
                            break;

                        case ScrollEventType.SmallIncrement:
                            {
                                _textView.ViewScroller.ScrollViewportVerticallyByPixels(-_textView.LineHeight);
                            }
                            break;

                        case ScrollEventType.SmallDecrement:
                            {
                                _textView.ViewScroller.ScrollViewportVerticallyByPixels(_textView.LineHeight);
                            }
                            break;

                        case ScrollEventType.ThumbTrack:
                            {
                                OnThumbScroll();
                            }
                            break;

                        default:
                            {
                                this.ScrollToCoordinate(e.NewValue);
                            }
                            break;
                    }
                }
            }
        }

        bool PositionIsShortDistanceFromVisibleRegion(SnapshotPoint point)
        {
            // We define a "short" distance as within a page, up or down, from the visible span.
            double pageSize = _scrollMap.ThumbSize;
            double coordinateAtStart = _scrollMap.GetCoordinateAtBufferPosition(_textView.TextViewLines.FirstVisibleLine.Start);
            double coordinateAtNewPosition = _scrollMap.GetCoordinateAtBufferPosition(point);

            return (Math.Abs(coordinateAtStart - coordinateAtNewPosition) <= pageSize);
        }

        void OnThumbScroll()
        {
            SnapshotPoint displayChar = _scrollMap.GetBufferPositionAtCoordinate(this.Value);

            if (_scrollShortDistanceSynchronously &&
                PositionIsShortDistanceFromVisibleRegion(displayChar))
            {
                ScrollToCurrentThumbPosition();
            }
            // If we don't have a dispatched update currently queued (if we had, _updateTimer would be non-null), start a new one.
            else if (_updateTimer == null)
            {
                int delay = _scrollBarDispatchDelay;

                // If we haven't received a thumb scroll in the last half second, consider this the start
                // of a new scrolling "session".
                if (_lastUpdateUtc == default(DateTime) || DateTime.UtcNow - _lastUpdateUtc > TimeSpan.FromMilliseconds(_newScrollSessionThreshold))
                {
                    delay = _initialScrollBarDispatchDelay;
                }

                _updateTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
                {
                    Interval = TimeSpan.FromMilliseconds(delay)
                };

                _updateTimer.Tick += (sender, args) =>
                    {
                        ScrollToCurrentThumbPosition();
                    };

                _updateTimer.Start();
            }
        }

        void ScrollToCurrentThumbPosition()
        {
            if (_textView == null || _textView.IsClosed)
                return;

            this.ScrollToCoordinate(this.Value);

            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer = null;
            }

            _lastUpdateUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Update the scrollbars whenever the editor's layout is changed
        /// </summary>
        public virtual void OnEditorLayoutChanged(object sender, EventArgs e)
        {
            if (!_textView.IsClosed)
            {
                double offset = 0.0;
                ITextViewLine firstLine = _textView.TextViewLines.FirstVisibleLine;
                if (firstLine.Top < _textView.ViewportTop)
                {
                    //The first line isn't fully visible so slightly offset the scrollbar value so that we'll still be able to scroll
                    //up. We're already in the middle of the line (GetCoordinate... returns a position in the middle of the line)
                    offset = 0.25;
                }

                double thumbTop = _scrollMap.GetCoordinateAtBufferPosition(firstLine.Start) + offset;
                double thumbBottom = this.GetCoordinateOfLineBottom(firstLine, _textView.TextViewLines.LastVisibleLine);

                // The WPF scrollbar factors the ViewportSize into the actual height of the scroll bar (e.g. the bottom of the scrollbar corresponds
                // to this.Maximum + this.ViewportSize), this becomes annoying if ViewportSize isn't a constant. If it isn't, then the mapping of pixel
                // position in the scroll bar to buffer position will change as ViewportSize changes.
                //
                // But -- since we're drawing landmarks in the scrollbar -- we really want the ViewportSize to correspond exactly to what is actually displayed
                // in the view. Get around this problem by:
                //  Using the real size (what is visible) for the the ViewportSize
                //  Offset the Maximum by the real size and add in the estimated thumb size.
                //
                // Net effect is that the bottom of the scroll bar is (_scrollBar.End - realThumbSize + estimatedThumbSize + realThumbSize)
                double realThumbSize = Math.Max(1.0, thumbBottom - thumbTop);

                double estimatedThumbSize = _scrollMap.ThumbSize;

                double minimum = this.Minimum = _scrollMap.Start;
                this.Maximum = Math.Max(minimum + 1.0, _scrollMap.End - realThumbSize + estimatedThumbSize);

                this.ViewportSize = realThumbSize;
                this.LargeChange = realThumbSize;

                this.Value = thumbTop;
            }
        }

        /// <summary>
        /// Get the scrollbar y coordinate of the bottom of the line.  Generally that will
        /// be the top of the next line, but if there's no next line, fake it
        /// based on the proportion of empty space below the last line.
        /// </summary>
        /// <remarks>This duplicates the similar method in src\Platform\Text\Impl\OverviewMargin\OverviewMargin.cs</remarks>
        private double GetCoordinateOfLineBottom(ITextViewLine firstLine, ITextViewLine lastLine)
        {
            if ((lastLine.EndIncludingLineBreak.Position < _textView.TextSnapshot.Length) || (_textView.ViewportHeight == 0.0))
            {
                return _scrollMap.GetCoordinateAtBufferPosition(lastLine.End);
            }
            else
            {
                // last line ... extrapolate what fraction of the view extends into "empty" space (== 1 - fraction in visible space)
                double empty = Math.Max(0.0, 1.0 - ((lastLine.Bottom - firstLine.Bottom) / _textView.ViewportHeight));
                return _scrollMap.End + Math.Floor(_scrollMap.ThumbSize * empty);
            }
        }

        void OnLeftShiftClick(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_textView.IsClosed)
            {
                using (CreateScrollMarker("VerticalScrollShiftClick"))
                {
                    //This should never return null (e.NewValue should always be in the range of the scrollbar
                    //which the scrollmap should handle).
                    this.ScrollToCoordinate(e.NewValue);
                }
            }
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_textView.IsClosed)
            {
                _trackTop = _trackBottom = double.NaN;

                EventHandler trackSpanChanged = this.TrackSpanChanged;
                if (trackSpanChanged != null)
                    trackSpanChanged(this, new EventArgs());
            }
        }

        private IDisposable CreateScrollMarker(string scrollKind)
        {
            return _performanceBlockMarker.CreateBlock("VsTextEditor.Scroll." + scrollKind);
        }

        #endregion // Event Handlers

        #region IVerticalScrollBar Members
        public IScrollMap Map { get { return _scrollMap; } }

        public double GetYCoordinateOfBufferPosition(SnapshotPoint bufferPosition)
        {
            double scrollCoordinate = _scrollMap.GetCoordinateAtBufferPosition(bufferPosition);

            return GetYCoordinateOfScrollMapPosition(scrollCoordinate);
        }

        public double GetYCoordinateOfScrollMapPosition(double scrollMapPosition)
        {
            this.EnsureTrackTopAndBottom();

            double minimum = _scrollMap.Start;
            double maximum = _scrollMap.End;
            double height = maximum - minimum;

            return _trackTop + ((scrollMapPosition - minimum) * (_trackBottom - _trackTop)) / (height + _scrollMap.ThumbSize);
        }

        public SnapshotPoint GetBufferPositionOfYCoordinate(double y)
        {
            this.EnsureTrackTopAndBottom();

            double minimum = _scrollMap.Start;
            double maximum = _scrollMap.End;
            double height = maximum - minimum;

            double scrollCoordinate = minimum + (y - _trackTop) * (height + _scrollMap.ThumbSize) / (_trackBottom - _trackTop);
            return _scrollMap.GetBufferPositionAtCoordinate(scrollCoordinate);
        }

        public double ThumbHeight
        {
            get
            {
                this.EnsureTrackTopAndBottom();

                double minimum = _scrollMap.Start;
                double maximum = _scrollMap.End;
                double height = maximum - minimum;

                return _scrollMap.ThumbSize / (height + _scrollMap.ThumbSize) * (_trackBottom - _trackTop);
            }
        }

        public double TrackSpanTop
        {
            get
            {
                this.EnsureTrackTopAndBottom();
                return _trackTop;
            }
        }

        public double TrackSpanBottom
        {
            get
            {
                this.EnsureTrackTopAndBottom();
                return _trackBottom;
            }
        }

        public double TrackSpanHeight
        {
            get
            {
                this.EnsureTrackTopAndBottom();

                return _trackBottom - _trackTop;
            }
        }

        public event EventHandler TrackSpanChanged;
        #endregion

        public virtual void ScrollToCoordinate(double coordinate)
        {
            SnapshotPoint displayChar = _scrollMap.GetBufferPositionAtCoordinate(coordinate);
            _textView.DisplayTextLineContainingBufferPosition(displayChar, 0.0, ViewRelativePosition.Top);
        }

        public override void OnDispose()
        {
            _textView.Options.OptionChanged -= this.OnOptionsChanged;
        }

        public override bool Enabled
        {
            get
            {
                this.ThrowIfDisposed();
                return _textView.Options.IsVerticalScrollBarEnabled();
            }
        }
    }
}
