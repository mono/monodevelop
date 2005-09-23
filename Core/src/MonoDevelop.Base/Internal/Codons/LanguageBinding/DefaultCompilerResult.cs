// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Collections;
using System.CodeDom.Compiler;
using System.Xml;

using MonoDevelop.Internal.Templates;
using MonoDevelop.Gui;

namespace MonoDevelop.Internal.Project
{
	/// <summary>
	/// Default implementation of the ICompilerResult interface, this implementation
	/// should be sufficient for most language bindings.
	/// </summary>
	public class DefaultCompilerResult : ICompilerResult
	{
		CompilerResults compilerResults;
		string compilerOutput;
		
		int warningCount;
		int errorCount;
		int buildCount = 1;
		int failedBuildCount;
		
		public DefaultCompilerResult ()
		{
			compilerResults = new CompilerResults (null);
			compilerOutput = "";
		}
		
		public DefaultCompilerResult (CompilerResults compilerResults, string compilerOutput)
		{
			this.compilerResults = compilerResults;
			this.compilerOutput = compilerOutput;
			
			if (compilerResults != null) {
				foreach (CompilerError err in compilerResults.Errors) {
					if (err.IsWarning) warningCount++;
					else errorCount++;
				}
				if (errorCount > 0) failedBuildCount = 1;
			}
		}
		
		public DefaultCompilerResult (CompilerResults compilerResults, string compilerOutput, int buildCount, int failedBuildCount)
		: this (compilerResults, compilerOutput)
		{
			this.buildCount = buildCount;
			this.failedBuildCount = failedBuildCount;
		}
		
		public CompilerResults CompilerResults {
			get { return compilerResults; }
		}
		
		public string CompilerOutput {
			get { return compilerOutput; }
		}
		
		public int WarningCount {
			get { return warningCount; }
		}
		
		public int ErrorCount {
			get { return errorCount; }
		}
		
		public int BuildCount {
			get { return buildCount; }
			set { buildCount = value; }
		}
		
		public int FailedBuildCount {
			get { return failedBuildCount; }
			set { failedBuildCount = value; }
		}
	}
}
