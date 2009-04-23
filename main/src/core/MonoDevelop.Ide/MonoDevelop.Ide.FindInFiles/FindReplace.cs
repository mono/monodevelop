// 
// FindReplace.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace MonoDevelop.Ide.FindInFiles
{
	public class FindReplace
	{
		Regex regex;
		
		public bool IsCanceled {
			get;
			set;
		}
		
		public bool IsRunning {
			get;
			set;
		}
		
		public int FoundMatchesCount {
			get;
			set;
		}
		
		public int SearchedFilesCount {
			get;
			set;
		}
		
		public FindReplace ()
		{
			IsCanceled = IsRunning = false;
		}
		
		public bool ValidatePattern (FilterOptions filter, string pattern)
		{
			if (filter.RegexSearch) {
				try {
					new Regex (pattern, RegexOptions.Compiled);
					return true;
				} catch (Exception) {
					return false;
				}
			}
			return true;
		}
		
		public IEnumerable<SearchResult> FindAll (Scope scope, string pattern, string replacePattern, FilterOptions filter)
		{
			if (filter.RegexSearch) {
				RegexOptions regexOptions = RegexOptions.Compiled;
				if (!filter.CaseSensitive)
					regexOptions |= RegexOptions.IgnoreCase;
				regex = new Regex (pattern, regexOptions);
			} else {
				CompilePattern (pattern, filter);
			}
			IsRunning = true;
			FoundMatchesCount = SearchedFilesCount = 0;
			try {
				foreach (FileProvider provider in scope.GetFiles (filter)) {
					if (IsCanceled)
						break;
					SearchedFilesCount++;
					foreach (SearchResult result in FindAll (provider, pattern, replacePattern, filter)) {
						if (IsCanceled)
							break;
						FoundMatchesCount++;
						yield return result;
					}
				}
			} finally {
				IsRunning = false;
			}
		}
		
		IEnumerable<SearchResult> FindAll (FileProvider provider, string pattern, string replacePattern, FilterOptions filter)
		{
			if (string.IsNullOrEmpty (pattern))
				return new SearchResult[0];
			string content;
			try {
				TextReader reader = provider.Open ();
				content = reader.ReadToEnd ();
				reader.Close ();
			} catch (Exception) {
				return new SearchResult[0];
			}
			if (filter.RegexSearch)
				return RegexSearch (provider, content, pattern, replacePattern, filter);
			return Search (provider, content, pattern, replacePattern, filter);
		}
		
		IEnumerable<SearchResult> RegexSearch (FileProvider provider, string content, string pattern, string replacePattern, FilterOptions filter)
		{
			List<SearchResult> results = new List<SearchResult> ();
			if (replacePattern == null) {
				foreach (Match match in regex.Matches (content)) {
					if (IsCanceled)
						break;
					if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt (content, match.Index, match.Length))
						results.Add (new SearchResult (provider, match.Index, match.Length)); 
				}
			} else {
				List<Match> matches = new List<Match> ();
				foreach (Match match in regex.Matches (content))
					matches.Add (match);
				provider.BeginReplace ();
				int delta = 0;
				for (int i = 0; !IsCanceled && i < matches.Count ; i++) {
					Match match = matches[i];
					if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt (content, match.Index, match.Length)) {
						string replacement = match.Result (replacePattern);
						results.Add (new SearchResult (provider, match.Index + delta, replacement.Length));
						provider.Replace (match.Index + delta, match.Length, replacement);
						delta += replacement.Length - match.Length;
					}
				}
				provider.EndReplace ();
			}
			return results;
		}
		
		int[] skip;
		int[] next;
		
		void CompilePattern (string pattern, FilterOptions filter)
		{
			if (!filter.CaseSensitive)
				pattern = pattern.ToUpper ();
			int plen = pattern.Length;
			skip = new int[(int)Char.MaxValue];
			
			for (int i = 0; i < (int)Char.MaxValue; i++) {
				skip[i] = plen;
			}
			
			for (int i = 0; i < plen; i++) 
				skip[(int)pattern[i]] = plen - i - 1;
			
			next = new int[plen + 1];
			
			for (int j = 0; j <= plen; j++) {
				int i = plen - 1;
				for (; i >= 1; i--) {
					for (int k = 1; k <= j; k++) {
						if (i - k < 0) 
							break;
						if (pattern[plen - k] != pattern[i - k]) 
							goto nexttry;
					}
					goto matched;
				nexttry:
					;
				}
			matched:
				next[j] = plen - i;
			}
		}
		
		IEnumerable<SearchResult> Search (FileProvider provider, string content, string pattern, string replacePattern, FilterOptions filter)
		{
			StringComparison comparison = filter.CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			int start = 0;
			
			List<SearchResult> results = new List<SearchResult> ();
			int plen = pattern.Length - 1;
			int i = plen; // position of last p letter in s
			int delta = 0;
			if (filter.CaseSensitive) { // code duplication is ugly, but it's
				while (i < content.Length) {
					int j = 0; // matched letter count
					while (j <= plen) {
						if (IsCanceled)
							return results;
						char ch = content[i - j];
						if (ch == pattern[plen - j]) {
							j++;
						} else {
							i += skip[(int)ch] > next[j] ? skip[(int)ch] : next[j];
							goto newi;
						}
					}
					int idx = i - plen;
					if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt (content, idx, pattern.Length)) {
						if (replacePattern != null) {
							results.Add (new SearchResult (provider, idx + delta, replacePattern.Length));
							provider.Replace (idx + delta, pattern.Length, replacePattern);
							delta += replacePattern.Length - pattern.Length;
						} else {
							results.Add (new SearchResult (provider, idx, pattern.Length));
						}
					}
					i++;
				newi:
					;
				}
			} else {
				pattern = pattern.ToUpper ();
				while (i < content.Length) {
					int j = 0; // matched letter count
					while (j <= plen) {
						if (IsCanceled)
							return results;
						char ch = Char.ToUpper (content[i - j]);
						if (ch == pattern[plen - j]) {
							j++;
						} else {
							i += skip[(int)ch] > next[j] ? skip[(int)ch] : next[j];
							goto newi;
						}
					}
					int idx = i - plen;
					if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt (content, idx, pattern.Length)) {
						if (replacePattern != null) {
							results.Add (new SearchResult (provider, idx + delta, replacePattern.Length));
							provider.Replace (idx + delta, pattern.Length, replacePattern);
							delta += replacePattern.Length - pattern.Length;
						} else {
							results.Add (new SearchResult (provider, idx, pattern.Length));
						}
					}
					i++;
				newi:
					;
				}
			}
			return results;
		}
	}
}
