using MonoDevelop.CodeAnalysis;
using MonoDevelop.CodeAnalysis.Gendarme;

[assembly:AssemblyAnalyzer (typeof (GendarmeAnalyzer))]

namespace MonoDevelop.CodeAnalysis.Gendarme {
	public class GendarmeAnalyzer : IAnalyzer {
		GendarmeRunner runner;
		GendarmeRuleLoader loader;
		
		public IRunner GetRunner ()
		{
			if (runner == null)
				runner = new GendarmeRunner ();
			
			return runner;
		}

		public IRuleLoader GetRuleLoader ()
		{
			if (loader == null)
				loader = new GendarmeRuleLoader ();
			
			return loader;
		}
	}
}