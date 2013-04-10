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
using System.Linq;
using Gtk;

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
			}
			IsRunning = true;
			FoundMatchesCount = SearchedFilesCount = 0;
			
			monitor.BeginTask (scope.GetDescription (filter, pattern, replacePattern), 50);
			try {
				int totalWork = scope.GetTotalWork (filter);
				int step = Math.Max (1, totalWork / 50);
				string content;
				
				foreach (FileProvider provider in scope.GetFiles (monitor, filter)) {
					if (monitor.IsCancelRequested)
						yield break;
					SearchedFilesCount++;
					try {
						content = provider.ReadString ();
						if (replacePattern != null)
							provider.BeginReplace (content);
					} catch (System.IO.FileNotFoundException) {
						Application.Invoke (delegate {
							MessageService.ShowError (string.Format (GettextCatalog.GetString ("File {0} not found.")), provider.FileName);
						});
						continue;
					}
					foreach (SearchResult result in FindAll (monitor, provider, content, pattern, replacePattern, filter)) {
						if (monitor.IsCancelRequested)
							yield break;
						FoundMatchesCount++;
						yield return result;
					}
					if (replacePattern != null)
						provider.EndReplace ();
					if (SearchedFilesCount % step == 0)
						monitor.Step (1); 
				}
			} finally {
				monitor.EndTask ();
				IsRunning = false;
			}
		}
		
		IEnumerable<SearchResult> FindAll (IProgressMonitor monitor, FileProvider provider, string content, string pattern, string replacePattern, FilterOptions filter)
		{
			if (string.IsNullOrEmpty (pattern))
				return Enumerable.Empty<SearchResult> ();
			
			if (filter.RegexSearch)
				return RegexSearch (monitor, provider, content, replacePattern, filter);
			
			return Search (provider, content, pattern, replacePattern, filter);
		}
		
		IEnumerable<SearchResult> RegexSearch (IProgressMonitor monitor, FileProvider provider, string content, string replacePattern, FilterOptions filter)
		{
			var results = new List<SearchResult> ();
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
				var matches = new List<Match> ();
				foreach (Match match in regex.Matches(content))
				{
					if (provider.SelectionStartPosition > -1 && match.Index < provider.SelectionStartPosition)
						continue;
					if (provider.SelectionEndPosition > -1 && match.Index + match.Length > provider.SelectionEndPosition)
						continue;
					matches.Add(match);
				}
				provider.BeginReplace (content);
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
				provider.EndReplace ();
			}
			return results;
		}
		
		public IEnumerable<SearchResult> Search (FileProvider provider, string content, string pattern, string replacePattern, FilterOptions filter)
		{
			if (string.IsNullOrEmpty (content))
				yield break;
			int idx = provider.SelectionStartPosition < 0 ? 0 : Math.Max (0, provider.SelectionStartPosition);
			int delta = 0;
			var comparison = filter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			int end = provider.SelectionEndPosition < 0 ? content.Length : Math.Min (content.Length, provider.SelectionEndPosition);
			while ((idx = content.IndexOf (pattern, idx, end - idx, comparison)) >= 0) {
				if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt (content, idx, pattern.Length)) {
					if (replacePattern != null) {
						provider.Replace (idx + delta, pattern.Length, replacePattern);
						yield return new SearchResult (provider, idx + delta, replacePattern.Length);
						delta += replacePattern.Length - pattern.Length;
					} else {
						yield return new SearchResult (provider, idx, pattern.Length);
					}
				}
				idx += pattern.Length;
			}
		}
	}
}
