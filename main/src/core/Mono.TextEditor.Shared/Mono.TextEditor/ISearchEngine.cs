// ISearchEngine.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor
{
	interface ISearchEngine
	{
		SearchRequest SearchRequest {
			get;
			set;
		}

		TextEditorData TextEditorData {
			get;
			set;
		}

		void CompilePattern ();

		bool IsValidPattern (string pattern, out string error);

		bool IsMatchAt (int offset);

		bool IsMatchAt (int offset, int length);

		SearchResult GetMatchAt (int offset);

		SearchResult GetMatchAt (int offset, int length);

		SearchResult SearchForward (System.ComponentModel.BackgroundWorker worker, TextViewMargin.SearchWorkerArguments args, int fromOffset);

		SearchResult SearchBackward (System.ComponentModel.BackgroundWorker worker, TextViewMargin.SearchWorkerArguments args, int fromOffset);

		SearchResult SearchForward (int fromOffset);

		SearchResult SearchBackward (int fromOffset);

		void Replace (SearchResult result, string pattern);

		int ReplaceAll (string withPattern);

		ISearchEngine Clone ();
	}

	abstract class AbstractSearchEngine : ISearchEngine
	{
		protected SearchRequest searchRequest;
		protected TextEditorData textEditorData;

		public TextEditorData TextEditorData {
			get {
				return textEditorData;
			}
			set {
				textEditorData = value;
			}
		}

		public SearchRequest SearchRequest {
			get {
				return searchRequest;
			}
			set {
				if (searchRequest != null)
					searchRequest.Changed -= OnRequestChanged;
				searchRequest = value;
				if (searchRequest != null) {
					searchRequest.Changed += OnRequestChanged;
					CompilePattern ();
				}
			}
		}

		void OnRequestChanged (object ob, EventArgs args)
		{
			if (searchRequest != null)
				CompilePattern ();
		}

		public bool IsMatchAt (int offset)
		{
			return GetMatchAt (offset) != null;
		}

		public bool IsMatchAt (int offset, int length)
		{
			return GetMatchAt (offset, length) != null;
		}

		public abstract void CompilePattern ();

		public abstract bool IsValidPattern (string pattern, out string error);

		public abstract SearchResult GetMatchAt (int offset);

		public abstract SearchResult GetMatchAt (int offset, int length);

		public abstract SearchResult SearchForward (System.ComponentModel.BackgroundWorker worker, TextViewMargin.SearchWorkerArguments args, int fromOffset);

		public abstract SearchResult SearchBackward (System.ComponentModel.BackgroundWorker worker, TextViewMargin.SearchWorkerArguments args, int fromOffset);

		public SearchResult SearchForward (int fromOffset)
		{
			return SearchForward (null, new TextViewMargin.SearchWorkerArguments { Text = textEditorData.Text }, fromOffset);
		}

		public SearchResult SearchBackward (int fromOffset)
		{
			return SearchBackward (null, new TextViewMargin.SearchWorkerArguments { Text = textEditorData.Text }, fromOffset);
		}

		public abstract void Replace (SearchResult result, string pattern);

		public abstract int ReplaceAll (string withPattern);

		public virtual ISearchEngine Clone ()
		{
			var result = (ISearchEngine)MemberwiseClone ();
			result.SearchRequest = searchRequest.Clone ();
			return result;
		}
	}

	class BasicSearchEngine : AbstractSearchEngine
	{
		string compiledPattern = "";

		public override void CompilePattern ()
		{
			if (searchRequest.SearchPattern != null)
				compiledPattern = searchRequest.CaseSensitive ? searchRequest.SearchPattern : searchRequest.SearchPattern.ToUpper ();
		}

		public override bool IsValidPattern (string pattern, out string error)
		{
			error = "";
			return pattern != null;
		}

		public override SearchResult GetMatchAt (int offset, int length)
		{
			if (compiledPattern.Length == length) {
				SearchResult match = GetMatchAt (offset);
				if (match != null)
					return match;
			}
			return null;
		}

		public override SearchResult GetMatchAt (int offset)
		{
			if (offset < 0)
				return null;
			var doc = textEditorData.Document;

			if ((!string.IsNullOrEmpty (SearchRequest.SearchPattern)) && offset + searchRequest.SearchPattern.Length <= doc.Length && compiledPattern.Length > 0) {
				if (searchRequest.CaseSensitive) {
					for (int i = 0; i < compiledPattern.Length && offset + i < doc.Length; i++) {
						if (doc.GetCharAt (offset + i) != compiledPattern [i])
							return null;
					}
				} else {
					for (int i = 0; i < compiledPattern.Length && offset + i < doc.Length; i++) {
						if (Char.ToUpper (doc.GetCharAt (offset + i)) != compiledPattern [i])
							return null;
					}
				}
				if (searchRequest.WholeWordOnly) {
					if (!doc.IsWholeWordAt (offset, compiledPattern.Length))
						return null;
				}
				return new SearchResult (offset, compiledPattern.Length, false);
			}
			return null;
		}

		public override SearchResult SearchForward (System.ComponentModel.BackgroundWorker worker, TextViewMargin.SearchWorkerArguments args, int fromOffset)
		{
			if (!string.IsNullOrEmpty (SearchRequest.SearchPattern)) {
				// TODO: Optimize
				for (int i = 0; i < args.Text.Length; i++) {
					int offset = (fromOffset + i) % args.Text.Length;
					if (worker != null && worker.CancellationPending)
						return null; 
					if (IsMatchAt (offset) && (searchRequest.SearchRegion.IsInvalid () || searchRequest.SearchRegion.Contains (offset)))
						return new SearchResult (offset, searchRequest.SearchPattern.Length, offset < fromOffset);
				}
			}
			return null;
		}

		public override SearchResult SearchBackward (System.ComponentModel.BackgroundWorker worker, TextViewMargin.SearchWorkerArguments args, int fromOffset)
		{
			if (!string.IsNullOrEmpty (SearchRequest.SearchPattern)) {
				// TODO: Optimize
				for (int i = 0; i < args.Text.Length; i++) {
					int offset = (fromOffset + args.Text.Length * 2 - 1 - i) % args.Text.Length;
					if (worker != null && worker.CancellationPending)
						return null;
					if (IsMatchAt (offset) && (searchRequest.SearchRegion.IsInvalid () || searchRequest.SearchRegion.Contains (offset)))
						return new SearchResult (offset, searchRequest.SearchPattern.Length, offset > fromOffset);
				}
			}
			return null;
		}

		public override void Replace (SearchResult result, string pattern)
		{
			textEditorData.Replace (result.Offset, result.Length, pattern);
		}

		public override int ReplaceAll (string withPattern)
		{
			var searchResults = new List<SearchResult> ();

			int offset = 0;
			if (!SearchRequest.SearchRegion.IsInvalid ())
				offset = SearchRequest.SearchRegion.Offset;
			SearchResult searchResult; 
			var text = textEditorData.Text;
			var args = new TextViewMargin.SearchWorkerArguments { Text = text };
			while (true) {
				searchResult = SearchForward (null, args, offset);
				if (searchResult == null || searchResult.SearchWrapped)
					break;
				searchResults.Add (searchResult);
				offset = searchResult.EndOffset;
			}
			if (searchResults.Count < 100) {
				using (var undo = textEditorData.OpenUndoGroup ()) {
					for (int i = searchResults.Count - 1; i >= 0; i--) {
						Replace (searchResults [i], withPattern);
					}
					if (searchResults.Count > 0)
						textEditorData.ClearSelection ();
				}
			} else {
				char[] oldText = text.ToCharArray ();
				var newText = new char[oldText.Length + searchResults.Count * (withPattern.Length - compiledPattern.Length)];
				char[] pattern = withPattern.ToCharArray ();
				int curOffset = 0, destOffset = 0;
				foreach (var sr in searchResults) {
					var length = sr.Offset - curOffset;
					Array.Copy (oldText, curOffset, newText, destOffset, length);
					destOffset += length;
					Array.Copy (pattern, 0, newText, destOffset, pattern.Length);
					destOffset += withPattern.Length;
					curOffset = sr.EndOffset;
				}
				var l = textEditorData.Length - curOffset;
				Array.Copy (oldText, curOffset, newText, destOffset, l);

				textEditorData.Replace (0, textEditorData.Length, new string (newText));
				textEditorData.ClearSelection ();
			}
			return searchResults.Count;
		}
	}

	class RegexSearchEngine : AbstractSearchEngine
	{
		Regex regex;

		public override void CompilePattern ()
		{
			try {
				RegexOptions options = RegexOptions.Compiled;
				if (!searchRequest.CaseSensitive)
					options |= RegexOptions.IgnoreCase;
				regex = new Regex (searchRequest.SearchPattern, options);
			} catch (Exception) {
				regex = null;
			}
		}

		public override bool IsValidPattern (string pattern, out string error)
		{
			error = "";
			try {
				RegexOptions options = RegexOptions.Compiled;
				if (!searchRequest.CaseSensitive)
					options |= RegexOptions.IgnoreCase;
				var r = new Regex (searchRequest.SearchPattern, options);
				return r != null;
			} catch (Exception e) {
				error = e.Message;
				return false;
			}
		}

		public override SearchResult GetMatchAt (int offset)
		{
			if (regex == null || String.IsNullOrEmpty (searchRequest.SearchPattern))
				return null;
			var match = regex.Match (textEditorData.Document.Text, offset);
			if (match != null && match.Success && match.Index == offset) {
				return new SearchResult (offset, match.Length, false);
			}
			return null;
		}

		public override SearchResult GetMatchAt (int offset, int length)
		{
			if (regex == null || String.IsNullOrEmpty (searchRequest.SearchPattern))
				return null;
			var match = regex.Match (textEditorData.Document.Text, offset, length);
			if (match != null && match.Success && match.Index == offset) {
				return new SearchResult (offset, match.Length, false);
			}
			return null;
		}

		public override SearchResult SearchForward (System.ComponentModel.BackgroundWorker worker, TextViewMargin.SearchWorkerArguments args, int fromOffset)
		{
			if (regex == null || String.IsNullOrEmpty (searchRequest.SearchPattern))
				return null;
			var match = regex.Match (args.Text, fromOffset);
			if (match.Success) {
				return new SearchResult (match.Index, match.Length, false);
			}
			match = regex.Match (args.Text, 0, fromOffset);
			if (match.Success) {
				return new SearchResult (match.Index, match.Length, true);
			}
			return null;
		}

		public override SearchResult SearchBackward (System.ComponentModel.BackgroundWorker worker, TextViewMargin.SearchWorkerArguments args, int fromOffset)
		{
			if (regex == null || String.IsNullOrEmpty (searchRequest.SearchPattern))
				return null;
			Match found = null; 
			Match last = null; 
			foreach (Match match in regex.Matches (args.Text)) {
				if (match.Index < fromOffset) {
					found = match;
				}
				last = match;
			}
			bool wrapped = false;
			if (found == null) {
				found = last;
				wrapped = true;
			}

			return found != null ? new SearchResult (found.Index, found.Length, wrapped) : null;
		}

		public override void Replace (SearchResult result, string pattern)
		{
			string text = textEditorData.Document.GetTextAt (result.Segment);
			textEditorData.Replace (result.Offset, result.Length, regex.Replace (text, pattern));
		}

		public override int ReplaceAll (string withPattern)
		{
			var searchResults = new List<SearchResult> ();

			int offset = 0;
			if (!SearchRequest.SearchRegion.IsInvalid ())
				offset = SearchRequest.SearchRegion.Offset;
			SearchResult searchResult; 
			var text = textEditorData.Text;
			var args = new TextViewMargin.SearchWorkerArguments { Text = text };
			while (true) {
				searchResult = SearchForward (null, args, offset);
				if (searchResult == null || searchResult.SearchWrapped)
					break;
				searchResults.Add (searchResult);
				offset = searchResult.EndOffset;
			}
			using (var undo = textEditorData.OpenUndoGroup ()) {
				for (int i = searchResults.Count - 1; i >= 0; i--) {
					Replace (searchResults [i], withPattern);
				}
				if (searchResults.Count > 0)
					textEditorData.ClearSelection ();
			}
			return searchResults.Count;
		}
	}
}