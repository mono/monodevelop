//
// MSBuildTargetResult.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Text;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	[Serializable]
	public class MSBuildTargetResult
	{
		public MSBuildTargetResult (
			string projectFile, bool isWarning, string subcategory, string code, string file,
			int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber,
			string message, string helpKeyword)
		{
			ProjectFile = projectFile;
			IsWarning = isWarning;
			Subcategory = subcategory;
			Code = code;
			File = file;
			LineNumber = lineNumber;
			ColumnNumber = columnNumber;
			EndLineNumber = endLineNumber;
			EndColumnNumber = endColumnNumber;
			Message = message;
			HelpKeyword = helpKeyword;
		}

		public string ProjectFile { get; set; }
		public bool IsWarning { get; set; }
		public string Subcategory { get; set; }
		public string Code { get; set; }
		public string File { get; set; }
		public int LineNumber { get; set; }
		public int ColumnNumber { get; set; }
		public int EndLineNumber { get; set; }
		public int EndColumnNumber { get; set; }
		public string Message { get; set; }
		public string HelpKeyword { get; set; }

		public override string ToString ()
		{
			var sb = new StringBuilder ();
			if (!string.IsNullOrEmpty (File)) {
				sb.Append (File);
				if (LineNumber > 0) {
					//(line)
					sb.Append ("(");
					sb.Append (LineNumber);
					if (ColumnNumber > 0) {
						//(line,col)
						sb.Append (",");
						sb.Append (ColumnNumber);
						if (EndColumnNumber > 0) {
							if (EndLineNumber > 0) {
								//(line,col,line,col)
								sb.Append (",");
								sb.Append (EndLineNumber);
								sb.Append (",");
								sb.Append (EndColumnNumber);
							} else {
								//(line,col-col)
								sb.Append ("-");
								sb.Append (EndColumnNumber);
							}
						}
					} else if (EndLineNumber > 0) {
						//(line-line)
						sb.Append ("-");
						sb.Append (EndLineNumber);
					}
					sb.Append (")");
				}
				sb.Append (": ");
			}
			if (!string.IsNullOrEmpty (Subcategory)) {
				sb.Append (Subcategory);
				sb.Append (" ");
			}
			sb.Append (IsWarning ? "warning" : "error");
			if (!string.IsNullOrEmpty (Code)) {
				sb.Append (" ");
				sb.Append (Code);
			}
			sb.Append (": ");
			sb.Append (Message);
			return sb.ToString ();
		}
	}
}
