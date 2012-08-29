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
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using MonoDevelop.Projects;

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
		
		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader textReader, Project project = null)
		{
			var doc = new DefaultParsedDocument (fileName);
			
			DefaultUnresolvedTypeDefinition currentFile = null;
			DefaultUnresolvedProperty currentRegion = null;
			
			string eol = Environment.NewLine;
			string content = textReader.ReadToEnd ();
			Match eolMatch = eolExpression.Match (content);
			if (eolMatch != null && eolMatch.Success)
				eol = eolMatch.Groups ["eol"].Value;
			
			string[] lines = content.Split (new string[]{eol}, StringSplitOptions.None);
			int linenum = 1;
			Match lineMatch;
			foreach (string line in lines) {
				lineMatch = fileHeaderExpression.Match (line.Trim ());
				if (lineMatch != null && lineMatch.Success) {
					if (currentFile != null) // Close out previous file region
						currentFile.BodyRegion = new DomRegion (currentFile.BodyRegion.BeginLine,
						                                        currentFile.BodyRegion.BeginColumn,
						                                        linenum - 1, int.MaxValue);
					if (currentRegion != null) // Close out previous chunk region
						currentRegion.BodyRegion = new DomRegion (currentRegion.BodyRegion.BeginLine,
						                                          currentRegion.BodyRegion.BeginColumn,
						                                          linenum - 1, int.MaxValue);
					
					// Create new file region
					currentFile = new DefaultUnresolvedTypeDefinition (string.Empty, string.Empty);
					currentFile.Region = currentFile.BodyRegion = new DomRegion (lastToken (lineMatch.Groups ["filepath"].Value), linenum, line.Length + 1, linenum, int.MaxValue);
					doc.TopLevelTypeDefinitions.Add (currentFile);
				} else {
					lineMatch = chunkExpression.Match (line);
					if (lineMatch != null && lineMatch.Success && currentFile != null) {
						if (currentRegion != null) // Close out previous chunk region
							currentRegion.BodyRegion = new DomRegion (currentRegion.BodyRegion.BeginLine,
							                                          currentRegion.BodyRegion.BeginColumn,
							                                          linenum - 1, int.MaxValue);
						
						// Create new chunk region
						currentRegion = new DefaultUnresolvedProperty (currentFile, lineMatch.Groups ["chunk"].Value);
						currentRegion.Region = currentRegion.BodyRegion = new DomRegion (currentFile.Region.FileName, linenum, line.Length + 1, linenum, int.MaxValue);
						currentFile.Members.Add (currentRegion);
					}
				}
				++linenum;
			}
			
			// Close out trailing regions
			if (currentFile != null)
				currentFile.BodyRegion = new DomRegion (currentFile.BodyRegion.BeginLine,
				                                        currentFile.BodyRegion.BeginColumn, 
				                                        Math.Max (1, linenum - 2), int.MaxValue);
			if (currentRegion != null)
				currentRegion.BodyRegion = new DomRegion (currentRegion.BodyRegion.BeginLine,
				                                          currentRegion.BodyRegion.BeginColumn, 
				                                          Math.Max (1, linenum - 2), int.MaxValue);
			
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

