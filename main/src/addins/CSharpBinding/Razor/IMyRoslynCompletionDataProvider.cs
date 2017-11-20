using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Ide.CodeCompletion
{
	public interface IMyRoslynCompletionDataProvider
	{
		MyRoslynCompletionData CreateCompletionData (Document document, ITextSnapshot textSnapshtot, CompletionService completionService, CompletionItem item);
	}
}