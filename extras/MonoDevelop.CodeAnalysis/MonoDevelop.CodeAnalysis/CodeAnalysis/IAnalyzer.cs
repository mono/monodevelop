namespace MonoDevelop.CodeAnalysis {
	/// <summary>
	/// Interface that provides methods for getting IRunner and IRuleLoader instances.
	/// It should be implemented for each plugin assembly.
	/// </summary>
	public interface IAnalyzer {
		IRunner GetRunner ();
		IRuleLoader GetRuleLoader ();
	}
}
