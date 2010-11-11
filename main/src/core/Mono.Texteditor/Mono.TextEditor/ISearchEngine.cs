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

namespace Mono.TextEditor
{
	public interface ISearchEngine
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
		
		SearchResult SearchForward (System.ComponentModel.BackgroundWorker worker, int fromOffset);
		SearchResult SearchBackward (System.ComponentModel.BackgroundWorker worker, int fromOffset);
		
		SearchResult SearchForward (int fromOffset);
		SearchResult SearchBackward (int fromOffset);
		
		void Replace (SearchResult result, string pattern);
		
		ISearchEngine Clone ();
	}
	
	public abstract class AbstractSearchEngine : ISearchEngine
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
		
		public abstract SearchResult SearchForward (System.ComponentModel.BackgroundWorker worker, int fromOffset);
		public abstract SearchResult SearchBackward (System.ComponentModel.BackgroundWorker worker, int fromOffset);
		
		public SearchResult SearchForward (int fromOffset)
		{
			return SearchForward (null, fromOffset);
		}
		public SearchResult SearchBackward (int fromOffset)
		{
			return SearchBackward (null, fromOffset);
		}
		public abstract void Replace (SearchResult result, string pattern);
		public virtual ISearchEngine Clone ()
		{
			ISearchEngine result = (ISearchEngine)MemberwiseClone ();
			result.SearchRequest = searchRequest.Clone ();
			return result;
		}
	}
	
	public class BasicSearchEngine : AbstractSearchEngine
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
			if ((!string.IsNullOrEmpty (SearchRequest.SearchPattern)) && offset + searchRequest.SearchPattern.Length <= this.textEditorData.Document.Length && compiledPattern.Length > 0) {
				if (searchRequest.CaseSensitive) {
					for (int i = 0; i < compiledPattern.Length && offset + i < textEditorData.Length; i++) {
						if (this.textEditorData.Document.GetCharAt (offset + i) != compiledPattern[i]) 
							return null;
					}
				} else {
					for (int i = 0; i < compiledPattern.Length && offset + i < textEditorData.Length; i++) {
						if (System.Char.ToUpper (this.textEditorData.Document.GetCharAt (offset + i)) != compiledPattern[i]) 
							return null;
					}
				}
				if (searchRequest.WholeWordOnly) {
					if (!this.textEditorData.Document.IsWholeWordAt (offset, compiledPattern.Length))
						return null;
				}
				return new SearchResult (offset, compiledPattern.Length, false);
			}
			return null;
		}
		
		public override SearchResult SearchForward (System.ComponentModel.BackgroundWorker worker, int fromOffset)
		{
			if (!string.IsNullOrEmpty (SearchRequest.SearchPattern)) {
				for (int i = 0; i < this.textEditorData.Document.Length; i++) {
					int offset = (fromOffset + i) % this.textEditorData.Document.Length;
					if (worker != null && worker.CancellationPending)
						return null;
					if (IsMatchAt (offset))
						return new SearchResult (offset, searchRequest.SearchPattern.Length, offset < fromOffset);
				}
			}
			return null;
		}
		
		public override SearchResult SearchBackward (System.ComponentModel.BackgroundWorker worker, int fromOffset)
		{
			if (!string.IsNullOrEmpty (SearchRequest.SearchPattern)) {
				for (int i = 0; i < this.textEditorData.Document.Length; i++) {
					int offset = (fromOffset + this.textEditorData.Document.Length * 2 - 1 - i) % this.textEditorData.Document.Length;
					if (worker != null && worker.CancellationPending)
						return null;
					if (IsMatchAt (offset))
						return new SearchResult (offset, searchRequest.SearchPattern.Length, offset > fromOffset);
				}
			}
			return null;
		}
		
		public override void Replace (SearchResult result, string pattern)
		{
			textEditorData.Replace (result.Offset, result.Length, pattern);
		}
	}
	
	public class RegexSearchEngine : AbstractSearchEngine
	{
		Regex regex = null;
		
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
				Regex r = new Regex (searchRequest.SearchPattern, options);
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
			System.Text.RegularExpressions.Match match = regex.Match (this.textEditorData.Document.Text, offset);
			if (match != null && match.Success && match.Index == offset) {
				return new SearchResult (offset, match.Length, false);
			}
			return null;
		}
		
		public override SearchResult GetMatchAt (int offset, int length)
		{
			if (regex == null || String.IsNullOrEmpty (searchRequest.SearchPattern))
				return null;
			System.Text.RegularExpressions.Match match = regex.Match (this.textEditorData.Document.Text, offset, length);
			if (match != null && match.Success && match.Index == offset) {
				return new SearchResult (offset, match.Length, false);
			}
			return null;
		}
		
		public override SearchResult SearchForward (System.ComponentModel.BackgroundWorker worker, int fromOffset)
		{
			if (regex == null || String.IsNullOrEmpty (searchRequest.SearchPattern))
				return null;
			System.Text.RegularExpressions.Match match = regex.Match (this.textEditorData.Document.Text, fromOffset);
			if (match.Success) {
				return new SearchResult (match.Index, 
				                         match.Length, false);
			}
			match = regex.Match (this.textEditorData.Document.Text, 0, fromOffset);
			if (match.Success) {
				return new SearchResult (match.Index, 
				                         match.Length, true);
			}
			return null;
		}
		
		public override SearchResult SearchBackward (System.ComponentModel.BackgroundWorker worker, int fromOffset)
		{
			if (regex == null || String.IsNullOrEmpty (searchRequest.SearchPattern))
				return null;
			System.Text.RegularExpressions.Match found = null; 
			System.Text.RegularExpressions.Match last = null; 
			foreach (System.Text.RegularExpressions.Match match in regex.Matches (this.textEditorData.Document.Text)) {
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
			
			if (found != null) {
				return new SearchResult (found.Index, found.Length, wrapped);
			}
			return null;
		}
		
		public override void Replace (SearchResult result, string pattern)
		{
			string text = this.textEditorData.Document.GetTextAt (result);
			this.textEditorData.Replace (result.Offset, result.Length, regex.Replace (text, pattern));
		}
		
	}
	
}
