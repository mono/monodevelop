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
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class BuildResult
	{
		List<BuildError> errors = new List<BuildError> ();
		IBuildTarget sourceTarget;

		public BuildResult()
			: this (null, 1, 0, null)
		{
		}

		public BuildResult (string compilerOutput, int buildCount, int failedBuildCount)
			: this (compilerOutput, buildCount, failedBuildCount, null)
		{
		}

		public BuildResult (CompilerResults compilerResults, string compilerOutput)
			: this (compilerOutput, 1, 0, compilerResults)
		{
		}

		BuildResult (string compilerOutput, int buildCount, int failedBuildCount, CompilerResults compilerResults)
		{
			CompilerOutput = compilerOutput;
			BuildCount = buildCount;
			FailedBuildCount = failedBuildCount;

			if (compilerResults != null) {
				foreach (CompilerError err in compilerResults.Errors) {
					Append (new BuildError (err));
				}
			}
		}

		public bool HasErrors {
			get { return ErrorCount > 0; }
		}

		public bool HasWarnings {
			get { return WarningCount > 0; }
		}

		public static BuildResult CreateSuccess ()
		{
			return new BuildResult ();
		}

		public static BuildResult CreateSkipped (IBuildTarget sourceTarget)
		{
			return new BuildResult {
				SkippedBuildCount = 1,
				sourceTarget = sourceTarget
			};
		}

		public static BuildResult CreateUpToDate (IBuildTarget sourceTarget)
		{
			return new BuildResult {
				UpToDateBuildCount = 1,
				sourceTarget = sourceTarget
			};
		}

		public static BuildResult CreateCancelled ()
		{
			return new BuildResult ().AddError (GettextCatalog.GetString ("Cancelled"));
		}

		public ReadOnlyCollection<BuildError> Errors {
			get { return errors.AsReadOnly (); }
		}
		
		public void ClearErrors ()
		{
			errors.Clear ();
			WarningCount = ErrorCount = 0;
			BuildCount = 1;
			FailedBuildCount = SkippedBuildCount = UpToDateBuildCount = 0;
			CompilerOutput = "";
			sourceTarget = null;
		}
		
		public BuildResult AddError (string file, int line, int col, string errorNum, string text)
		{
			Append (new BuildError (file, line, col, errorNum, text));
			return this;
		}
		
		public BuildResult AddError (string text)
		{
			Append (new BuildError (null, 0, 0, null, text));
			return this;
		}
		
		public BuildResult AddError (string text, string file)
		{
			Append (new BuildError (file, 0, 0, null, text));
			return this;
		}
		
		public BuildResult AddWarning (string file, int line, int col, string errorNum, string text)
		{
			var ce = new BuildError (file, line, col, errorNum, text) {
				IsWarning = true
			};
			Append (ce);
			return this;
		}
		
		public BuildResult AddWarning (string text)
		{
			AddWarning (text, null);
			return this;
		}
		
		public BuildResult AddWarning (string text, string file)
		{
			AddWarning (file, 0, 0, null, text);
			return this;
		}
		
		public BuildResult Append (BuildResult res)
		{
			if (res == null)
				return this;
			errors.AddRange (res.Errors);
			WarningCount += res.WarningCount;
			ErrorCount += res.ErrorCount;
			BuildCount += res.BuildCount;
			FailedBuildCount += res.FailedBuildCount;
			UpToDateBuildCount += res.UpToDateBuildCount;
			SkippedBuildCount += res.SkippedBuildCount;
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
				WarningCount++;
			else {
				ErrorCount++;
				if (FailedBuildCount == 0)
					FailedBuildCount = 1;
			}
			return this;
		}

		internal BuildResult SetSource (IBuildTarget source)
		{
			SourceTarget = source;
			return this;
		}

		public string CompilerOutput { get; set; }

		public int WarningCount { get; private set; }

		public int ErrorCount { get; private set; }

		public int BuildCount { get; set; }

		public int FailedBuildCount { get; set; }

		public int SkippedBuildCount { get; set; }

		public int UpToDateBuildCount { get; set; }

		public int SuccessfulBuildCount => BuildCount - FailedBuildCount - SkippedBuildCount - UpToDateBuildCount;
		
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