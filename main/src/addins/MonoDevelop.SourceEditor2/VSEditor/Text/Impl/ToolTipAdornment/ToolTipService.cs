namespace Microsoft.VisualStudio.Text.AdornmentLibrary.ToolTip.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Adornments;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(IToolTipService))]
    internal sealed class ToolTipService : IToolTipService
    {
        private readonly IEnumerable<Lazy<IToolTipPresenterFactory, IOrderable>> unorderedPresenterProviders;
        private readonly IGuardedOperations guardedOperations;
        private readonly JoinableTaskContext joinableTaskContext;

        // Lazily initialized.
        private IEnumerable<Lazy<IToolTipPresenterFactory, IOrderable>> orderedPresenterProviders;

        [ImportingConstructor]
        public ToolTipService(
            [ImportMany]IEnumerable<Lazy<IToolTipPresenterFactory, IOrderable>> unorderedPresenterProviders,
            IGuardedOperations guardedOperations,
            JoinableTaskContext joinableTaskContext)
        {
            this.unorderedPresenterProviders = unorderedPresenterProviders
                ?? throw new ArgumentNullException(nameof(unorderedPresenterProviders));
            this.guardedOperations = guardedOperations
                ?? throw new ArgumentNullException(nameof(guardedOperations));
            this.joinableTaskContext = joinableTaskContext
                ?? throw new ArgumentNullException(nameof(joinableTaskContext));
        }

        private IEnumerable<Lazy<IToolTipPresenterFactory, IOrderable>> OrderedPresenterProviders
            => this.orderedPresenterProviders ?? (this.orderedPresenterProviders = Orderer.Order(this.unorderedPresenterProviders));

        public IToolTipPresenter CreatePresenter(ITextView textView, ToolTipParameters parameters)
        {
            if (!this.joinableTaskContext.IsOnMainThread)
            {
                throw new InvalidOperationException("Must be called from UI thread");
            }

            foreach (var provider in this.OrderedPresenterProviders)
            {
                var presenter = this.guardedOperations.CallExtensionPoint(
                    () => provider.Value.Create(textView, parameters ?? ToolTipParameters.Default),
                    valueOnThrow: null);

                if (presenter != null)
                {
                    // Wrap in a presenter that wraps all calls in a guarded operation.
                    return new GuardedToolTipPresenter(this.guardedOperations, presenter);
                }
            }

            throw new InvalidOperationException($"No applicable {nameof(IToolTipPresenterFactory)}");
        }
    }
}
