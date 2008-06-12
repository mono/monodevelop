using System;

namespace MonoDevelop.CodeAnalysis {
	
	/// <summary>
	/// Attribute that specifies type implementing IAnalyzer for a plugin assembly.
	/// </summary>
	[AttributeUsage (AttributeTargets.Assembly)]
	public sealed class AssemblyAnalyzerAttribute : Attribute {
		private Type type;
		
		public AssemblyAnalyzerAttribute (Type Type)
		{
			if (Type.GetInterface (typeof (IAnalyzer).FullName) == null)
				throw new ArgumentException ("AssemblyAnalyzer can only point to types implementing IAnalyzer interface.");

			type = Type;
		}

		public Type Type {
			get { return type; }
		}
	}
}
