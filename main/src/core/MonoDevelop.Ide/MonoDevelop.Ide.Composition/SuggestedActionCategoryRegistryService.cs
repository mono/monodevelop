using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Language.Intellisense;

namespace MonoDevelop.Ide.Composition
{
	[Export(typeof(ISuggestedActionCategoryRegistryService))]
	class SuggestedActionCategoryRegistryService : ISuggestedActionCategoryRegistryService
	{
		public IEnumerable<ISuggestedActionCategory> Categories => throw new NotImplementedException ();

		public ISuggestedActionCategorySet Any => throw new NotImplementedException ();

		public ISuggestedActionCategorySet AllCodeFixes => throw new NotImplementedException ();

		public ISuggestedActionCategorySet AllRefactorings => throw new NotImplementedException ();

		public ISuggestedActionCategorySet AllCodeFixesAndRefactorings => throw new NotImplementedException ();

		public ISuggestedActionCategorySet CreateSuggestedActionCategorySet (IEnumerable<string> categories)
		{
			throw new NotImplementedException ();
		}

		public ISuggestedActionCategorySet CreateSuggestedActionCategorySet (params string[] categories)
		{
			throw new NotImplementedException ();
		}

		public ISuggestedActionCategory GetCategory (string categoryName)
		{
			throw new NotImplementedException ();
		}
	}
}
