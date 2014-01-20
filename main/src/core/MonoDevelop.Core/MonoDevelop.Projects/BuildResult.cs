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
using System.Text.RegularExpressions;

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
		static BuildResult success = new BuildResult ();
		
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
			}
		}

		public bool HasErrors {
			get { return ErrorCount > 0; }
		}

		public bool HasWarnings {
			get { return WarningCount > 0; }
		}

		public static BuildResult Success {
			get { return success; }
		}
		
		public ReadOnlyCollection<BuildError> Errors {
			get { return errors.AsReadOnly (); }
		}
		
		public void ClearErrors ()
		{
			errors.Clear ();
			warningCount = errorCount = 0;
			buildCount = 1;
			failedBuildCount = 0;
			compilerOutput = "";
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
			BuildError ce = new BuildError (file, line, col, errorNum, text);
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
			failedBuildCount += res.failedBuildCount;
			if (!string.IsNullOrEmpty (res.CompilerOutput))
				compilerOutput += "\n" + res.CompilerOutput;
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
				if (failedBuildCount == 0)
					failedBuildCount = 1;
			}
			return this;
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
			System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			if (!string.IsNullOrEmpty (fileName)) {
				sb.Append (fileName);
				if (line > 1) {
					sb.Append ('(').Append (line);
					if (column > 1)
						sb.Append (',').Append (column);
					sb.Append (')');
				}
				sb.Append (" : ");
			}
			if (isWarning)
				sb.Append ("warning");
			else
				sb.Append ("error");
			
			if (!string.IsNullOrEmpty (errorNumber))
				sb.Append (' ').Append (errorNumber);
			
			sb.Append (": ").Append (errorText);
			return sb.ToString ();
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
		
		//FIXME: this doesn't get hanlde the complete MSBuild error format, see
		//http://blogs.msdn.com/b/msbuild/archive/2006/11/03/msbuild-visual-studio-aware-error-messages-and-message-formats.aspx
		static Regex regexError = new Regex (
			@"^(\s*(?<file>[^\(]+)(\((?<line>\d*)(,(?<column>\d*[\+]*))?\))?:\s+)*(?<level>\w+)\s+(?<number>..\d+):\s*(?<message>.*)",
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		
		public static BuildError FromMSBuildErrorFormat (string lineText)
		{
			Match match = regexError.Match (lineText);
			if (!match.Success)
				return null;
			
			return new BuildError () {
				FileName    = match.Result ("${file}") ?? "",
				IsWarning   = match.Result ("${level}") == "warning",
				ErrorNumber = match.Result ("${number}"),
				ErrorText   = match.Result ("${message}"),
				Line        = GetLineNumber (match.Result ("${line}")),
				Column      = GetLineNumber (match.Result ("${column}")),
			};
		}
		
		static int GetLineNumber (string textValue)
		{
			if (string.IsNullOrEmpty (textValue))
				return 0;
			
			int val;
			if (Int32.TryParse (textValue, out val))
				return val;
			
			return -1;
		}
	}
}