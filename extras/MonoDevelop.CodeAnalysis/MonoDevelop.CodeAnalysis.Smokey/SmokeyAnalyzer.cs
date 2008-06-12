using MonoDevelop.CodeAnalysis;
using MonoDevelop.CodeAnalysis.Smokey;

[assembly:AssemblyAnalyzer (typeof (SmokeyAnalyzer))]

namespace MonoDevelop.CodeAnalysis.Smokey {
	public class SmokeyAnalyzer : IAnalyzer {
		SmokeyRunner runner;
		SmokeyRuleLoader loader;
		
		public IRunner GetRunner ()
		{
			if (runner == null)
				runner = new SmokeyRunner ();
			
			return runner;
		}

		public IRuleLoader GetRuleLoader ()
		{
			if (loader == null)
				loader = new SmokeyRuleLoader ();
			
			return loader;
		}
	}
}