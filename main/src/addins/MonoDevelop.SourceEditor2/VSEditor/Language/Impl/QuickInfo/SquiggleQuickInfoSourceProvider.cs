namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("squiggle")]
    [Order]
    [ContentType("any")]
    internal sealed class SquiggleQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactoryService { get; set; }

        [Import]
        internal JoinableTaskContext JoinableTaskContext { get; set; }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new SquiggleQuickInfoSource(this, textBuffer);
        }
    }
}
