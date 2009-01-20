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
		
		Document Document {
			get;
			set;
		}
		
		void CompilePattern ();
		
		bool IsValidPattern (string pattern, out string error);
		bool IsMatchAt (int offset);
		bool IsMatchAt (int offset, int length);
		
		SearchResult GetMatchAt (int offset);
		SearchResult GetMatchAt (int offset, int length);
		
		SearchResult SearchForward (int fromOffset);
		SearchResult SearchBackward (int fromOffset);
		
		void Replace (SearchResult result, string pattern);
	}
	
	public abstract class AbstractSearchEngine : ISearchEngine
	{
		protected SearchRequest searchRequest;
		protected Document document;
		
		public Document Document {
			get {
				return document;
			}
			set {
				document = value;
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
		
		public abstract SearchResult SearchForward (int fromOffset);
		public abstract SearchResult SearchBackward (int fromOffset);
		public abstract void Replace (SearchResult result, string pattern);
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
			if (offset + searchRequest.SearchPattern.Length <= document.Length && compiledPattern.Length > 0) {
				if (searchRequest.CaseSensitive) {
					for (int i = 0; i < compiledPattern.Length; i++) {
						if (document.GetCharAt (offset + i) != compiledPattern[i]) 
							return null;
					}
				} else {
					for (int i = 0; i < compiledPattern.Length; i++) {
						if (System.Char.ToUpper (document.GetCharAt (offset + i)) != compiledPattern[i]) 
							return null;
					}
				}
				if (searchRequest.WholeWordOnly) {
					if (!document.IsWholeWordAt (offset, compiledPattern.Length))
						return null;
				}
				return new SearchResult (offset, compiledPattern.Length, false);
			}
			return null;
		}
		
		public override SearchResult SearchForward (int fromOffset)
		{
			for (int i = 0; i < document.Length - searchRequest.SearchPattern.Length; i++) {
				int offset = (fromOffset + i) % document.Length;
				if (IsMatchAt (offset))
					return new SearchResult (offset, searchRequest.SearchPattern.Length, offset < fromOffset);
			}
			return null;
		}
		
		public override SearchResult SearchBackward (int fromOffset)
		{
			for (int i = 0; i < document.Length - searchRequest.SearchPattern.Length; i++) {
				int offset = (fromOffset + document.Length * 2 - 1- i) % document.Length;
				if (IsMatchAt (offset))
					return new SearchResult (offset, searchRequest.SearchPattern.Length, offset > fromOffset);
			}
			return null;
		}
		
		public override void Replace (SearchResult result, string pattern)
		{
			document.Replace (result.Offset, result.Length, pattern);
		}
	}
	
	public class RegexSearchEngine : AbstractSearchEngine
	{
		Regex regex = null;
		
		public override void CompilePattern ()
		{
			try {
				regex = new Regex (searchRequest.SearchPattern, RegexOptions.Compiled);
			} catch (Exception) {
				regex = null;
			}
		}
		
		public override bool IsValidPattern (string pattern, out string error)
		{
			error = "";
			try {
				Regex r = new Regex (searchRequest.SearchPattern, RegexOptions.Compiled);
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
			System.Text.RegularExpressions.Match match = regex.Match (document.Text, offset);
			if (match != null && match.Success && match.Index == offset) {
				return new SearchResult (offset, match.Length, false);
			}
			return null;
		}
		
		public override SearchResult GetMatchAt (int offset, int length)
		{
			if (regex == null || String.IsNullOrEmpty (searchRequest.SearchPattern))
				return null;
			System.Text.RegularExpressions.Match match = regex.Match (document.Text, offset, length);
			if (match != null && match.Success && match.Index == offset) {
				return new SearchResult (offset, match.Length, false);
			}
			return null;
		}
		
		public override SearchResult SearchForward (int fromOffset)
		{
			if (regex == null)
				return null;
			System.Text.RegularExpressions.Match match = regex.Match (document.Text, fromOffset);
			if (match.Success) {
				return new SearchResult (match.Index, 
				                         match.Length, false);
			}
			match = regex.Match (document.Text, 0, fromOffset);
			if (match.Success) {
				return new SearchResult (match.Index, 
				                         match.Length, true);
			}
			return null;
		}
		
		public override SearchResult SearchBackward (int fromOffset)
		{
			if (regex == null)
				return null;
			System.Text.RegularExpressions.Match found = null; 
			System.Text.RegularExpressions.Match last = null; 
			foreach (System.Text.RegularExpressions.Match match in regex.Matches (document.Text)) {
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
			string text = document.GetTextAt (result);
			document.Replace (result.Offset, result.Length, regex.Replace (text, pattern));
		}
		
	}
	
}
