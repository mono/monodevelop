using System;
using System.Collections.Generic;

namespace MonoDevelop.CodeAnalysis {
	
	public interface IRuleLoader {
		IEnumerable<IRule> GetRules ();
		IEnumerable<IRule> GetRules (Category category);
		
		IEnumerable<Category> GetCategories ();
	}
}
