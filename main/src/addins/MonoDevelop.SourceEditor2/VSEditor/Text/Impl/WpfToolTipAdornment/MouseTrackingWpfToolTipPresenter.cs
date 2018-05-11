using Xwt.Backends;
namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
	using Xwt;
	using Rect = Xwt.Rectangle;
	using System.Windows.Input;
	using MonoDevelop.Components;

	internal sealed class MouseTrackingWpfToolTipPresenter : BaseWpfToolTipPresenter
    {
        // The tooltip can cause flickering issues if shown on the pixel row immediately below text. This
        // offset is used to move all tooltips down in order to eliminate the flicker.
        private const int ToolTipVerticalOffset = 1;
		private object mouseContainer;

        public MouseTrackingWpfToolTipPresenter(
            IViewElementFactoryService viewElementFactoryService,
            IObscuringTipManager obscuringTipManager,
            ITextView textView,
            ToolTipParameters parameters,
            ToolTipPresenterStyle presenterStyle)
            : base(viewElementFactoryService, obscuringTipManager, textView, parameters, presenterStyle)
        {
        }

        public override void Dismiss()
        {
            if (this.mouseContainer != null)
            {
				if (this.mouseContainer is Xwt.Widget xwtWidget) {
					xwtWidget.MouseExited -= this.OnMouseLeaveContainer;
					xwtWidget.MouseMoved -= this.OnMouseMoveContainer;
				} else if (this.mouseContainer is Gtk.Widget gtkWidget) {
					gtkWidget.LeaveNotifyEvent -= this.OnMouseLeaveContainer;
					gtkWidget.MotionNotifyEvent -= this.OnMouseMoveContainer;
				}
                this.mouseContainer = null;
            }

            base.Dismiss();
        }

        public override void StartOrUpdate(ITrackingSpan applicableToSpan, IEnumerable<object> content)
        {
            Debug.Assert(this.parameters.TrackMouse,
               "This tooltip presenter is only valid when TrackMouse is true.");

            this.applicableToSpan = applicableToSpan;

            if (DismissIfMouseOutsideOfVisualSpan())
            {
                this.Dismiss();
                return;
            }

            if (!this.popup.Visible)
            {
                this.SubscribeToContainerUnderMouse();

                Rect? presentationSpanRect = this.GetViewSpanRect(this.PresentationSpan);
                if (!presentationSpanRect.HasValue)
                {
                    this.Dismiss();
                    return;
                }

				this.popup.Location = Xwt.Desktop.MouseLocation;
            }

            base.StartOrUpdate(applicableToSpan, content);
        }

        private void SubscribeToContainerUnderMouse()
        {
			if (!this.isDismissed
				&& (this.mouseContainer == null)
				&& (((IMdTextView)textView).VisualElement.IsMouseOver () || (popup.IsMouseOver () && popup.Content != null))) {
				if (popup.IsMouseOver ()) {
					mouseContainer = popup.Content;
				} else {
					mouseContainer = ((IMdTextView)textView).VisualElement;
				}
				if (this.mouseContainer is Xwt.Widget xwtWidget) {
					xwtWidget.MouseExited += this.OnMouseLeaveContainer;
					xwtWidget.MouseMoved += this.OnMouseMoveContainer;
				} else if (this.mouseContainer is Gtk.Widget gtkWidget) {
					gtkWidget.LeaveNotifyEvent += this.OnMouseLeaveContainer;
					gtkWidget.MotionNotifyEvent += this.OnMouseMoveContainer;
				}
			}
        }

        private void UnsubscribeFromMouseContainer()
        {
            if (this.mouseContainer != null)
            {
				if (this.mouseContainer is Xwt.Widget xwtWidget) {
					xwtWidget.MouseExited -= this.OnMouseLeaveContainer;
					xwtWidget.MouseMoved -= this.OnMouseMoveContainer;
				} else if (this.mouseContainer is Gtk.Widget gtkWidget) {
					gtkWidget.LeaveNotifyEvent -= this.OnMouseLeaveContainer;
					gtkWidget.MotionNotifyEvent -= this.OnMouseMoveContainer;
				}
                this.mouseContainer = null;
            }
        }

        private void OnMouseLeaveContainer(object sender, EventArgs e)
        {
            Debug.Assert(sender == this.mouseContainer);

            UnsubscribeFromMouseContainer();
            this.SubscribeToContainerUnderMouse();

            if (this.mouseContainer == null)
            {
                this.Dismiss();
            }
            else
            {
                DismissIfMouseOutsideOfVisualSpan();
            }
        }

        private void OnMouseMoveContainer(object sender, EventArgs e)
            => this.DismissIfMouseOutsideOfVisualSpan();

        private bool DismissIfMouseOutsideOfVisualSpan()
        {
            var wpfTextView = this.WpfTextView;

            if (wpfTextView == null || this.ShouldClearToolTipOnMouseMove())
            {
                this.Dismiss();
                return true;
            }

            return false;
        }

        private Point GetMousePosRelativeToView()
        {
            var view = WpfTextView;
            if (view == null)
            {
                throw new InvalidOperationException("Can't determine the relative position of the mouse without a WpfTextView.");
            }

            Point mousePoint;
            //if (e != null)
            //{
            //    mousePoint = e.GetPosition(view.VisualElement);
            //}
            //else
            //{
                mousePoint = Mouse.GetPosition(view.VisualElement);
            //}

            mousePoint.X += view.ViewportLeft;
            mousePoint.Y += view.ViewportTop;

            return mousePoint;
        }

        //private CustomPopupPlacement[] PlacePopup(Size popupSize, Size targetSize, Point offset)
        //{
        //    Debug.Assert(!this.isDismissed);

        //    if (WpfTextView.VisualElement == null || PresentationSource.FromVisual(WpfTextView.VisualElement) == null)
        //    {
        //        return new CustomPopupPlacement[] { };
        //    }

        //    double zoom = this.WpfTextView.ZoomLevel / 100.0;

        //    // We need to provide a placement point relative to the top left corner of the popup placement rectangle.
        //    // X would be the distance from mouse point to the left side of the placement rectangle so that the popup
        //    // is positioned horizontally at mouse position.
        //    Point mouseRelativeToView = GetMousePosRelativeToView();
        //    Point popupTargetOrigin = new Point(mouseRelativeToView.X - this.WpfTextView.ViewportLeft - this.WpfTextView.VisualElement.PointFromScreen(this.popup.PlacementRectangle.BottomLeft).X, 0);
        //    Matrix transformToDevice = PresentationSource.FromVisual(WpfTextView.VisualElement).CompositionTarget.TransformToDevice;
        //    popupTargetOrigin = transformToDevice.Transform(popupTargetOrigin);
        //    popupTargetOrigin.X *= zoom;

        //    // Y would be the height of the placement rectangle - so the popup is right below it
        //    popupTargetOrigin.Y = targetSize.Height;

        //    return new CustomPopupPlacement[] { new CustomPopupPlacement(popupTargetOrigin, PopupPrimaryAxis.Horizontal) };
        //}

        private bool ShouldClearToolTipOnMouseMove()
        {
            if (this.popup != null)
            {
                if (this.popup.IsMouseOver() || this.parameters.KeepOpen)
                {
                    return false;
                }
                else if (this.WpfTextView != null && !this.WpfTextView.VisualElement.IsMouseOver())
                {
                    // The mouse is not over any interactive content and not over the text view either
                    return true;
                }
            }

            return ShouldClearToolTipOnMouseMove(GetMousePosRelativeToView());
        }

        private bool ShouldClearToolTipOnMouseMove(Point pointRelativeToView)
        {
            var view = WpfTextView;
            if (view == null || view.TextViewLines == null)
            {
                return true;
            }

            // If the point isn't over a valid line, dismiss.
            ITextViewLine line = view.TextViewLines.GetTextViewLineContainingYCoordinate(pointRelativeToView.Y);
            if (line == null)
            {
                return true;
            }

            if (this.PresentationSpan == null)

            {
                return false;
            }

            SnapshotSpan span = this.PresentationSpan.GetSpan(view.TextSnapshot);
            SnapshotPoint? position = line.GetBufferPositionFromXCoordinate(pointRelativeToView.X, true);

            //Special case handling for the last line on the buffer: we want to treat the mouse hovering near the end
            //of the line as a "hit"
            if ((!position.HasValue) && (line.End == line.Snapshot.Length))
            {
                if ((pointRelativeToView.X >= line.TextLeft) && (pointRelativeToView.X < line.TextRight + line.EndOfLineWidth))
                {
                    position = line.End;
                }
            }

            // If the position is valid and within the start/end of the visual span, inclusive, we
            // don't need to dismiss.
            if (position.HasValue && (span.Contains(position.Value) || span.End == position.Value))
            {
                return false;
            }

            // No match, dismiss the tooltip.
            return true;
        }

        private Rect? GetViewSpanRect(ITrackingSpan viewSpan)
        {
            var view = WpfTextView;
            if (view == null || view.TextViewLines == null || view.IsClosed)
            {
                return null;
            }

            SnapshotSpan visualSpan = viewSpan.GetSpan(view.TextSnapshot);

            Rect? spanRectangle = null;
            if (visualSpan.Length > 0)
            {
                double left = double.MaxValue;
                double top = double.MaxValue;
                double right = double.MinValue;
                double bottom = double.MinValue;

                var bounds = view.TextViewLines.GetNormalizedTextBounds(visualSpan);
                foreach (var bound in bounds)
                {
                    left = Math.Min(left, bound.Left);
                    top = Math.Min(top, bound.TextTop);
                    right = Math.Max(right, bound.Right);
                    bottom = Math.Max(bottom, bound.TextBottom + ToolTipVerticalOffset);
                }

                // If the start of the span lies within the view, use that instead of the left-bound of the span as a whole.
                // This will cause popups to be left-aligned with the start of their span, if at all possible.
                var startLine = view.TextViewLines.GetTextViewLineContainingBufferPosition(visualSpan.Start);
                if (startLine != null)
                {
                    var startPointBounds = startLine.GetExtendedCharacterBounds(visualSpan.Start);
                    if ((startPointBounds.Left < right) &&
                        (startPointBounds.Left >= view.ViewportLeft) &&
                        (startPointBounds.Left < view.ViewportRight))
                    {
                        left = startPointBounds.Left;
                    }
                }

                //Special case handling for when the end of the span is at the start of a line.
                ITextViewLine line = view.TextViewLines.GetTextViewLineContainingBufferPosition(visualSpan.End);
                if ((line != null) && (line.Start == visualSpan.End))
                {
                    bottom = Math.Max(bottom, line.TextBottom + ToolTipVerticalOffset);
                }

                if (left < right)
                {
                    spanRectangle = new Rect(left, top, right - left, bottom - top);
                }
            }
            else
            {
                // visualSpan is zero length so the default MarkerGeometry will be null. Create a custom marker geometry based on
                // the location.
                ITextViewLine line = view.TextViewLines.GetTextViewLineContainingBufferPosition(visualSpan.Start);
                if (line != null)
                {
                    TextBounds bounds = line.GetCharacterBounds(visualSpan.Start);
                    spanRectangle = new Rect(bounds.Left, bounds.TextTop, 0.0, bounds.TextHeight + ToolTipVerticalOffset);
                }
            }

            if (spanRectangle.HasValue && !spanRectangle.Value.IsEmpty)
            {
                //Get the portion of the span geometry that is inside the view.
                Rect viewRect = new Rect(view.ViewportLeft,
                                         view.ViewportTop,
                                         view.ViewportWidth,
                                         view.ViewportHeight);

                Rect spanRect = spanRectangle.Value;
                spanRect.Intersect(viewRect);

                Rect spanRectInScreenCoordinates =
                    new Rect(this.GetScreenPointFromTextXY(spanRect.Left, spanRect.Top),
                             this.GetScreenPointFromTextXY(spanRect.Right, spanRect.Bottom));

                return spanRectInScreenCoordinates;
            }

            return null;
        }
    }
}
