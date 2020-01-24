// 
// DiffParser.cs
//  
// Author:
//       Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
// 
// Copyright (c) 2010 Levi Bard
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
using System.IO;
using System.Text.RegularExpressions;

using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Core.Text;

namespace MonoDevelop.VersionControl.Views
{
	/// <summary>
	/// Parser for unified diffs
	/// </summary>
	public class DiffParser : TypeSystemParser
	{
		// Match the original file and time/revstamp line, capturing the filepath and the stamp
		static Regex fileHeaderExpression = new Regex (@"^---\s+(?<filepath>[^\t]+)\t(?<stamp>.*)$", RegexOptions.Compiled);
		
		// Match a chunk header line
		static Regex chunkExpression = new Regex (@"^@@\s+(?<chunk>-\d+,\d+\s+\+\d+,\d+)\s+@@", RegexOptions.Compiled);
		
		// Capture the file's EOL string
		static Regex eolExpression = new Regex (@"(?<eol>\r\n|\n|\r)", RegexOptions.Compiled);
		
		#region AbstractParser overrides

		public override System.Threading.Tasks.Task<ParsedDocument> Parse (ParseOptions parseOptions, System.Threading.CancellationToken cancellationToken)
		{
			ParsedDocument doc = new DefaultParsedDocument (parseOptions.FileName);
			return System.Threading.Tasks.Task.FromResult (doc);
		}
		
		#endregion
		
		// Return the last token from a filepath 
		// (delimiter may not match Path.DirectorySeparatorChar)
		static string lastToken (string filepath)
		{
			if (!string.IsNullOrEmpty (filepath)) {
				string[] tokens = filepath.Split (new char[]{'\\','/'}, StringSplitOptions.RemoveEmptyEntries);
				return tokens[tokens.Length-1];
			}
			
			return string.Empty;
		}
	}
}

