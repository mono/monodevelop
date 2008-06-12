using System;

namespace MonoDevelop.CodeAnalysis {
	
	public interface IRule {
		string Id { get; }
		
		string Name { get; }
		string Description { get; }
	}
}
