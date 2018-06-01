namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;

	// TODO: re-enable this as part of
	// https://devdiv.visualstudio.com/DevDiv/Xamarin%20VS%20for%20Mac/_workitems/edit/617427
	// [Export(typeof(IAsyncQuickInfoSourceProvider))]
	[Name ("squiggle")]
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
