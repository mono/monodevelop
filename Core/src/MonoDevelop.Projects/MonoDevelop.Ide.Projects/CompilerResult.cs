//
// CompilerResult.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.CodeDom.Compiler;

namespace MonoDevelop.Ide.Projects
{
	public class CompilerResult
	{
		CompilerResults compilerResults = new CompilerResults (null);
		string output;
		
		public CompilerResults CompilerResults {
			get { return compilerResults; }
		}
		
		public string Output {
			get { return output; }
			set { output = value; }
		}
		
		public int Warnings {
			get {
				if (compilerResults == null) 
					return 0;
				int result = 0;
				foreach (CompilerError err in compilerResults.Errors) {
					if (err.IsWarning) 
						result++;
				}
				return result;
			}
		}
		
		public int Errors {
			get {
				if (compilerResults == null) 
					return 0;
				int result = 0;
				foreach (CompilerError err in compilerResults.Errors) {
					if (!err.IsWarning) 
						result++;
				}
				return result;
			}
		}

		public CompilerResult ()
		{
		}
		public CompilerResult (CompilerResults compilerResults, string output)
		{
			this.compilerResults = compilerResults;
			this.output          = output;
		}
		
		public void AddError (string text)
		{
			AddError (null, 0, 0, null, text);
		}
		public void AddError (string file, int line, int col, string errorNum, string text)
		{
			compilerResults.Errors.Add (new CompilerError (file, line, col, errorNum, text));
		}
		
		public void AddWarning (string text)
		{
			AddWarning (null, 0, 0, null, text);
		}
		public void AddWarning (string file, int line, int col, string errorNum, string text)
		{
			CompilerError ce = new CompilerError (file, line, col, errorNum, text);
			ce.IsWarning = true;
			compilerResults.Errors.Add (ce);
		}
	}
}
