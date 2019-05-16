// 
// FindInFilesSession.cs
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
	class FindInFilesSession
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

		public FindInFilesSession ()
		{
			IsRunning = false;
		}

		public bool ValidatePattern (FindInFilesModel filter, string pattern)
		{
			if (filter.RegexSearch) {
				try {
					_ = new Regex (pattern);
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
			public SearchResult[] Results;
			public string Text { get; internal set; }
			public System.Text.Encoding Encoding { get; internal set; }

			public FileSearchResult (FileProvider provider, TextReader reader)
			{
				Provider = provider;
				Reader = reader;
			}
		}

		public IEnumerable<SearchResult> FindAll (FindInFilesModel model, IReadOnlyList<FileProvider> fileList, ProgressMonitor monitor, CancellationToken token)
		{
			if (model.RegexSearch) {
				RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.Multiline;
				if (!model.CaseSensitive)
					regexOptions |= RegexOptions.IgnoreCase;
				regex = new Regex (model.FindPattern, regexOptions);
			}
			IsRunning = true;
			FoundMatchesCount = SearchedFilesCount = 0;

			try {
				int totalWork = fileList.Count;
				int step = Math.Max (1, totalWork / 50);

				var contents = new FileSearchResult [fileList.Count];
				for (var i = 0; i < fileList.Count; i++) {
					var provider = fileList [i];
					if (token.IsCancellationRequested)
						return Enumerable.Empty<SearchResult> ();
					try {
						searchedFilesCount++;
						contents[i] = new FileSearchResult (provider, null);

						if (searchedFilesCount % step == 0)
							monitor.Step (2);
					} catch (FileNotFoundException) {
						MessageService.ShowError (string.Format (GettextCatalog.GetString ("File {0} not found.")), provider.FileName);
					}
				}

				var results = new List<SearchResult> ();
				if (model.RegexSearch && model.InReplaceMode) {
					foreach (var content in contents) {
						if (token.IsCancellationRequested)
							return Enumerable.Empty<SearchResult> ();
						results.AddRange (RegexSearch (model, monitor, content.Provider));
					}
				} else {
					var options = new ParallelOptions {
						MaxDegreeOfParallelism = 4,
						CancellationToken = token
					};
					Parallel.ForEach (contents, options, content => {
						if (token.IsCancellationRequested)
							return;
						try {
							Interlocked.Increment (ref searchedFilesCount);
							if (model.InReplaceMode) {
								content.Text = content.Reader.ReadToEnd ();
								content.Encoding = content.Provider.CurrentEncoding;
								content.Reader = new StringReader (content.Text);
							}
							content.Results = FindAll (model, monitor, content.Provider);
							lock (results) {
								results.AddRange (content.Results);
							}
							FoundMatchesCount += content.Results.Length;
							if (searchedFilesCount % step == 0)
								monitor.Step (1);
						} catch (Exception e) {
							LoggingService.LogError ("Exception during search.", e);
						}
					});

					if (model.InReplaceMode) {
						foreach (var content in contents) {
							if (token.IsCancellationRequested)
								return Enumerable.Empty<SearchResult> ();
							if (content.Results == null || content.Results.Length == 0)
								continue;
							try {
								content.Provider.BeginReplace (content.Text, content.Encoding);
								Replace (content.Provider, content.Results, model.ReplacePattern);
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

		SearchResult[] FindAll (FindInFilesModel model, ProgressMonitor monitor, FileProvider provider)
		{
			if (string.IsNullOrEmpty (model.FindPattern))
				return Array.Empty<SearchResult> ();

			if (model.RegexSearch)
				return RegexSearch (model, monitor, provider);

			return Search (model, provider);
		}

		SearchResult[] RegexSearch (FindInFilesModel model, ProgressMonitor monitor, FileProvider provider)
		{
			string content = IdeApp.Workbench.GetDocumentText (provider.FileName);
			var results = new List<SearchResult> ();
			if (!model.InReplaceMode) {
				foreach (Match match in regex.Matches (content)) {
					if (monitor.CancellationToken.IsCancellationRequested)
						break;
					if (provider.SelectionStartPosition > -1 && match.Index < provider.SelectionStartPosition)
						continue;
					if (provider.SelectionEndPosition > -1 && match.Index + match.Length > provider.SelectionEndPosition)
						continue;
					if (!model.WholeWordsOnly || FindInFilesModel.IsWholeWordAt(content, match.Index, match.Length))
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
					if (!model.WholeWordsOnly || FindInFilesModel.IsWholeWordAt (content, match.Index, match.Length)) {
						string replacement = match.Result (model.ReplacePattern);
						results.Add (new SearchResult (provider, match.Index + delta, replacement.Length));
						provider.Replace (match.Index + delta, match.Index, match.Length, replacement);
						delta += replacement.Length - match.Length;
					}
				}
				provider.EndReplace ();
			}
			return results.ToArray ();
		}

		SearchResult[] Search (FindInFilesModel model, FileProvider provider)
		{
			string content = IdeApp.Workbench != null ? IdeApp.Workbench.GetDocumentText (provider.FileName) : File.ReadAllText (provider.FileName);
			var findResults = model.PatternSearcher.FindAll (content);
			var result = new SearchResult [findResults.Length];
			for (var i = 0; i < findResults.Length; i++) {
				result[i] = new SearchResult (provider, findResults [i], model.FindPattern.Length);
			}
			return result;
		}

		public void Replace (FileProvider provider, SearchResult[] searchResult, string replacePattern)
		{
			int delta = 0;
			foreach (var sr in searchResult) {
				provider.Replace (sr.Offset + delta, sr.Offset, sr.Length, replacePattern);
				delta += replacePattern.Length - sr.Length;
			}
		}
	}
}
