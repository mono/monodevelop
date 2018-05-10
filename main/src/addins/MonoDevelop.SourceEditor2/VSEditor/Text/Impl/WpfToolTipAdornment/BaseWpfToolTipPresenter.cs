namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using MonoDevelop.Components;
    using Xwt;

    internal abstract class BaseWpfToolTipPresenter : IToolTipPresenter, IObscuringTip
    {
        protected readonly IViewElementFactoryService viewElementFactoryService;
        protected readonly IObscuringTipManager obscuringTipManager;
        protected readonly ITextView textView;
        protected readonly ToolTipParameters parameters;
        protected readonly ToolTipPresenterStyle presenterStyle;

        protected readonly MonoDevelop.Components.XwtPopup popup = new MonoDevelop.Components.XwtPopup (Xwt.PopupWindow.PopupType.Tooltip);
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

        public double Opacity => this.popup.Content.Opacity;

        bool IObscuringTip.Dismiss()
        {
            bool isDismissed = this.isDismissed;
            this.Dismiss();

            return isDismissed;
        }

        public void SetOpacity(double opacity)
        {
            var child = this.popup.Content;
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
                this.popup.Visible = false;
                //this.popup.Content = null;
                this.isDismissed = true;
                this.obscuringTipManager.RemoveTip(this.textView, this);
            }

            this.Dismissed?.Invoke(this, EventArgs.Empty);
        }

        public virtual void StartOrUpdate(ITrackingSpan applicableToSpan, IEnumerable<object> content)
        {
            Debug.Assert(!this.isDismissed);

            if (!this.popup.Visible)
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
            Debug.Assert(!this.popup.Visible && !this.isDismissed);

            if (this.PresentationSpan == null)
            {
                this.Dismiss();
                return;
            }

            this.Update(content);

            this.popup.Closed += this.OnPopupClosed;
            this.popup.Visible = true;
           //todo this.popup.BringIntoView();
            this.obscuringTipManager.PushTip(this.textView, this);
        }

        public void Update(IEnumerable<object> content)
        {
            // Translate intermediate objects to UIElements.
            var contentViewElements = content.Select(
                item => this.viewElementFactoryService.CreateViewElement<Xwt.Widget>(
                    this.textView, item))
                    .Where(item => item != null);

            var vbox = new Xwt.VBox ();
            foreach (var view in contentViewElements)
            {
				vbox.PackStart (view, margin: 4);
            }
            this.popup.Content = vbox;
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

        protected IMdTextView WpfTextView => this.textView as IMdTextView;

        protected Point GetScreenPointFromTextXY(double x, double y)
        {
            var view = WpfTextView;
            Debug.Assert(view != null);

            return view.VisualElement.GetScreenCoordinates(new Gdk.Point((int)(x - view.ViewportLeft), (int)(y - view.ViewportTop))).ToXwtPoint();
        }

        private void OnPopupClosed(object sender, EventArgs e) => this.Dismiss();

        #endregion
    }
}
