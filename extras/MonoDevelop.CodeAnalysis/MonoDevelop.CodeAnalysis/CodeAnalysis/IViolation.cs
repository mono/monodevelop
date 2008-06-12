using System;

namespace MonoDevelop.CodeAnalysis {
	
	public interface IViolation {
		IRule Rule { get; }
		
		Severity Severity { get; }
		Confidence Confidence { get; }
		CodeLocation Location { get; }
		
		Uri Documentation { get; }
		string Problem { get; }
		string Solution { get; }
	}
}
