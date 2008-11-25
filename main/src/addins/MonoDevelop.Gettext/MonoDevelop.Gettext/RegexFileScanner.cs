// RegexFileScanner.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.Gettext
{
	public class RegexFileScanner: IFileScanner
	{
		List<RegexInfo> regexes = new List<RegexInfo> ();
		List<Regex> excluded = new List<Regex> ();
		List<TransformInfo> transforms = new List<TransformInfo> ();
		IList<string> extensions;
		IList<string> mimeTypes;
		
		class RegexInfo {
			public Regex Regex;
			public int ValueGroupIndex;
			public int PluralGroupIndex;
			public StringEscaping.EscapeMode EscapeMode;
		}
		
		class TransformInfo {
			public Regex Regex;
			public string ReplaceText;
		}
		
		public RegexFileScanner (string[] extensions, string[] mimeTypes)
		{
			this.extensions = extensions != null ? extensions : new string[0];
			this.mimeTypes = mimeTypes != null ? mimeTypes : new string[0];
		}
		
		public void AddIncludeRegex (string regex, int valueGroupIndex, string regexOptions, StringEscaping.EscapeMode escapeMode)
		{
			AddIncludeRegex (regex, valueGroupIndex, -1, regexOptions, escapeMode);
		}
		
		public void AddIncludeRegex (string regex, int valueGroupIndex, int pluralGroupIndex, string regexOptions, StringEscaping.EscapeMode escapeMode)
		{
			Regex rx = new Regex (regex, ParseOptions (regexOptions));
			RegexInfo ri = new RegexInfo ();
			ri.Regex = rx;
			ri.ValueGroupIndex = valueGroupIndex;
			ri.PluralGroupIndex = pluralGroupIndex;
			ri.EscapeMode = escapeMode;
			regexes.Add (ri);
		}
		
		public void AddExcludeRegex (string regex, string regexOptions)
		{
			Regex rx = new Regex (regex, ParseOptions (regexOptions));
			excluded.Add (rx);
		}
		
		public void AddTransformRegex (string regex, string replaceText, string regexOptions)
		{
			Regex rx = new Regex (regex, ParseOptions (regexOptions));
			TransformInfo ri = new TransformInfo ();
			ri.Regex = rx;
			ri.ReplaceText = replaceText;
			transforms.Add (ri);
		}
		
		RegexOptions ParseOptions (string regexOptions)
		{
			RegexOptions retval = RegexOptions.Compiled;
			if (string.IsNullOrEmpty (regexOptions))
				return retval;
			
			foreach (string s in regexOptions.Split('|')) {
				try {
					RegexOptions option = (RegexOptions) System.Enum.Parse (typeof (RegexOptions), s);
					retval |= option;
				} catch (Exception ex) {
					LoggingService.LogError ("Unknown RegexOptions value in Gettext scanner", ex);
				}
			}
			return retval;
		}
		
		public bool CanScan (TranslationProject project, Catalog catalog, string fileName, string mimeType)
		{
			if (extensions.Count == 0 && mimeTypes.Count == 0)
				return true;
			string ext = Path.GetExtension (fileName);
			if (ext.Length > 0) ext = ext.Substring (1); // remove initial dot
			bool r = extensions.Contains (ext) || mimeTypes.Contains (mimeType);
			return r;
		}
		
		public virtual void UpdateCatalog (TranslationProject project, Catalog catalog, IProgressMonitor monitor, string fileName)
		{
			string text = File.ReadAllText (fileName);
			string relativeFileName = MonoDevelop.Core.FileService.AbsoluteToRelativePath (project.BaseDirectory, fileName);
			string fileNamePrefix   = relativeFileName + ":";
			if (String.IsNullOrEmpty (text))
				return;
			
			// Get a list of all excluded regions
			List<Match> excludeMatches = new List<Match> ();
			foreach (Regex regex in excluded) {
				foreach (Match m in regex.Matches (text))
					excludeMatches.Add (m);
			}
			
			// Sort the list by match index
			excludeMatches.Sort (delegate (Match a, Match b) {
				return a.Index.CompareTo (b.Index);
			});
			
			// Remove from the list all regions which start in an excluded region
			int pos=0;
			for (int n=0; n<excludeMatches.Count; n++) {
				Match m = excludeMatches [n];
				if (m.Index < pos) {
					excludeMatches.RemoveAt (n);
					n--;
				} else {
					pos = m.Index + m.Length;
				}
			}
			
			foreach (RegexInfo ri in regexes) {
				int lineNumber = 0;
				int oldIndex  = 0;
				foreach (Match match in ri.Regex.Matches (text)) {
					// Ignore matches inside excluded regions
					bool ignore = false;
					foreach (Match em in excludeMatches) {
						if (match.Index >= em.Index && match.Index < em.Index + em.Length) {
							ignore = true;
							LoggingService.LogDebug ("Excluded Gettext string '{0}' in file '{1}'", match.Groups[ri.ValueGroupIndex].Value, fileName);
							break;
						}
					}
					if (ignore)
						continue;
					
					string mt = match.Groups[ri.ValueGroupIndex].Value;
					if (mt.Length == 0)
						continue;
					
					foreach (TransformInfo ti in transforms)
						mt = ti.Regex.Replace (mt, ti.ReplaceText);
					
					try {
						mt = StringEscaping.UnEscape (ri.EscapeMode, mt);
					} catch (FormatException fex) {
						monitor.ReportWarning ("Error unescaping string '" + mt + "': " + fex.Message);
						continue;
					}
					
					if (mt.Trim().Length == 0)
						continue;
					
					//get the plural string if it's a plural form and apply transforms
					string pt = ri.PluralGroupIndex != -1 ? match.Groups[ri.PluralGroupIndex].Value : null;
					if (pt != null)
						foreach (TransformInfo ti in transforms)
							pt = ti.Regex.Replace (pt, ti.ReplaceText);
					
					//add to the catalog
					CatalogEntry entry = catalog.AddItem (mt, pt);
					lineNumber += GetLineCount (text, oldIndex, match.Index);
					oldIndex = match.Index;
					entry.AddReference (fileNamePrefix + lineNumber);
				}
			}
		}
		
		int GetLineCount (string text, int startIndex, int endIndex)
		{
			int result = 0;
			for (int i = startIndex; i < endIndex; i++) {
				if (text[i] == '\n')
					result++;
			}
			return result;
		}
	}
}
