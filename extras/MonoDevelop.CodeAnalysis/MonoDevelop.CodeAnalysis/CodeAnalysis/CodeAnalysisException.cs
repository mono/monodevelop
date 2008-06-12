using System;

namespace MonoDevelop.CodeAnalysis {
	
	public class CodeAnalysisException : Exception {
		
		public CodeAnalysisException () { }
		
		public CodeAnalysisException (string message)
			: base (message) { }
		
		public CodeAnalysisException (string message, Exception inner)
			: base (message, inner) { }
	}
}
