using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Debugger
{
	[Export (typeof (ITaggerProvider))]
	[TagType (typeof (IClassificationTag))]
	[ContentType ("text")]
	class CurrentStatementForegroundTaggerProvider : ITaggerProvider
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

		public ITagger<T> CreateTagger<T> (ITextBuffer buffer) where T : ITag
		{
			return new CurrentStatementForegroundTagger (tag, buffer, isGreen: false) as ITagger<T>;
		}
	}
}
