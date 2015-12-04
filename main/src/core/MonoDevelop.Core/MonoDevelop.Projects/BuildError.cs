//
// BuildError.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using System.CodeDom.Compiler;

namespace MonoDevelop.Projects
{
	public class BuildError
	{
		public BuildError () : this (String.Empty, 0, 0, String.Empty, String.Empty)
		{
		}

		public BuildError (string fileName, int line, int column, string errorNumber, string errorText)
		{
			FileName = fileName;
			Line = line;
			Column = column;
			ErrorNumber = errorNumber;
			ErrorText = errorText;
		}

		public BuildError (CompilerError error)
		{
			FileName = error.FileName;
			Line = error.Line;
			Column = error.Column;
			ErrorNumber = error.ErrorNumber;
			ErrorText = error.ErrorText;
			IsWarning = error.IsWarning;
		}

		public override string ToString ()
		{
			var sb = new System.Text.StringBuilder ();
			if (!string.IsNullOrEmpty (FileName)) {
				sb.Append (FileName);
				if (Line > 1) {
					sb.Append ('(').Append (Line);
					if (Column > 1)
						sb.Append (',').Append (Column);
					sb.Append (')');
				}
				sb.Append (" : ");
			}
			if (IsWarning)
				sb.Append ("warning");
			else
				sb.Append ("error");

			if (!string.IsNullOrEmpty (ErrorNumber))
				sb.Append (' ').Append (ErrorNumber);

			sb.Append (": ").Append (ErrorText);
			return sb.ToString ();
		}

		public string FileName { get; set; }
		public string Subcategory { get; set; }

		public bool IsWarning { get; set; }

		public string ErrorNumber { get; set; }
		public string ErrorText { get; set; }
		public string HelpKeyword { get; set; }

		public int Line { get; set; }
		public int Column { get; set; }
		public int EndLine { get; set; }
		public int EndColumn { get; set; }

		public IBuildTarget SourceTarget { get; set; }

		public static BuildError FromMSBuildErrorFormat (string lineText)
		{
			var result = MSBuildErrorParser.TryParseLine (lineText);
			if (result == null)
				return null;

			return new BuildError {
				FileName    = result.Origin ?? "",
				Subcategory = result.Subcategory,
				IsWarning   = !result.IsError,
				ErrorNumber = result.Code,
				ErrorText   = result.Message,
				Line        = result.Line,
				EndLine     = result.EndLine,
				Column      = result.Column,
				EndColumn   = result.EndColumn,
				HelpKeyword = result.HelpKeyword,
			};
		}
	}
}