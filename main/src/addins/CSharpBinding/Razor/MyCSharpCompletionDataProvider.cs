using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.CodeCompletion
{
	[Export (typeof (IMyRoslynCompletionDataProvider))]
	[ContentType ("CSharp")]
	public class MyCSharpCompletionDataProvider : IMyRoslynCompletionDataProvider
	{
		public MyRoslynCompletionData CreateCompletionData (Document document, ITextSnapshot textSnapshtot, CompletionService completionService, CompletionItem item)
		{
			return new MyCSharpCompletionData (document, textSnapshtot, completionService, item);
		}
	}
}