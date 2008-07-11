
using System;

namespace Stetic
{
	[Serializable]
	public class CodeGenerationResult
	{
		SteticCompilationUnit[] units;
		string[] warnings;
		
		internal CodeGenerationResult (SteticCompilationUnit[] units, string[] warnings)
		{
			this.units = units;
			this.warnings = warnings;
		}
		
		public SteticCompilationUnit[] Units {
			get { return units; }
		}
		
		public string[] Warnings {
			get { return warnings; }
		}
	}
}
