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
			// TODO: remove this as described in
			// https://devdiv.visualstudio.com/DevDiv/Xamarin%20VS%20for%20Mac/_workitems/edit/617427
			if (textView.TextBuffer.ContentType.IsOfType ("CSharp")) {
				return;
			}

			// No need to do anything further, this type hooks up events to the
			// text view and tracks its own life cycle.
			new QuickInfoController(
                this.quickInfoBroker,
                this.joinableTaskContext,
                textView);
        }
    }
}
