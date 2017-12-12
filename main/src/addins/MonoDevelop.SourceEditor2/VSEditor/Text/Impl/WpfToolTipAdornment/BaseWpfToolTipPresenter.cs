namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;

    internal abstract class BaseWpfToolTipPresenter : IToolTipPresenter, IObscuringTip
    {
        protected readonly IViewElementFactoryService viewElementFactoryService;
        protected readonly IObscuringTipManager obscuringTipManager;
        protected readonly ITextView textView;
        protected readonly ToolTipParameters parameters;
        protected readonly ToolTipPresenterStyle presenterStyle;

        protected readonly Popup popup = new Popup();
        protected ITrackingSpan applicableToSpan;
        protected bool isDismissed;

        public BaseWpfToolTipPresenter(
            IViewElementFactoryService viewElementFactoryService,
            IObscuringTipManager obscuringTipManager,
            ITextView textView,
            ToolTipParameters parameters,
            ToolTipPresenterStyle presenterStyle)
        {
            this.viewElementFactoryService = viewElementFactoryService
                ?? throw new ArgumentNullException(nameof(viewElementFactoryService));
            this.obscuringTipManager = obscuringTipManager
                ?? throw new ArgumentNullException(nameof(obscuringTipManager));
            this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.presenterStyle = presenterStyle ?? throw new ArgumentNullException(nameof(presenterStyle));
        }

        #region IObscuringTip

        public double Opacity => this.popup?.Child.Opacity ?? 1.0;

        bool IObscuringTip.Dismiss()
        {
            bool isDismissed = this.isDismissed;
            this.Dismiss();

            return isDismissed;
        }

        public void SetOpacity(double opacity)
        {
            var child = this.popup.Child;
            if (child != null)
            {
                child.Opacity = opacity;
            }
        }

        #endregion

        #region IToolTipPresenter

        public event EventHandler Dismissed;

        public virtual void Dismiss()
        {
            if (this.popup != null)
            {
                this.popup.Closed -= this.OnPopupClosed;
                this.popup.IsOpen = false;
                this.popup.Child = null;
                this.isDismissed = true;
                this.obscuringTipManager.RemoveTip(this.textView, this);
            }

            this.Dismissed?.Invoke(this, EventArgs.Empty);
        }

        public virtual void StartOrUpdate(ITrackingSpan applicableToSpan, IEnumerable<object> content)
        {
            Debug.Assert(!this.isDismissed);

            if (!this.popup.IsOpen)
            {
                this.Start(content);
            }
            else
            {
                this.Update(content);
            }
        }

        #endregion

        #region Impl

        private void Start(IEnumerable<object> content)
        {
            Debug.Assert(!this.popup.IsOpen && !this.isDismissed);

            if (this.PresentationSpan == null)
            {
                this.Dismiss();
                return;
            }

            this.popup.AllowsTransparency = true;
            this.popup.UseLayoutRounding = true;
            this.popup.SnapsToDevicePixels = true;
            TextOptions.SetTextFormattingMode(this.popup, TextFormattingMode.Display);

            this.Update(content);

            this.popup.Closed += this.OnPopupClosed;

            this.popup.IsOpen = true;
            this.popup.BringIntoView();
            this.obscuringTipManager.PushTip(this.textView, this);
        }

        public void Update(IEnumerable<object> content)
        {
            // Translate intermediate objects to UIElements.
            var contentViewElements = content.Select(
                item => this.viewElementFactoryService.CreateViewElement<UIElement>(
                    this.textView, item))
                    .Where(item => item != null);

            var control = new WpfToolTipControl(this.WpfTextView)
            {
                // Translate intermediate to UI.
                DataContext = new WpfToolTipViewModel(
                    this.parameters,
                    contentViewElements,
                    this.presenterStyle)
            };

            this.popup.Child = control;
        }

        protected ITrackingSpan PresentationSpan
        {
            get
            {
                if (this.applicableToSpan == null)
                {
                    return null;
                }

                SpanTrackingMode mode = this.applicableToSpan.TrackingMode;
                NormalizedSnapshotSpanCollection viewSpans = this.textView.BufferGraph.MapUpToBuffer(
                    this.applicableToSpan.GetSpan(this.applicableToSpan.TextBuffer.CurrentSnapshot),
                    mode,
                    this.textView.TextBuffer);
                return viewSpans.Count > 0 ? viewSpans[0].Snapshot.CreateTrackingSpan(viewSpans[0], mode) : null;
            }
        }

        protected IWpfTextView WpfTextView => this.textView as IWpfTextView;

        protected Point GetScreenPointFromTextXY(double x, double y)
        {
            var view = WpfTextView;
            Debug.Assert(view != null);

            return view.VisualElement.PointToScreen(new Point(x - view.ViewportLeft, y - view.ViewportTop));
        }

        private void OnPopupClosed(object sender, EventArgs e) => this.Dismiss();

        #endregion
    }
}
