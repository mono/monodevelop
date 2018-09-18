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


		public IEnumerable<SearchResult> FindAll (Scope scope, ProgressMonitor monitor, string pattern, string replacePattern, FilterOptions filter, CancellationToken token)
		{
			if (filter.RegexSearch) {
				RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.Multiline;
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

				var contents = new List<FileSearchResult> ();
				var filenames = new List<string> ();
				foreach (var provider in scope.GetFiles (monitor, filter)) {
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

				var readers = IdeApp.Workbench.GetDocumentReaders (filenames);

				int idx = 0;
				if (readers != null) {
					foreach (var r in readers) {
						contents [idx].Reader = r;

						idx++;
					}
				}

				idx = 0;
				int c = 0;
				int t = 0;
				foreach (var result in contents) {
					if (readers == null || readers [idx] == null) {
						result.Reader = result.Provider.GetReaderForFileName ();
					} else {
						result.Reader = readers [idx];
						c++;
					}
					t++;
					idx++;
				}

				var results = new List<SearchResult> ();
				if (filter.RegexSearch && replacePattern != null) {
					foreach (var content in contents) {
						if (token.IsCancellationRequested)
							return Enumerable.Empty<SearchResult> ();
						results.AddRange (RegexSearch (monitor, content.Provider, content.Reader, replacePattern, filter));
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
							content.Results.AddRange (FindAll (monitor, content.Provider, content.Reader, pattern, replacePattern, filter));
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
				monitor.EndTask ();
				IsRunning = false;
			}
		}

		// Took: 17743

		IEnumerable<SearchResult> FindAll (ProgressMonitor monitor, FileProvider provider, TextReader content, string pattern, string replacePattern, FilterOptions filter)
		{
			if (string.IsNullOrEmpty (pattern))
				return Enumerable.Empty<SearchResult> ();

			if (filter.RegexSearch)
				return RegexSearch (monitor, provider, content, replacePattern, filter);

			return Search (provider, content, pattern, filter);
		}

		IEnumerable<SearchResult> RegexSearch (ProgressMonitor monitor, FileProvider provider, TextReader reader, string replacePattern, FilterOptions filter)
		{
			string content = reader.ReadToEnd ();
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
						provider.Replace (match.Index + delta, match.Length, replacement);
						delta += replacement.Length - match.Length;
					}
				}
				provider.EndReplace ();
			}
			return results;
		}

		class RingBufferReader
		{
			int i, l;
			char [] buffer;
			TextReader reader;

			public RingBufferReader (TextReader reader, int bufferSize)
			{
				this.reader = reader;
				buffer = new char [bufferSize];
			}

			public int Next ()
			{
				if (l == 0) {
					int ch = reader.Read ();
					buffer [i] = (char)ch;
					i = (i + 1) % buffer.Length;
					return ch;
				}
				l--;
				var result = buffer [i];
				i = (i + 1) % buffer.Length;
				return result;
			}

			public void TakeBack (int num)
			{
				l += num;
				i = (i + buffer.Length - num) % buffer.Length;
			}
		}

		public IEnumerable<SearchResult> Search (FileProvider provider, TextReader reader, string pattern, FilterOptions filter)
		{
			if (reader == null)
				yield break;
			int i = provider.SelectionStartPosition < 0 ? 0 : Math.Max (0, provider.SelectionStartPosition);
			var buffer = new RingBufferReader(reader, pattern.Length + 2);
			bool wasSeparator = true;
			if (!filter.CaseSensitive)
				pattern = pattern.ToUpperInvariant ();
			while (true) {
				int next = buffer.Next ();
				if (next < 0)
					yield break;
				char ch = (char)next;
				if ((filter.CaseSensitive ? ch : char.ToUpperInvariant (ch)) == pattern [0] &&
				    (!filter.WholeWordsOnly || wasSeparator)) {
					bool isMatch = true;
					for (int j = 1; j < pattern.Length; j++) {
						next = buffer.Next ();
						if (next < 0)
							yield break;
						if ((filter.CaseSensitive ? next : char.ToUpperInvariant ((char)next)) != pattern [j]) {
							buffer.TakeBack (j);
							isMatch = false;
							break;
						}
					}
					if (isMatch) {
						if (filter.WholeWordsOnly) {
							next = buffer.Next ();
							if (next >= 0 && !FilterOptions.IsWordSeparator ((char)next)) {
								buffer.TakeBack (pattern.Length);
								i++;
								continue;
							}
							buffer.TakeBack (1);
						}

						yield return new SearchResult (provider, i, pattern.Length);
						i += pattern.Length - 1;
					}
				}

				i++;
				if (filter.WholeWordsOnly) {
					wasSeparator = FilterOptions.IsWordSeparator ((char)ch);
				}
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
