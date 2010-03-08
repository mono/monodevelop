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
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;


namespace MonoDevelop.Ide.FindInFiles
{
	public class FindReplace
	{
		Regex regex;
		
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
			IsRunning = false;
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
		
		public IEnumerable<SearchResult> FindAll (Scope scope, IProgressMonitor monitor, string pattern, string replacePattern, FilterOptions filter)
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
			
			monitor.BeginTask (scope.GetDescription (filter, pattern, replacePattern), 100);
			try {
				int totalWork = scope.GetTotalWork (filter);
				int step = Math.Max (1, totalWork / 100);
				foreach (FileProvider provider in scope.GetFiles (monitor, filter)) {
					if (monitor.IsCancelRequested)
						break;
					SearchedFilesCount++;
					if (!string.IsNullOrEmpty (replacePattern))
						provider.BeginReplace ();
					foreach (SearchResult result in FindAll (monitor, provider, pattern, replacePattern, filter)) {
						if (monitor.IsCancelRequested)
							break;
						FoundMatchesCount++;
						yield return result;
					}
					if (!string.IsNullOrEmpty (replacePattern))
						provider.EndReplace ();
					if (SearchedFilesCount % step == 0) 
						monitor.Step (1);
					DispatchService.RunPendingEvents ();
				}
			} finally {
				monitor.EndTask ();
				IsRunning = false;
			}
		}
		
		IEnumerable<SearchResult> FindAll (IProgressMonitor monitor, FileProvider provider, string pattern, string replacePattern, FilterOptions filter)
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
				return RegexSearch (monitor, provider, content, pattern, replacePattern, filter);
			return Search (provider, content, pattern, replacePattern, filter);
		}
		
		IEnumerable<SearchResult> RegexSearch (IProgressMonitor monitor, FileProvider provider, string content, string pattern, string replacePattern, FilterOptions filter)
		{
			List<SearchResult> results = new List<SearchResult> ();
			if (replacePattern == null) {
				foreach (Match match in regex.Matches (content)) {
					if (monitor.IsCancelRequested)
						break;
					if (provider.SelectionStartPosition > -1 && match.Index < provider.SelectionStartPosition)
						continue;
					if (provider.SelectionEndPosition > -1 && match.Index + match.Length > provider.SelectionEndPosition)
						continue;
					if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt(content, match.Index, match.Length))
						results.Add(new SearchResult(provider, match.Index, match.Length));
				}
			} else {
				List<Match> matches = new List<Match> ();
				foreach (Match match in regex.Matches(content))
				{
					if (provider.SelectionStartPosition > -1 && match.Index < provider.SelectionStartPosition)
						continue;
					if (provider.SelectionEndPosition > -1 && match.Index + match.Length > provider.SelectionEndPosition)
						continue;
					matches.Add(match);
				}
				if (!string.IsNullOrEmpty (replacePattern))
					provider.BeginReplace ();
				int delta = 0;
				for (int i = 0; !monitor.IsCancelRequested && i < matches.Count; i++) {
					Match match = matches[i];
					if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt (content, match.Index, match.Length)) {
						string replacement = match.Result (replacePattern);
						results.Add (new SearchResult (provider, match.Index + delta, replacement.Length));
						provider.Replace (match.Index + delta, match.Length, replacement);
						delta += replacement.Length - match.Length;
					}
				}
				if (!string.IsNullOrEmpty (replacePattern))
					provider.EndReplace ();
			}
			return results;
		}
		
		
		int[] occ;
		int[] next;
		
		public void CompilePattern (string pattern, FilterOptions filter)
		{
			if (!filter.CaseSensitive)
				pattern = pattern.ToUpper ();
			int plen = pattern.Length;
			
			occ = new int[(int)Char.MaxValue];
			int i;
			for (i = 0; i < (int)Char.MaxValue; i++) {
				occ[i] = -1;
			}
			for (i = 0; i < plen; i++) 
				occ[(int)pattern[i]] = i;
			
			int[] f = new int[plen + 1];
			next = new int[plen + 1];
			
			// Pre process part 1
			i = plen;
			int j = plen + 1;
			f[i] = j;
			while (i > 0) {
				while (j <= plen && pattern[i - 1] != pattern[j - 1]) {
					if (next[j] == 0) 
						next[j] = j-i;
					j = f[j];
				}
				i--;
				j--;
				f[i] = j;
			}
			
			// Pre process part 2
			j = f[0];
			for (i = 0; i <= plen; i++) {
				if (next[i] == 0) 
					next[i] = j;
				if (i == j) 
					j = f[j];
			}
			
		}
		
		public IEnumerable<SearchResult> Search (FileProvider provider, string content, string pattern, string replacePattern, FilterOptions filter)
		{
			if (!filter.CaseSensitive) {
				pattern = pattern.ToUpper ();
				content = content.ToUpper ();
			}

			int plen = pattern.Length - 1;
			int delta = 0;
			int i = 0, end = content.Length - pattern.Length;
			if (provider.SelectionStartPosition > -1)
				i = provider.SelectionStartPosition;
			if (provider.SelectionEndPosition > -1)
				end = provider.SelectionEndPosition - pattern.Length;
			while (i <= end) {
				int j = plen;
				while (j >= 0 && pattern[j] == content[i + j])
					j--;

				if (j < 0) {
					int idx = i;
					if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt (content, idx, pattern.Length)) {
						if (replacePattern != null) {
							yield return new SearchResult (provider, idx + delta, replacePattern.Length);
							
							provider.Replace (idx + delta, pattern.Length, replacePattern);
							delta += replacePattern.Length - pattern.Length;
						} else {
							yield return new SearchResult (provider, idx, pattern.Length);
						}
					}
					i += next[0];
				}
				else
					i += System.Math.Max (next[j + 1], j-occ[(int)content[i + j]]);
			}
		}
	}
}
