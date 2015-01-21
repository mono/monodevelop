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
		List<BuildError> errors = new List<BuildError> ();
		IBuildTarget sourceTarget;
		
		public BuildResult()
		{
		}
		
		public BuildResult (string compilerOutput, int buildCount, int failedBuildCount)
		{
			CompilerOutput = compilerOutput;
			this.buildCount = buildCount;
			FailedBuildCount = failedBuildCount;
		}
		
		public BuildResult (CompilerResults compilerResults, string compilerOutput)
		{
			CompilerOutput = compilerOutput;
			
			if (compilerResults != null) {
				foreach (CompilerError err in compilerResults.Errors) {
					Append (new BuildError (err));
				}
			}
		}
		
		public ReadOnlyCollection<BuildError> Errors {
			get { return errors.AsReadOnly (); }
		}
		
		public void ClearErrors ()
		{
			errors.Clear ();
			warningCount = errorCount = 0;
			buildCount = 1;
			FailedBuildCount = 0;
			CompilerOutput = "";
			sourceTarget = null;
		}
		
		public void AddError (string file, int line, int col, string errorNum, string text)
		{
			Append (new BuildError (file, line, col, errorNum, text));
		}
		
		public void AddError (string text)
		{
			Append (new BuildError (null, 0, 0, null, text));
		}
		
		public void AddError (string text, string file)
		{
			Append (new BuildError (file, 0, 0, null, text));
		}
		
		public void AddWarning (string file, int line, int col, string errorNum, string text)
		{
			var ce = new BuildError (file, line, col, errorNum, text);
			ce.IsWarning = true;
			Append (ce);
		}
		
		public void AddWarning (string text)
		{
			AddWarning (text, null);
		}
		
		public void AddWarning (string text, string file)
		{
			AddWarning (file, 0, 0, null, text);
		}
		
		public BuildResult Append (BuildResult res)
		{
			if (res == null)
				return this;
			errors.AddRange (res.Errors);
			warningCount += res.WarningCount;
			errorCount += res.ErrorCount;
			buildCount += res.BuildCount;
			FailedBuildCount += res.FailedBuildCount;
			if (!string.IsNullOrEmpty (res.CompilerOutput))
				CompilerOutput += "\n" + res.CompilerOutput;
			return this;
		}
		
		public BuildResult Append (IEnumerable<BuildResult> results)
		{
			foreach (var res in results)
				Append (res);
			return this;
		}
		
		public BuildResult Append (BuildError error)
		{
			if (error == null)
				return this;
			errors.Add (error);
			if (sourceTarget != null && error.SourceTarget == null)
				error.SourceTarget = sourceTarget;
			if (error.IsWarning)
				warningCount++;
			else {
				errorCount++;
				if (FailedBuildCount == 0)
					FailedBuildCount = 1;
			}
			return this;
		}

		public string CompilerOutput { get; set; }
		
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
		
		public int FailedBuildCount { get; set; }
		
		public bool Failed {
			get { return FailedBuildCount > 0; }
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
}