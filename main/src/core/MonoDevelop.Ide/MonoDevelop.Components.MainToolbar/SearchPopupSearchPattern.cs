//
// SearchPopupSearchPattern.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Collections.Generic;
using Gtk;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using System.Text.RegularExpressions;

namespace MonoDevelop.Components.MainToolbar
{
	public readonly struct SearchPopupSearchPattern
	{
		public readonly string Tag;
		public readonly string Pattern;
		public readonly int    LineNumber;
		public readonly int    Column;
		public readonly string UnparsedPattern;

		public bool HasLineNumber {
			get {
				return LineNumber >= 0;
			}
		}
		public bool HasColumn {
			get {
				return Column >= 0;
			}
		}

		public SearchPopupSearchPattern (string tag, string pattern, int lineNumber = -1, int column = -1, string unparsedPattern = "")
		{
			Tag = tag;
			Pattern = pattern;
			LineNumber = lineNumber;
			Column = column;
			UnparsedPattern = unparsedPattern;
		}

		static readonly Regex githubRegex = new Regex ("((?<tag>.*):)?(?<pattern>[^#]+)#L(?<line>\\d+)$", RegexOptions.Compiled); // for example: ExceptionCaughtDialog.cs#L510
		static readonly Regex lineRegex = new Regex ("((?<tag>[^:]*\\S)\\s*:)?\\s*(?<pattern>[^0-:][^:\\s]*)\\s*:[a-zA-Z]*\\s*(?<line>\\d+)$", RegexOptions.Compiled); // for example: ExceptionCaughtDialog.cs:line 510

		public static SearchPopupSearchPattern ParsePattern (string searchPattern)
		{
			var githubMatch = githubRegex.Match (searchPattern);
			if (githubMatch.Success) {
				int parsedLine;
				try {
					parsedLine = int.Parse (githubMatch.Groups ["line"].Value);
				} catch (Exception) {
					parsedLine = -1;
				}
				return new SearchPopupSearchPattern (githubMatch.Groups["tag"].Success ? githubMatch.Groups["tag"].Value : null, 
				                                     githubMatch.Groups["pattern"].Success ? githubMatch.Groups["pattern"].Value : null,
				                                     parsedLine,
				                                     -1, 
				                                     searchPattern);

			}


			var lineRegexMatch = lineRegex.Match (searchPattern);
			if (lineRegexMatch.Success) {
				int parsedLine;
				try {
					parsedLine = int.Parse (lineRegexMatch.Groups ["line"].Value);
				} catch (Exception) {
					parsedLine = -1;
				}
				return new SearchPopupSearchPattern (lineRegexMatch.Groups["tag"].Success ? lineRegexMatch.Groups["tag"].Value : null, 
				                                     lineRegexMatch.Groups["pattern"].Value,
				                                     parsedLine,
				                                     -1, 
				                                     searchPattern);

			}

			string tag = null;
			string pattern = null;
			int lineNumber = -1;
			int column = -1;

			const int maxTag = 4;
			string[] parts = new string[maxTag];
			int foundTags = 0;
			int idx = 0;
			for (int i = 0; i < searchPattern.Length; i++) {
				if (searchPattern[i] == ':') {
					parts[foundTags++] = searchPattern.Substring (idx, i - idx);
					idx = i + 1;
					if (foundTags >= maxTag)
						break;
				}
			}
			if (foundTags < maxTag)
				parts[foundTags++] = searchPattern.Substring (idx,searchPattern.Length - idx);
			switch (foundTags) {
			case 1:
				pattern = parts [0];
				break;
			case 2:
				if (!string.IsNullOrEmpty (parts[1]) && TryParseLineColumn (parts[1], ref lineNumber, ref column)) {
					if (!string.IsNullOrEmpty (parts [0]))
						pattern = parts [0];
				} else {
					tag = parts [0];
					pattern = parts [1];
				}
				break;
			case 3:
				if (IsNumber (parts [1]) && IsNumber (parts [2])) {
					if (!string.IsNullOrEmpty (parts [0]))
						pattern = parts [0];
					if (!TryParseLineColumn (parts [1] + "," + parts[2], ref lineNumber, ref column))
						lineNumber = 0;
				} else if (IsNumber (parts [1]) && string.IsNullOrEmpty (parts [2])) {
					if (!string.IsNullOrEmpty (parts [0]))
						pattern = parts [0];
					if (!TryParseLineColumn (parts [1] + ",0", ref lineNumber, ref column))
						lineNumber = 0;
				} else {
					tag = parts [0];
					pattern = parts [1] ?? "";
					if (!TryParseLineColumn (parts [2], ref lineNumber, ref column))
						lineNumber = 0;
				}
				break;
			case 4:
				tag = parts [0];
				pattern = parts [1];
				if (!TryParseLineColumn (parts [2] +","+parts[3], ref lineNumber, ref column))
					lineNumber = 0;
				break;
			}
			return new SearchPopupSearchPattern (tag, pattern?.Trim (), lineNumber, column, searchPattern);
		}

		static bool TryParseLineColumn (string str, ref int lineNumber, ref int columnNumber)
		{
			int idx = str.IndexOf (',');
			string line = str;
			string col = null;

			if (idx >= 0) {
				line = str.Substring (0, idx).Trim ();
				col = str.Substring (idx + 1).Trim ();
			}

			try {
				lineNumber = string.IsNullOrEmpty (line) ? 0 : int.Parse (line);
			} catch {
				return false;
			}

			try {
				if (col != null)
					columnNumber = string.IsNullOrEmpty (col) ? 0 : int.Parse (col);
			} catch {
				return false;
			}
			return true;
		}

		static bool IsNumber (string text)
		{
			try {
				int.Parse (text);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		public override int GetHashCode ()
		{
			return (Tag != null ? Tag.GetHashCode () : 0) ^ (Pattern != null ? Pattern.GetHashCode () : 0) ^ LineNumber.GetHashCode () ^ Column.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			if (!(obj is SearchPopupSearchPattern))
				return false;
			var other = (SearchPopupSearchPattern)obj;
			return Tag == other.Tag && Pattern == other.Pattern && LineNumber == other.LineNumber && Column == other.Column;
		}

		public static bool operator ==(SearchPopupSearchPattern l, SearchPopupSearchPattern r)
		{
			return l.Equals (r);
		}

		public static bool operator !=(SearchPopupSearchPattern l, SearchPopupSearchPattern r)
		{
			return !(l == r);
		}

		static string FormatString (string pattern)
		{
			if (pattern == null)
				return "<null>";
			return '"' + pattern + '"';
		}

		public override string ToString ()
		{
			return string.Format ("[SearchPopupSearchPattern: Tag={0}, Pattern={1}, LineNumber={2}, Column={3}]", FormatString(Tag), FormatString(Pattern), LineNumber, Column);
		}
	}
}