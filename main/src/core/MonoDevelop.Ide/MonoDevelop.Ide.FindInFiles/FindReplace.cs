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
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

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

		int searchedFilesCount;
		public int SearchedFilesCount {
			get {
				return searchedFilesCount;
			}
			set {
				searchedFilesCount = value;
			}
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

		public IEnumerable<SearchResult> FindAll (Scope scope, ProgressMonitor monitor, string pattern, string replacePattern, FilterOptions filter)
		{
			if (filter.RegexSearch) {
				RegexOptions regexOptions = RegexOptions.Compiled;
				if (!filter.CaseSensitive)
					regexOptions |= RegexOptions.IgnoreCase;
				regex = new Regex (pattern, regexOptions);
			}
			IsRunning = true;
			FoundMatchesCount = SearchedFilesCount = 0;
			monitor.BeginTask (scope.GetDescription (filter, pattern, replacePattern), 150);
			try {
				int totalWork = scope.GetTotalWork (filter);
				int step = Math.Max (1, totalWork / 50);


				var contents = new List<Tuple<FileProvider, string, List<SearchResult>>>();
				foreach (var provider in scope.GetFiles (monitor, filter)) {
					try {
						searchedFilesCount++;
						contents.Add(Tuple.Create (provider, provider.ReadString (), new List<SearchResult> ()));
						if (searchedFilesCount % step == 0)
							monitor.Step (2); 
					} catch (FileNotFoundException) {
						MessageService.ShowError (string.Format (GettextCatalog.GetString ("File {0} not found.")), provider.FileName);
					}
				}

				var results = new List<SearchResult>();
				if (filter.RegexSearch && replacePattern != null) {
					foreach (var content in contents) {
						results.AddRange (RegexSearch (monitor, content.Item1, content.Item2, replacePattern, filter));
					}
				} else {
					Parallel.ForEach (contents, content => { 
						if (monitor.CancellationToken.IsCancellationRequested)
							return;
						try {
							Interlocked.Increment (ref searchedFilesCount);
							content.Item3.AddRange(FindAll (monitor, content.Item1, content.Item2, pattern, replacePattern, filter));
							lock (results) {
								results.AddRange (content.Item3);
							}
							FoundMatchesCount += content.Item3.Count;
							if (searchedFilesCount % step == 0)
								monitor.Step (1); 
						} catch (Exception e) {
							LoggingService.LogError("Exception during search.", e);
						}
					});

					if (replacePattern != null) {
						foreach (var content in contents) {
							if (content.Item3.Count == 0)
								continue;
							try {
								content.Item1.BeginReplace (content.Item2);
								Replace (content.Item1, content.Item3, replacePattern);
								content.Item1.EndReplace ();
							} catch (Exception e) {
								LoggingService.LogError("Exception during replace.", e);
							}
						}
					}
				}

				return results;
			} finally {
				monitor.EndTask ();
				IsRunning = false;
			}
		}

		IEnumerable<SearchResult> FindAll (ProgressMonitor monitor, FileProvider provider, string content, string pattern, string replacePattern, FilterOptions filter)
		{
			if (string.IsNullOrEmpty (pattern))
				return Enumerable.Empty<SearchResult> ();

			if (filter.RegexSearch)
				return RegexSearch (monitor, provider, content, replacePattern, filter);

			return Search (provider, content, pattern, filter);
		}

		IEnumerable<SearchResult> RegexSearch (ProgressMonitor monitor, FileProvider provider, string content, string replacePattern, FilterOptions filter)
		{
			var results = new List<SearchResult> ();
			if (replacePattern == null) {
				foreach (Match match in regex.Matches (content)) {
					if (monitor.CancellationToken.IsCancellationRequested)
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
				for (int i = 0; !monitor.CancellationToken.IsCancellationRequested && i < matches.Count; i++) {
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

		public IEnumerable<SearchResult> Search (FileProvider provider, string content, string pattern, FilterOptions filter)
		{
			if (string.IsNullOrEmpty (content))
				yield break;
			int idx = provider.SelectionStartPosition < 0 ? 0 : Math.Max (0, provider.SelectionStartPosition);
			var comparison = filter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			int end = provider.SelectionEndPosition < 0 ? content.Length : Math.Min (content.Length, provider.SelectionEndPosition);
			while ((idx = content.IndexOf (pattern, idx, end - idx, comparison)) >= 0) {
				if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt (content, idx, pattern.Length)) {
					yield return new SearchResult (provider, idx, pattern.Length);
				}
				idx += pattern.Length;
			}
		}

		public void Replace (FileProvider provider, IEnumerable<SearchResult> searchResult, string replacePattern)
		{
			int delta = 0;
			foreach (var sr in searchResult) {
				provider.Replace (sr.Offset + delta, sr.Length, replacePattern);
				delta += replacePattern.Length - sr.Length;
			}
		}
	}
}
