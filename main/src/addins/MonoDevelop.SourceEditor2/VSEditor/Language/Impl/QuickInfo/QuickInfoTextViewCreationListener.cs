namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System;
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ITextViewCreationListener))]
    [ContentType("any")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
    [TextViewRole(PredefinedTextViewRoles.CodeDefinitionView)]
    internal sealed class QuickInfoTextViewCreationListener : ITextViewCreationListener
    {
        private readonly IAsyncQuickInfoBroker quickInfoBroker;
        private readonly JoinableTaskContext joinableTaskContext;

        [ImportingConstructor]
        public QuickInfoTextViewCreationListener(
            IAsyncQuickInfoBroker quickInfoBroker,
            JoinableTaskContext joinableTaskContext)
        {
            this.quickInfoBroker = quickInfoBroker
                ?? throw new ArgumentNullException(nameof(quickInfoBroker));
            this.joinableTaskContext = joinableTaskContext
                ?? throw new ArgumentNullException(nameof(joinableTaskContext));
        }

        public void TextViewCreated(ITextView textView)
        {
            // No need to do anything further, this type hooks up events to the
            // text view and tracks its own life cycle.
            new QuickInfoController(
                this.quickInfoBroker,
                this.joinableTaskContext,
                textView);
        }
    }
}
