// BuildResult.cs
//
// Author:
//   Gonzalo Paniagua Javier (gonzalo@ximian.com)
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MonoDevelop.Projects
{
	public class BuildResult
	{
		int warningCount;
		int errorCount;
		int buildCount = 1;
		int failedBuildCount;
		string compilerOutput;
		List<BuildError> errors = new List<BuildError> ();
		IBuildTarget sourceTarget;
		
		public BuildResult()
		{
		}
		
		public BuildResult (string compilerOutput, int buildCount, int failedBuildCount)
		{
			this.compilerOutput = compilerOutput;
			this.buildCount = buildCount;
			this.failedBuildCount = failedBuildCount;
		}
		
		public BuildResult (CompilerResults compilerResults, string compilerOutput)
		{
			this.compilerOutput = compilerOutput;
			
			if (compilerResults != null) {
				foreach (CompilerError err in compilerResults.Errors) {
					Append (new BuildError (err));
				}
				if (errorCount > 0) failedBuildCount = 1;
			}
		}
		
		public ReadOnlyCollection<BuildError> Errors {
			get { return errors.AsReadOnly (); }
		}
		
		public void AddError (string file, int line, int col, string errorNum, string text)
		{
			Append (new BuildError (file, line, col, errorNum, text));
		}
		
		public void AddError (string text)
		{
			Append (new BuildError (null, 0, 0, null, text));
		}
		
		public void AddWarning (string file, int line, int col, string errorNum, string text)
		{
			BuildError ce = new BuildError (file, line, col, errorNum, text);
			ce.IsWarning = true;
			Append (ce);
		}
		
		public void AddWarning (string text)
		{
			BuildError ce = new BuildError (null, 0, 0, null, text);
			ce.IsWarning = true;
			Append (ce);
		}
		
		public void Append (BuildResult res)
		{
			errors.AddRange (res.Errors);
			warningCount += res.WarningCount;
			errorCount += res.ErrorCount;
			buildCount += res.BuildCount;
			failedBuildCount += res.failedBuildCount;
			if (!string.IsNullOrEmpty (res.CompilerOutput))
				compilerOutput += "\n" + res.CompilerOutput;
		}
		
		public void Append (BuildError error)
		{
			errors.Add (error);
			if (sourceTarget != null && error.SourceTarget == null)
				error.SourceTarget = sourceTarget;
			if (error.IsWarning)
				warningCount++;
			else
				errorCount++;
		}

		public string CompilerOutput {
			get { return compilerOutput; }
			set { compilerOutput = value; }
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
		
		public bool Failed {
			get { return failedBuildCount > 0; }
		}

		public IBuildTarget SourceTarget {
			get {
				return sourceTarget;
			}
			set {
				sourceTarget = value;
				if (sourceTarget != null) {
					foreach (BuildError err in Errors) {
						if (err.SourceTarget == null)
							err.SourceTarget = sourceTarget;
					}
				}
			}
		}
	}
	
	public class BuildError
	{
		string fileName;
		int line;
		int column;
		string errorNumber;
		string errorText;
		bool isWarning = false;
		IBuildTarget sourceTarget;

		public BuildError () : this (String.Empty, 0, 0, String.Empty, String.Empty)
		{
		}

		public BuildError (string fileName, int line, int column, string errorNumber, string errorText)
		{
			this.fileName = fileName;
			this.line = line;
			this.column = column;
			this.errorNumber = errorNumber;
			this.errorText = errorText;
		}
		
		public BuildError (CompilerError error)
		{
			this.fileName = error.FileName;
			this.line = error.Line;
			this.column = error.Column;
			this.errorNumber = error.ErrorNumber;
			this.errorText = error.ErrorText;
			this.isWarning = error.IsWarning;
		}

		public override string ToString ()
		{
			string type = isWarning ? "warning" : "error";
			return String.Format (System.Globalization.CultureInfo.InvariantCulture,
					"{0}({1},{2}) : {3} {4}: {5}", fileName, line, column, type,
					errorNumber, errorText);
		}

		public int Line
		{
			get { return line; }
			set { line = value; }
		}

		public int Column
		{
			get { return column; }
			set { column = value; }
		}

		public string ErrorNumber
		{
			get { return errorNumber; }
			set { errorNumber = value; }
		}

		public string ErrorText
		{
			get { return errorText; }
			set { errorText = value; }
		}

		public bool IsWarning
		{
			get { return isWarning; }
			set { isWarning = value; }
		}

		public string FileName
		{
			get { return fileName; }
			set { fileName = value; }
		}
		
		public IBuildTarget SourceTarget {
			get { return sourceTarget; }
			set { sourceTarget = value; }
		}
	}
}