namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Utilities;

    internal sealed class GuardedToolTipPresenter : IToolTipPresenter
    {
        private readonly IGuardedOperations guardedOperations;
        private bool isDismissed = false;

        public GuardedToolTipPresenter(
            IGuardedOperations guardedOperations,
            IToolTipPresenter presenter)
        {
            this.guardedOperations = guardedOperations ?? throw new ArgumentNullException(nameof(guardedOperations));
            this.Presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));

            presenter.Dismissed += this.OnDismissed;
        }

        public event EventHandler Dismissed;

        internal IToolTipPresenter Presenter { get; }

        public void Dismiss()
        {
            if (!this.isDismissed)
            {
                this.isDismissed = true;
                this.guardedOperations.CallExtensionPoint(() =>
                {
                    this.Presenter.Dismissed -= this.Dismissed;
                    this.Presenter.Dismiss();
                });

                this.Dismissed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void StartOrUpdate(ITrackingSpan applicableToSpan, IEnumerable<object> content)
        {
            if (this.isDismissed)
            {
                throw new InvalidOperationException($"{nameof(IToolTipPresenter)} is dismissed");
            }

            this.guardedOperations.CallExtensionPoint(
                () => this.Presenter.StartOrUpdate(applicableToSpan, content));
        }

        private void OnDismissed(object sender, EventArgs e) => this.Dismiss();
    }
}
