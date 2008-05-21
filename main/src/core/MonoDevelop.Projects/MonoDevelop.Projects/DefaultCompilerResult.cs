//  DefaultCompilerResult.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System.Collections;
using System.CodeDom.Compiler;
using System.Xml;

namespace MonoDevelop.Projects
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
		
		public void AddError (string file, int line, int col, string errorNum, string text)
		{
			compilerResults.Errors.Add (new CompilerError (file, line, col, errorNum, text));
			errorCount++;
		}
		
		public void AddError (string text)
		{
			compilerResults.Errors.Add (new CompilerError (null, 0, 0, null, text));
			errorCount++;
		}
		
		public void AddWarning (string file, int line, int col, string errorNum, string text)
		{
			CompilerError ce = new CompilerError (file, line, col, errorNum, text);
			ce.IsWarning = true;
			compilerResults.Errors.Add (ce);
			errorCount++;
		}
		
		public void AddWarning (string text)
		{
			CompilerError ce = new CompilerError (null, 0, 0, null, text);
			ce.IsWarning = true;
			compilerResults.Errors.Add (ce);
			errorCount++;
		}
		
		public void Append (ICompilerResult res)
		{
			compilerResults.Errors.AddRange (res.CompilerResults.Errors);
			warningCount += res.WarningCount;
			errorCount += res.ErrorCount;
			buildCount += res.BuildCount;
			if (!string.IsNullOrEmpty (res.CompilerOutput))
				compilerOutput += "\n" + res.CompilerOutput;
		}
	}
}
