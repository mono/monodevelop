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
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Projects.MSBuild
{
	[MessageDataType]
	class MSBuildTargetResult
	{
		public MSBuildTargetResult ()
		{
		}

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

		[MessageDataProperty]
		public string ProjectFile { get; set; }

		[MessageDataProperty]
		public bool IsWarning { get; set; }

		[MessageDataProperty]
		public string Subcategory { get; set; }

		[MessageDataProperty]
		public string Code { get; set; }

		[MessageDataProperty]
		public string File { get; set; }

		[MessageDataProperty]
		public int LineNumber { get; set; }

		[MessageDataProperty]
		public int ColumnNumber { get; set; }

		[MessageDataProperty]
		public int EndLineNumber { get; set; }

		[MessageDataProperty]
		public int EndColumnNumber { get; set; }

		[MessageDataProperty]
		public string Message { get; set; }

		[MessageDataProperty]
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
