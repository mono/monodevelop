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
	class FindReplace
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
					new Regex (pattern);
					return true;
				} catch (Exception) {
					return false;
				}
			}
			return true;
		}

		class FileSearchResult
		{
			public FileProvider Provider;
			public TextReader Reader;
			public List<SearchResult> Results;
			public string Text { get; internal set; }
			public System.Text.Encoding Encoding { get; internal set; }

			public FileSearchResult (FileProvider provider, TextReader reader, List<SearchResult> results)
			{
				Provider = provider;
				Reader = reader;
				Results = results;
			}
		}


		public IEnumerable<SearchResult> FindAll (IReadOnlyList<FileProvider> fileList, ProgressMonitor monitor, string pattern, string replacePattern, FilterOptions filter, CancellationToken token)
		{
			if (filter.RegexSearch) {
				RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.Multiline;
				if (!filter.CaseSensitive)
					regexOptions |= RegexOptions.IgnoreCase;
				regex = new Regex (pattern, regexOptions);
			}
			IsRunning = true;
			FoundMatchesCount = SearchedFilesCount = 0;

			try {
				int totalWork = fileList.Count;
				int step = Math.Max (1, totalWork / 50);

				var contents = new List<FileSearchResult> ();
				var filenames = new List<string> ();
				foreach (var provider in fileList) {
					if (token.IsCancellationRequested)
						return Enumerable.Empty<SearchResult> ();
					try {
						searchedFilesCount++;
						contents.Add (new FileSearchResult (provider, null, new List<SearchResult> ()));

						filenames.Add (Path.GetFullPath (provider.FileName));

						if (searchedFilesCount % step == 0)
							monitor.Step (2);
					} catch (FileNotFoundException) {
						MessageService.ShowError (string.Format (GettextCatalog.GetString ("File {0} not found.")), provider.FileName);
					}
				}

				var results = new List<SearchResult> ();
				if (filter.RegexSearch && replacePattern != null) {
					foreach (var content in contents) {
						if (token.IsCancellationRequested)
							return Enumerable.Empty<SearchResult> ();
						results.AddRange (RegexSearch (monitor, content.Provider, replacePattern, filter));
					}
				} else {
					var options = new ParallelOptions ();
					options.MaxDegreeOfParallelism = 4;
					options.CancellationToken = token;
					Parallel.ForEach (contents, options, content => {
						if (token.IsCancellationRequested)
							return;
						try {
							Interlocked.Increment (ref searchedFilesCount);
							if (replacePattern != null) {
								content.Text = content.Reader.ReadToEnd ();
								content.Encoding = content.Provider.CurrentEncoding;
								content.Reader = new StringReader (content.Text);
							}
							content.Results.AddRange (FindAll (monitor, content.Provider, pattern, replacePattern, filter));
							lock (results) {
								results.AddRange (content.Results);
							}
							FoundMatchesCount += content.Results.Count;
							if (searchedFilesCount % step == 0)
								monitor.Step (1);
						} catch (Exception e) {
							LoggingService.LogError ("Exception during search.", e);
						}
					});

					if (replacePattern != null) {
						foreach (var content in contents) {
							if (token.IsCancellationRequested)
								return Enumerable.Empty<SearchResult> ();
							if (content.Results.Count == 0)
								continue;
							try {
								content.Provider.BeginReplace (content.Text, content.Encoding);
								Replace (content.Provider, content.Results, replacePattern);
								content.Provider.EndReplace ();
							} catch (Exception e) {
								LoggingService.LogError ("Exception during replace.", e);
							}
						}
					}
				}

				return results;
			} catch (OperationCanceledException) {
				return Enumerable.Empty<SearchResult> ();
			} finally {
				IsRunning = false;
			}
		}

		// Took: 17743

		IEnumerable<SearchResult> FindAll (ProgressMonitor monitor, FileProvider provider, string pattern, string replacePattern, FilterOptions filter)
		{
			if (string.IsNullOrEmpty (pattern))
				return Enumerable.Empty<SearchResult> ();

			if (filter.RegexSearch)
				return RegexSearch (monitor, provider, replacePattern, filter);

			return Search (provider, pattern, filter);
		}

		IEnumerable<SearchResult> RegexSearch (ProgressMonitor monitor, FileProvider provider, string replacePattern, FilterOptions filter)
		{
			string content = IdeApp.Workbench.GetDocumentText (provider.FileName);
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
				provider.BeginReplace (content, provider.CurrentEncoding);
				int delta = 0;
				for (int i = 0; !monitor.CancellationToken.IsCancellationRequested && i < matches.Count; i++) {
					Match match = matches[i];
					if (!filter.WholeWordsOnly || FilterOptions.IsWholeWordAt (content, match.Index, match.Length)) {
						string replacement = match.Result (replacePattern);
						results.Add (new SearchResult (provider, match.Index + delta, replacement.Length));
						provider.Replace (match.Index + delta, match.Index, match.Length, replacement);
						delta += replacement.Length - match.Length;
					}
				}
				provider.EndReplace ();
			}
			return results;
		}


		public IEnumerable<SearchResult> Search (FileProvider provider, string pattern, FilterOptions filter)
		{
			var searcher = new PatternSearcher (pattern, filter.CaseSensitive, filter.WholeWordsOnly);
			string content = IdeApp.Workbench.GetDocumentText (provider.FileName);

			foreach (var idx in searcher.FindAll (content)) {
				yield return new SearchResult (provider, idx, pattern.Length);
			}
		}

		public void Replace (FileProvider provider, IEnumerable<SearchResult> searchResult, string replacePattern)
		{
			int delta = 0;
			foreach (var sr in searchResult) {
				provider.Replace (sr.Offset + delta, sr.Offset, sr.Length, replacePattern);
				delta += replacePattern.Length - sr.Length;
			}
		}
	}
}
