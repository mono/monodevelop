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
		TextEditorData TextEditorData {
			get;
			set;
		}
		
		string SearchPattern {
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
	}
	
	public abstract class AbstractSearchEngine : ISearchEngine
	{
		protected TextEditorData data;
		protected string searchPattern = "";
		
		public TextEditorData TextEditorData {
			get {
				return data;
			}
			set {
				data = value;
			}
		}
		
		public string SearchPattern {
			get {
				return searchPattern;
			}
			set {
				searchPattern = value;
				CompilePattern ();
			}
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
	}
	
	public class BasicSearchEngine : AbstractSearchEngine
	{
		string compiledPattern = "";
		public override void CompilePattern ()
		{
			compiledPattern = data.IsCaseSensitive ? SearchPattern : SearchPattern.ToUpper ();
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
			if (offset + SearchPattern.Length <= data.Document.Length && compiledPattern.Length > 0) {
				if (data.IsCaseSensitive) {
					for (int i = 0; i < compiledPattern.Length; i++) {
						if (data.Document.GetCharAt (offset + i) != compiledPattern[i]) 
							return null;
					}
				} else {
					for (int i = 0; i < compiledPattern.Length; i++) {
						if (System.Char.ToUpper (data.Document.GetCharAt (offset + i)) != compiledPattern[i]) 
							return null;
					}
				}
				if (data.IsWholeWordOnly) {
					if (!data.Document.IsWholeWordAt (offset, compiledPattern.Length))
						return null;
				}
				return new SearchResult (offset, compiledPattern.Length, false);
			}
			return null;
		}
		
		public override SearchResult SearchForward (int fromOffset)
		{
			for (int i = 0; i < data.Document.Length - this.SearchPattern.Length; i++) {
				int offset = (fromOffset + i) % data.Document.Length;
				if (IsMatchAt (offset))
					return new SearchResult (offset, this.SearchPattern.Length, offset < fromOffset);
			}
			return null;
		}
		
		public override SearchResult SearchBackward (int fromOffset)
		{
			for (int i = 0; i < data.Document.Length - this.SearchPattern.Length; i++) {
				int offset = (fromOffset + data.Document.Length * 2 - 1- i) % data.Document.Length;
				if (IsMatchAt (offset))
					return new SearchResult (offset, this.SearchPattern.Length, offset > fromOffset);
			}
			return null;
		}
	}
	
	public class RegexSearchEngine : AbstractSearchEngine
	{
		Regex regex = null;
		
		public override void CompilePattern ()
		{
			try {
				regex = new Regex (this.searchPattern, RegexOptions.Compiled);
			} catch (Exception) {
				regex = null;
			}
		}
		
		public override bool IsValidPattern (string pattern, out string error)
		{
			error = "";
			try {
				Regex r = new Regex (this.searchPattern, RegexOptions.Compiled);
				return r != null;
			} catch (Exception e) {
				error = e.Message;
				return false;
			}
		}
		
		public override SearchResult GetMatchAt (int offset)
		{
			if (regex == null || String.IsNullOrEmpty (this.searchPattern))
				return null;
			System.Text.RegularExpressions.Match match = regex.Match (data.Document.Text, offset);
			if (match != null && match.Success && match.Index == offset) {
				return new SearchResult (offset, match.Length, false);
			}
			return null;
		}
		
		public override SearchResult GetMatchAt (int offset, int length)
		{
			if (regex == null || String.IsNullOrEmpty (this.searchPattern))
				return null;
			System.Text.RegularExpressions.Match match = regex.Match (data.Document.Text, offset, length);
			if (match != null && match.Success && match.Index == offset) {
				return new SearchResult (offset, match.Length, false);
			}
			return null;
		}
		
		public override SearchResult SearchForward (int fromOffset)
		{
			if (regex == null)
				return null;
			System.Text.RegularExpressions.Match match = regex.Match (data.Document.Text, fromOffset);
			if (match.Success) {
				return new SearchResult (match.Index, 
				                         match.Length, false);
			}
			match = regex.Match (data.Document.Text, 0, fromOffset);
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
			foreach (System.Text.RegularExpressions.Match match in regex.Matches (data.Document.Text)) {
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
	}
	
}
