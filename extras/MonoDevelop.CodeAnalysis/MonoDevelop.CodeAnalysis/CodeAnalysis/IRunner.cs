using System;
using System.Collections.Generic;

namespace MonoDevelop.CodeAnalysis {
	
	public interface IRunner {
		string Id { get; }
		string Name { get; }
		
		IEnumerable<IViolation> Run (string inspectedFile, IEnumerable<IRule> ruleSet);
	}
}
