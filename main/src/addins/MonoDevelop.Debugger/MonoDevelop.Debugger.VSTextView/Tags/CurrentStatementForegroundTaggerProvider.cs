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
	[ContentType ("any")]
	class CurrentStatementForegroundTaggerProvider : IViewTaggerProvider
	{
		private readonly IClassificationTypeRegistryService classificationTypeRegistryService;
		private readonly IClassificationType classificationType;
		private readonly ClassificationTag tag;

		[ImportingConstructor]
		public CurrentStatementForegroundTaggerProvider (IClassificationTypeRegistryService classificationTypeRegistryService)
		{
			this.classificationTypeRegistryService = classificationTypeRegistryService;
			this.classificationType = classificationTypeRegistryService.GetClassificationType (ClassificationTypes.CurrentStatementForegroundTypeName);
			this.tag = new ClassificationTag (classificationType);
		}

		public ITagger<T> CreateTagger<T> (ITextView textView, ITextBuffer buffer) where T : ITag
		{
			return new CurrentStatementForegroundTagger (tag, textView, isGreen: false) as ITagger<T>;
		}
	}
}
