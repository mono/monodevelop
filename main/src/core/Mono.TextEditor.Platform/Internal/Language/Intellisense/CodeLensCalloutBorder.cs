// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// The CodeLensCalloutBorder decorator is used to draw a border with a callout shape and background around another element.
    /// the callout presumes it is getting used in a popup window that is placed closely relative to the popup's PlacementTarget.
    /// 
    /// It also supports a "squish" behavior, where the callout will attempt to squish its content's vertical size down to a set size to
    /// avoid the popup popping down and covering editor content.
    /// </summary>
    public sealed class CodeLensCalloutBorder : Decorator
    {
        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        public CodeLensCalloutBorder() 
            : base()
        {
            PresentationSource.AddSourceChangedHandler(this, this.SourceChangedHandler);
        }

        /// <summary>
        /// The BorderWidth property defined how thick a border to draw.  The property's value is a double value, not a thickness
        /// </summary>
        public double BorderWidth
        {
            get { return (double)GetValue(BorderWidthProperty); }
            set { SetValue(BorderWidthProperty, value); }
        }

        /// <summary>
        /// The Padding property inflates the effective size of the child by the specified thickness.  This
        /// achieves the same effect as adding margin on the child, but is present here for convenience.
        /// </summary>
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// The BorderBrush property defines the brush used to fill the border region.
        /// </summary>
        public Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// The Background property defines the brush used to fill the area within the border.
        /// </summary>
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// The CalloutSize property defines the size of the callout shape drawn at the top or bottom of the border
        /// </summary>
        public Size CalloutSize
        {
            get { return (Size)GetValue(CalloutSizeProperty); }
            set { SetValue(CalloutSizeProperty, value); }
        }

        /// <summary>
        /// the smallest size this callout will "squish" its content to to avoid the popup popping down.
        /// </summary>
        public double MinSquishHeight
        {
            get { return (double)GetValue(MinSquishHeightProperty); }
            set { SetValue(MinSquishHeightProperty, value); }
        }

        /// <summary>
        /// get or set the callout target component.  this is where the arrow (attempts) to point.
        /// </summary>
        public UIElement CalloutTarget
        {
            get { return (UIElement)GetValue(CalloutTargetProperty); }
            set { SetValue(CalloutTargetProperty, value); }
        }

        /// <summary>
        /// DependencyProperty for <see cref="BorderWidth" /> property.
        /// </summary>
        public static readonly DependencyProperty BorderWidthProperty = DependencyProperty.Register("BorderWidth", typeof(double), typeof(CodeLensCalloutBorder),
                                            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));


        /// <summary>
        /// DependencyProperty for <see cref="Padding" /> property.
        /// </summary>
        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register("Padding", typeof(Thickness), typeof(CodeLensCalloutBorder),
                                            new FrameworkPropertyMetadata(
                                                new Thickness(),
                                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
                                            new ValidateValueCallback(IsThicknessValid));


        /// <summary>
        /// DependencyProperty for <see cref="BorderBrush" /> property.
        /// </summary>
        public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register("BorderBrush", typeof(Brush), typeof(CodeLensCalloutBorder),
                                            new FrameworkPropertyMetadata(
                                                (Brush)null,
                                                FrameworkPropertyMetadataOptions.AffectsRender |
                                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty = Panel.BackgroundProperty.AddOwner(typeof(CodeLensCalloutBorder),
                        new FrameworkPropertyMetadata(
                                (Brush)null,
                                FrameworkPropertyMetadataOptions.AffectsRender |
                                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        /// <summary>
        /// DependencyProperty for <see cref="CalloutSize"/> property.
        /// </summary>
        public static readonly DependencyProperty CalloutSizeProperty = DependencyProperty.Register("CalloutSize", typeof(Size), typeof(CodeLensCalloutBorder),
                        new FrameworkPropertyMetadata(new Size(16, 16), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// DependencyProperty for <see cref="CalloutTarget"/> property.
        /// </summary>
        public static readonly DependencyProperty CalloutTargetProperty = DependencyProperty.Register("CalloutTarget", typeof(UIElement), typeof(CodeLensCalloutBorder),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnCalloutTargetChanged));

        /// <summary>
        /// DependencyProperty for <see cref="MinSquishHeight"/> property.
        /// </summary>
        public static readonly DependencyProperty MinSquishHeightProperty = DependencyProperty.Register("MinSquishHeight", typeof(double), typeof(CodeLensCalloutBorder),
                        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// given a container size, return the maxium space that could be available to a child given the callout, margins, borders, etc.
        /// </summary>
        /// <param name="maxContainerWidth"></param>
        /// <param name="maxContainerHeight"></param>
        /// <returns>the maximum size a child could be</returns>
        public Size GetMaxContentSize( double maxContainerWidth, double maxContainerHeight )
        {
            double border = this.BorderWidth * 2.0; //x2 because the border appears on each side
            Size padding = HelperCollapseThickness(this.Padding);
            Size margin = HelperCollapseThickness(this.Margin);
            Size callout = this.CalloutSize;

            return new Size(maxContainerWidth - border - padding.Width - margin.Width,
                            maxContainerHeight - border - padding.Height - margin.Height - callout.Height);
        }

        /// <summary>
        /// Updates DesiredSize of the CodeLensCalloutBorder.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// Border determines its desired size it needs from the specified border the child: its sizing
        /// properties, margin, and requested size.
        /// </remarks>
        /// <param name="constraint">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The Decorator's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            UIElement child = this.Child;
            Size mySize = new Size();

            // Compute the chrome size added by the various elements
            double doubleWidth = this.BorderWidth * 2.0; //x2 because the border appears on each side
            Size border = new Size(doubleWidth, doubleWidth);
            Size padding = HelperCollapseThickness(this.Padding);
            Size callout;
            // if there's going to be a callout, take away the extra height from the child
            var calloutElement = this.CalloutTarget as FrameworkElement;
            if (calloutElement != null && calloutElement.IsVisible && this.IsVisible)
            {
                callout = this.CalloutSize;
            }
            else
            {
                callout = new Size();
            }

            //If we have a child
            if (child != null)
            {
                // Combine into total decorating size
                Size combined = new Size(border.Width + padding.Width, border.Height + padding.Height + callout.Height);

                // Remove size of combined chrome from child's reference size.
                Size childConstraint = new Size(Math.Max(0.0, constraint.Width - combined.Width),
                                                Math.Max(0.0, constraint.Height - combined.Height));
                double minSquish = MinSquishHeight;
                double currentSquishValue = LastTargetScreenLocation.HasValue ? (LastTargetScreenLocation.Value.Y - MonitorTop - combined.Height) : 0; // also subtract the top coord for the monitor
                bool isSquished = false;

                // if squishing is turned on, and the callout target is near the top of the screen (but not TOO close to the top) use that value as the height constraint
                if (minSquish > 0 && currentSquishValue >= minSquish && currentSquishValue <= childConstraint.Height)
                {
                    childConstraint.Height = currentSquishValue;
                    isSquished = true;
                }

                child.Measure(childConstraint);
                Size childSize = child.DesiredSize;

                // if it got squished, but the child has a minimum size bigger than that, reset the constraint and remeasure
                if (isSquished && childSize.Height > constraint.Height)
                {
                    childConstraint.Height = Math.Max(0.0, constraint.Height - combined.Height);
                    child.Measure(childConstraint);
                    childSize = child.DesiredSize;
                }

                // Now use the returned size to drive our size, by adding back the margins, etc.
                mySize.Width = childSize.Width + combined.Width;
                mySize.Height = childSize.Height + combined.Height;
            }
            else
            {
                // Combine into total decorating size
                mySize = new Size(border.Width + padding.Width, border.Height + padding.Height + callout.Height);
            }

            return mySize;
        }

        /// <summary>
        /// CodeLensCalloutBorder computes the position of its single child and applies its child's alignments to the child.
        /// 
        /// </summary>
        /// <param name="finalSize">The size reserved for this element by the parent</param>
        /// <returns>The actual ink area of the element, typically the same as finalSize</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Thickness borders;
            double offset = 0;
            double thickness = this.BorderWidth;
            bool popupIsAbove = true;
            Size calloutSize = this.CalloutSize;

            var calloutElement = this.CalloutTarget as FrameworkElement;
            if (calloutElement != null && calloutElement.IsVisible && this.IsVisible)
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    // figure out where the center of the target is on the screen
                    Point targetScreenLoc = calloutElement.PointToScreen(new Point(calloutElement.ActualWidth / 2, 0));
                    Point popupScreenLoc = LastPopupScreenLocation;
                    popupIsAbove = popupScreenLoc.Y < targetScreenLoc.Y;

                    // calculate the offset of the target location and the popup's x coord
                    offset = popupScreenLoc.X < targetScreenLoc.X ? (targetScreenLoc.X - popupScreenLoc.X) : (popupScreenLoc.X - targetScreenLoc.X);
                    // that was all screen coords, needs to be converted to units for location inside the window
                    offset = source.CompositionTarget.TransformFromDevice.Transform(new Point(offset, 0)).X;
                    // account for any margin (typically to allow a drop shadow)
                    offset -= this.Margin.Left;

                    // deflate the inner rect even more to account for the callout
                    borders = new Thickness(thickness,
                                            thickness + (popupIsAbove ? 0 : CalloutSize.Height),
                                            thickness,
                                            thickness + (popupIsAbove ? CalloutSize.Height : 0));
                }
                else
                {
                    borders = new Thickness(thickness, thickness, thickness, thickness);
                    calloutSize = new Size(0, 0);
                }
            }
            else
            {
                borders = new Thickness(thickness, thickness, thickness, thickness);
                calloutSize = new Size(0, 0);
            }

            Rect boundRect = new Rect(finalSize);

            //  arrange child
            UIElement child = Child;
            if (child != null)
            {
                Rect innerRect = HelperDeflateRect(boundRect, borders);
                Rect childRect = HelperDeflateRect(innerRect, Padding);
                child.Arrange(childRect);
            }

            // now generate the geometry for the callout
            if (!IsZero(boundRect.Width) && !IsZero(boundRect.Height))
            {
                StreamGeometry borderGeometry = new StreamGeometry();

                using (StreamGeometryContext ctx = borderGeometry.Open())
                {
                    GuidelineCache = GenerateCalloutGeometry(ctx, boundRect, calloutSize, offset, popupIsAbove, thickness);
                }

                borderGeometry.Freeze();
                BorderGeometryCache = borderGeometry;
            }
            else
            {
                BorderGeometryCache = null;
                GuidelineCache = null;
            }

            return (finalSize);
        }

        /// <summary>
        /// if a callout was generated in arrange, this draws it on render
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            Brush brush = this.Background;
            StreamGeometry borderGeometry = BorderGeometryCache;
            if (borderGeometry != null && brush != null)
            {
                Brush outline = this.BorderBrush;
                GuidelineSet guidelines = GuidelineCache;
                Pen p = (outline == null || this.BorderWidth == 0) ? null : new Pen(outline, this.BorderWidth);
                bool addGuidelines = p != null && guidelines != null;

                // need to create guideline sets to make sure the border edge doesn't get anti-aliased
                if (addGuidelines)
                {
                    dc.PushGuidelineSet(GuidelineCache);
                }

                dc.DrawGeometry(brush, p, borderGeometry);

                if (addGuidelines)
                {
                    dc.Pop();
                }
            }
        }

        // the presentation source has changed, hook up to the hwnd to watch for the popup moving
        private void SourceChangedHandler(object sender, SourceChangedEventArgs e)
        {
            HwndSource newHwnd = (HwndSource)HwndSource.FromVisual(this);
            if (HwndCache != newHwnd && HwndCache != null)
            {
                HwndCache.RemoveHook(WndProcHook);
            }

            HwndCache = newHwnd;
            if (HwndCache != null)
            {
                HwndCache.AddHook(WndProcHook);
            }
            else
            {
                // going offscreen, clear the monitor info too
                MonitorTop = 0;
            }
        }

        // invalidate measure, and visual (invalidates render)
        private void Invalidate()
        {
            InvalidateMeasure();
            InvalidateVisual();
        }

        private const int WM_WINDOWPOSCHANGED = 0x0047;

        // watch for pos change messages, get the screen location, and make the callout re-arrange
        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_WINDOWPOSCHANGED)
            {
                // wParam - This parameter is not used. 
                // lParam - A pointer to a WINDOWPOS structure that contains information about the window's new size and position. 
                NativeMethods.WINDOWPOS windowPos = new NativeMethods.WINDOWPOS();
                Marshal.PtrToStructure(lParam, windowPos);
                // if the window pos has changed, invalidate.  
                if (LastPopupScreenLocation.X != windowPos.x || LastPopupScreenLocation.Y != windowPos.y)
                {
                    LastPopupScreenLocation = new Point(windowPos.x, windowPos.y);
                    Invalidate();
                }
            }

            return IntPtr.Zero;
        }

        // the callout target has changed, update hooks to watch the target move and update the callout
        private static void OnCalloutTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var content = d as CodeLensCalloutBorder;
            if (content != null)
            {
                var target = args.NewValue as UIElement;
                var old = args.OldValue as UIElement;

                if (target != null)
                {
                    target.LayoutUpdated += content.PlacementTargetLayoutChanged;
                }

                if (old != null)
                {
                    old.LayoutUpdated -= content.PlacementTargetLayoutChanged;
                }
            }
        }

        // see if the placement target's location or width has changed, we'll ignore any other layout changes
        private void PlacementTargetLayoutChanged(object sender, EventArgs e)
        {
            var element = this.CalloutTarget as FrameworkElement;
            if (element != null && element.IsVisible && this.IsVisible)
            {
                LastTargetScreenLocation = element.PointToScreen(new Point(element.ActualWidth / 2, 0));
            }
        }

        // Const values come from sdk\inc\crt\float.h
        private const double DBL_EPSILON = 2.2204460492503131e-016; /* smallest such that 1.0+DBL_EPSILON != 1.0 */

        private static bool IsThicknessValid(object value)
        {
            Thickness t = (Thickness)value;
            if (t.Left < 0d || t.Right < 0d || t.Top < 0d || t.Bottom < 0d)
            {
                return false;
            }
            if (Double.IsNaN(t.Left) || Double.IsNaN(t.Right) || Double.IsNaN(t.Top) || Double.IsNaN(t.Bottom))
            {
                return false;
            }
            if (Double.IsPositiveInfinity(t.Left) || Double.IsPositiveInfinity(t.Right) || Double.IsPositiveInfinity(t.Top) || Double.IsPositiveInfinity(t.Bottom))
            {
                return false;
            }
            if (Double.IsNegativeInfinity(t.Left) || Double.IsNegativeInfinity(t.Right) || Double.IsNegativeInfinity(t.Top) || Double.IsNegativeInfinity(t.Bottom))
            {
                return false;
            }
            return true;
        }

        private static bool IsZero(double value)
        {
            return Math.Abs(value) < 10.0 * DBL_EPSILON;
        }

        // Helper function to add up the left and right size as width, as well as the top and bottom size as height
        private static Size HelperCollapseThickness(Thickness th)
        {
            return new Size(th.Left + th.Right, th.Top + th.Bottom);
        }

        /// Helper to deflate rectangle by thickness
        private static Rect HelperDeflateRect(Rect rt, Thickness thick)
        {
            return new Rect(rt.Left + thick.Left,
                            rt.Top + thick.Top,
                            Math.Max(0.0, rt.Width - thick.Left - thick.Right),
                            Math.Max(0.0, rt.Height - thick.Top - thick.Bottom));
        }

        /// <summary>
        /// generates the geometry for the callout in the given context, and a set of guidelines to make sure the lines don't get anti-aliased
        /// </summary>
        /// <param name="ctx">already open stream geometry context</param>
        /// <param name="rect">the outer size of the entire callout border</param>
        /// <param name="calloutSize">the size of the callout itself</param>
        /// <param name="calloutOffset">the x offset for the point of the callout</param>
        /// <param name="calloutBelow">indicates that the callout is being displayed at the bottom of the border</param>
        /// <param name="thickness">thickness of the border</param>
        /// <returns>guidelines that can be used when drawing to prevent anti-alaising of the horizontal and vertical lines</returns>
        private static GuidelineSet GenerateCalloutGeometry(StreamGeometryContext ctx, Rect rect, Size calloutSize, double calloutOffset, bool calloutBelow, double thickness)
        {
            GuidelineSet guidelines = new GuidelineSet();
            //  compute the coordinates of the content square
            Point topLeft = new Point(0, calloutBelow ? 0 : calloutSize.Height);
            Point topRight = new Point(rect.Width - 1, topLeft.Y);
            Point bottomRight = new Point(topRight.X, rect.Height - (calloutBelow ? calloutSize.Height : 0) - 1);
            Point bottomLeft = new Point(topLeft.X, bottomRight.Y);
            Point[] calloutPoints;

            // figure out how many callout points there are
            double calloutHalfWidth = calloutSize.Width / 2;
            double calloutLeft = calloutOffset - calloutHalfWidth;
            double calloutRight = calloutOffset + calloutHalfWidth;

            // swap some values depending on if the callout is pointing down or up
            Point calloutLineLeft = calloutBelow ? bottomLeft : topLeft;
            double calloutHeight = calloutBelow ? calloutSize.Height : -calloutSize.Height;

            // special cases for the callout at the absolute left or right edge, the callout butts up against the edge
            if (calloutLeft <= topLeft.X)
            {
                // 2 points to make |/
                calloutPoints = new[] { 
                        new Point(calloutLineLeft.X + calloutSize.Width, calloutLineLeft.Y), // left edge of callout
                        new Point(calloutLineLeft.X, calloutLineLeft.Y + calloutHeight), // left edge of callout goes straight down instead of down+right
                    };
            }
            else if (calloutRight >= topRight.X)
            {
                Point calloutLineRight = calloutBelow ? bottomRight : topRight;

                // 2 points to make \|
                calloutPoints = new[] { 
                        new Point(calloutLineRight.X, calloutLineRight.Y + calloutHeight), // bottom center of callout
                        new Point(calloutLineRight.X - calloutSize.Width, calloutLineRight.Y), // left edge of callout
                    };
            }
            else
            {
                // 3 points to make \/
                calloutPoints = new[] { 
                        new Point(calloutLineLeft.X + calloutRight, calloutLineLeft.Y), // right edge of callout
                        new Point(calloutLineLeft.X + calloutOffset, calloutLineLeft.Y + calloutHeight), // bottom center of callout
                        new Point(calloutLineLeft.X + calloutLeft, calloutLineLeft.Y), // left edge of callout
                    };
            }

            //  add on offsets
            Vector offset = new Vector(rect.TopLeft.X, rect.TopLeft.Y);
            topLeft += offset;
            topRight += offset;
            bottomRight += offset;
            bottomLeft += offset;
            for (int i = 0; i < calloutPoints.Length; i++)
            {
                calloutPoints[i] += offset;
            }

            // add guidelines to prevent the border line from getting anti-aliased
            double halfWidth = thickness / 2;
            guidelines.GuidelinesX.Add(topLeft.X + halfWidth);
            guidelines.GuidelinesX.Add(topRight.X + halfWidth);

            if (!calloutBelow)
            {
                guidelines.GuidelinesY.Add(topLeft.Y + calloutHeight + halfWidth); // calloutheight is negative here
            }

            guidelines.GuidelinesY.Add(topLeft.Y + halfWidth);
            guidelines.GuidelinesY.Add(bottomLeft.Y + halfWidth);

            if (calloutBelow)
            {
                guidelines.GuidelinesY.Add(bottomLeft.Y + calloutHeight + halfWidth);
            }

            // starting at the top left, work clockwise around the callout
            ctx.BeginFigure(topLeft, isFilled: true, isClosed: true);

            // if the callout is at the top, it gets drawn here
            if (!calloutBelow)
            {
                // line to each callout point across the top (except in reverse order of the original array, it assumed it was at the bottom going clockwise left to right)
                for (int i = calloutPoints.Length - 1; i >= 0; i--)
                {
                    ctx.LineTo(calloutPoints[i], isStroked: true, isSmoothJoin: false);
                }
            }

            // Top line
            ctx.LineTo(topRight, isStroked: true, isSmoothJoin: false);

            // Right line
            ctx.LineTo(bottomRight, isStroked: true, isSmoothJoin: false);

            // if the callout is at the bottom, it gets drawn here
            if (calloutBelow)
            {
                // line to each callout point across the bottom
                foreach (var point in calloutPoints)
                {
                    ctx.LineTo(point, isStroked: true, isSmoothJoin: false);
                }
            }

            // bottom line
            ctx.LineTo(bottomLeft, isStroked: true, isSmoothJoin: false);

            // Left line
            ctx.LineTo(topLeft, isStroked: true, isSmoothJoin: false);

            return guidelines;
        }

        /// <summary>
        /// get the top coordinate of the current monitor.  
        /// If there are multiple monitors, the coord could be negative.  
        /// If the taskbar (or anything else) is reserving display space, the top may be non-zero
        /// </summary>
        private int MonitorTop { get; set; }

        /// <summary>
        /// updates the MonitorTopCoord based on the monitor that contains the given screen coordinate
        /// </summary>
        /// <param name="screenCoord">The screen coordinate used to figure out which monitor the callout is on</param>
        private void UpdateMonitorTop(Point? screenCoord)
        {
            if (screenCoord.HasValue)
            {
                NativeMethods.POINT point = new NativeMethods.POINT() { x = (int)screenCoord.Value.X, y = (int)screenCoord.Value.Y };
                IntPtr hMonitor = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTONEAREST);
                NativeMethods.MONITORINFO monitorInfo = new NativeMethods.MONITORINFO();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.MONITORINFO));
                NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo);

                MonitorTop = monitorInfo.rcWork.top;
            }
            else
            {
                MonitorTop = 0;
            }
        }

        private Point? lastTargetScreenLocation;

        /// <summary>
        /// the last location the target was on screen, used to decide if the callout should recompute its location
        /// </summary>
        private Point? LastTargetScreenLocation 
        {
            get { return lastTargetScreenLocation; }

            set
            {
                if (value != lastTargetScreenLocation)
                {
                    lastTargetScreenLocation = value;
                    // grab the monitor info for size calculations as well
                    UpdateMonitorTop(lastTargetScreenLocation);
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// the last location the popup was on screen, used to decide if the callout should recompute its location
        /// </summary>
        private Point LastPopupScreenLocation { get; set; }

        /// <summary>
        /// geometry drawn in OnRender
        /// </summary>
        private StreamGeometry BorderGeometryCache { get; set; }

        /// <summary>
        /// guidelines used in OnRender
        /// </summary>
        private GuidelineSet GuidelineCache { get; set; }

        /// <summary>
        /// the Hwnd of the window for the callout, whenever this moves, the callout is updated
        /// </summary>
        private HwndSource HwndCache { get; set; }
    }
}