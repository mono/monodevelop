namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;

    internal sealed class SpanTrackingWpfToolTipPresenter : BaseWpfToolTipPresenter
    {
        public SpanTrackingWpfToolTipPresenter(
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
            if (!this.isDismissed)
            {
                this.popup.LayoutUpdated -= this.OnLayoutUpdated;
                this.textView.LayoutChanged -= this.OnTextViewLayoutChanged;
                this.WpfTextView.Caret.PositionChanged -= this.OnCaretPositionChanged;
                this.WpfTextView.ZoomLevelChanged -= this.OnZoomLevelChanged;
                this.WpfTextView.VisualElement.LostKeyboardFocus -= this.OnLostKeyboardFocus;
            }

            base.Dismiss();
        }

        public override void StartOrUpdate(ITrackingSpan applicableToSpan, IEnumerable<object> content)
        {
            this.applicableToSpan = applicableToSpan;
            this.popup.Placement = PlacementMode.RelativePoint;
            this.popup.PlacementTarget = this.WpfTextView.VisualElement;
            this.popup.HorizontalAlignment = HorizontalAlignment.Left;
            this.popup.VerticalAlignment = VerticalAlignment.Top;

            if (!this.popup.IsOpen)
            {
                this.popup.LayoutUpdated += this.OnLayoutUpdated;
                this.textView.LayoutChanged += this.OnTextViewLayoutChanged;
                this.WpfTextView.Caret.PositionChanged += this.OnCaretPositionChanged;
                this.WpfTextView.ZoomLevelChanged += this.OnZoomLevelChanged;
                this.WpfTextView.VisualElement.LostKeyboardFocus += this.OnLostKeyboardFocus;
            }

            base.StartOrUpdate(applicableToSpan, content);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => this.Dismiss();

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e) => this.Dismiss();

        // Refreshes and async addition of controls like those used by Roslyn cause the tooltip
        // to change size. Recompute position when this happens.
        private void OnLayoutUpdated(object sender, EventArgs e) => this.UpdatePopupPosition();

        private void OnZoomLevelChanged(object sender, ZoomLevelChangedEventArgs e) => this.UpdatePopupPosition();

        private void OnTextViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.NewViewState.EditSnapshot == e.OldViewState.EditSnapshot || this.parameters.IgnoreBufferChange)
            {
                this.UpdatePopupPosition();
            }
            else
            {
                this.Dismiss();
            }
        }

        private void UpdatePopupPosition()
        {
            var startPoint = this.PresentationSpan.GetStartPoint(this.textView.TextSnapshot);
            var endPoint = this.PresentationSpan.GetEndPoint(this.textView.TextSnapshot);

            if (!this.textView.IsClosed
                && this.textView.TextViewLines.FormattedSpan.Contains(endPoint))
            {
                var startLine = this.WpfTextView.GetTextViewLineContainingBufferPosition(startPoint);
                if (startLine != null)
                {
                    double verticalOffset;

                    var endLine = this.WpfTextView.GetTextViewLineContainingBufferPosition(endPoint);
                    if (endLine != null)
                    {
                        verticalOffset = (endLine.Bottom - this.textView.ViewportTop);
                    }
                    else
                    {
                        verticalOffset = (startLine.Bottom - this.textView.ViewportTop);
                    }

                    // WPF seems to have a bug where if the offsets are the same, the control doesn't move, even
                    // if its relative target did. Force the popup to move by toggling the offsets.
                    this.popup.HorizontalOffset = (startLine.GetCharacterBounds(startPoint).Leading - this.textView.ViewportLeft);
                    this.popup.VerticalOffset = verticalOffset + 0.0001;
                    this.popup.VerticalOffset = verticalOffset;

                    return;
                }
            }

            this.Dismiss();
        }
    }
}
