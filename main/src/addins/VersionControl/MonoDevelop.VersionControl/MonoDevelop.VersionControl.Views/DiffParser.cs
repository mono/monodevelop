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

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.VersionControl.Views
{
	/// <summary>
	/// Parser for unified diffs
	/// </summary>
	public class DiffParser: AbstractParser
	{
		// Match the original file and time/revstamp line, capturing the filepath and the stamp
		static Regex fileHeaderExpression = new Regex (@"^---\s+(?<filepath>[^\t]+)\t(?<stamp>.*)$", RegexOptions.Compiled);
		
		// Match a chunk header line
		static Regex chunkExpression = new Regex (@"^@@\s+(?<chunk>-\d+,\d+\s+\+\d+,\d+)\s+@@", RegexOptions.Compiled);
		
		// Capture the file's EOL string
		static Regex eolExpression = new Regex (@"(?<eol>\r\n|\n|\r)", RegexOptions.Compiled);
		
		#region AbstractParser overrides
		
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			ParsedDocument doc = new ParsedDocument (fileName);
			if(null == doc.CompilationUnit)
				doc.CompilationUnit = new CompilationUnit (fileName);
			CompilationUnit cu = (CompilationUnit)doc.CompilationUnit;
			DomType currentFile = null;
			DomProperty currentRegion = null;
			
			string eol = Environment.NewLine;
			Match eolMatch = eolExpression.Match (content);
			if (eolMatch != null && eolMatch.Success)
				eol = eolMatch.Groups["eol"].Value;
			
			string[] lines = content.Split (new string[]{eol}, StringSplitOptions.None);
			int linenum = 1;
			Match lineMatch;
			foreach (string line in lines)
			{
				lineMatch = fileHeaderExpression.Match (line.Trim());
				if (lineMatch != null && lineMatch.Success) {
					if (currentFile != null) // Close out previous file region
						currentFile.BodyRegion = new DomRegion (currentFile.BodyRegion.Start.Line,
						                                        currentFile.BodyRegion.Start.Column,
						                                        linenum-1, int.MaxValue);
					if (currentRegion != null) // Close out previous chunk region
						currentRegion.BodyRegion = new DomRegion (currentRegion.BodyRegion.Start.Line,
						                                          currentRegion.BodyRegion.Start.Column,
						                                          linenum-1, int.MaxValue);
					
					// Create new file region
					currentFile = new DomType (cu, ClassType.Unknown, Modifiers.None, 
					                           lastToken (lineMatch.Groups["filepath"].Value),
					                           new DomLocation (linenum, 1), 
					                           string.Empty,
					                           new DomRegion (linenum, line.Length+1, linenum, int.MaxValue));
					cu.Add (currentFile);
				} else {
					lineMatch = chunkExpression.Match (line);
					if (lineMatch != null && lineMatch.Success && currentFile != null) {
						if (currentRegion != null) // Close out previous chunk region
							currentRegion.BodyRegion = new DomRegion (currentRegion.BodyRegion.Start.Line,
							                                          currentRegion.BodyRegion.Start.Column,
							                                          linenum-1, int.MaxValue);
						
						// Create new chunk region
						currentRegion = new DomProperty (lineMatch.Groups["chunk"].Value, Modifiers.None, 
						                                 new DomLocation (linenum, 1), 
						                                 new DomRegion (linenum, line.Length+1, linenum, int.MaxValue), null);
						currentFile.Add (currentRegion);
					}
				}
				++linenum;
			}
			
			// Close out trailing regions
			if (currentFile != null)
				currentFile.BodyRegion = new DomRegion (currentFile.BodyRegion.Start.Line,
				                                        currentFile.BodyRegion.Start.Column, 
				                                        Math.Max (1, linenum-2), int.MaxValue);
			if (currentRegion != null)
				currentRegion.BodyRegion = new DomRegion (currentRegion.BodyRegion.Start.Line,
				                                          currentRegion.BodyRegion.Start.Column, 
				                                          Math.Max (1, linenum-2), int.MaxValue);
			
			return doc;
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

