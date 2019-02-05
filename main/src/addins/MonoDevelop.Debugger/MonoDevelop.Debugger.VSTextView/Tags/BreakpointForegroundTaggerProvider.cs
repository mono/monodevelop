using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (IViewTaggerProvider))]
	[TagType (typeof (IClassificationTag))]
	[ContentType ("text")]
	class BreakpointForegroundTaggerProvider : IViewTaggerProvider
	{
		private readonly ClassificationTag tag;
		private readonly ClassificationTag disabled;
		private readonly ClassificationTag invalid;

		[ImportingConstructor]
		public BreakpointForegroundTaggerProvider (IClassificationTypeRegistryService classificationTypeRegistryService)
		{
			tag = new ClassificationTag (classificationTypeRegistryService.GetClassificationType (ClassificationTypes.BreakpointForegroundTypeName));
			disabled = new ClassificationTag (classificationTypeRegistryService.GetClassificationType (ClassificationTypes.BreakpointDisabledForegroundTypeName));
			invalid = new ClassificationTag (classificationTypeRegistryService.GetClassificationType (ClassificationTypes.BreakpointInvalidForegroundTypeName));
		}

		public ITagger<T> CreateTagger<T> (ITextView textView, ITextBuffer buffer) where T : ITag
		{
			return new BreakpointForegroundTagger (tag, disabled, invalid, textView, BreakpointManagerService.GetBreakpointManager(textView)) as ITagger<T>;
		}
	}
}
