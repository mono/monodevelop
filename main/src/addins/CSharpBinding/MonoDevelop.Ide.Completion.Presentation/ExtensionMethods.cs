using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Ide.Completion.Presentation
{
	public static class ExtensionMethods
	{
		/// <summary>
		/// Temporary workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/662639.
		/// The first call may fail with the ArgumentException coming from Mono due to ConditionalWeakTable
		/// reentrancy, however by that time the table will have already been populated. 
		/// Subsequent call should not hit this problem and should succeed.
		/// TODO: remove when https://github.com/dotnet/roslyn/issues/28256 is fixed.
		/// </summary>
		public static Document GetOpenDocumentInCurrentContextWithChangesSafe (this ITextSnapshot textSnapshot)
		{
			Document document = null;
			try {
				document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges ();
			} catch {
				try {
					document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges ();
				} catch {
				}
			}

			return document;
		}
	}
}