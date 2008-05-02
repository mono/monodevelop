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
		
		bool IsValidPattern (string pattern);
		bool IsMatchAt (int offset);
		bool IsMatchAt (int offset, int length);
		
		SearchResult SearchForward (int fromOffset);
		SearchResult SearchBackward (int fromOffset);
	}
	
	public class BasicSearchEngine : ISearchEngine
	{
		string searchPattern = "";
		string compiledPattern = "";
		TextEditorData data;
		
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
		
		public virtual void CompilePattern ()
		{
			compiledPattern = data.IsCaseSensitive ? SearchPattern : SearchPattern.ToUpper ();
		}
		
		public bool IsValidPattern (string pattern)
		{
			return pattern != null;
		}
		
		public bool IsMatchAt (int offset, int length)
		{
			return IsMatchAt (offset) && compiledPattern.Length == length;
		}
		
		public bool IsMatchAt (int offset)
		{
			if (offset + SearchPattern.Length <= data.Document.Length && compiledPattern.Length > 0) {
				if (data.IsCaseSensitive) {
					for (int i = 0; i < compiledPattern.Length; i++) {
						if (data.Document.GetCharAt (offset + i) != compiledPattern[i]) 
							return false;
					}
				} else {
					for (int i = 0; i < compiledPattern.Length; i++) {
						if (System.Char.ToUpper (data.Document.GetCharAt (offset + i)) != compiledPattern[i]) 
							return false;
					}
				}
				if (data.IsWholeWordOnly) {
					return data.Document.IsWholeWordAt (offset, compiledPattern.Length);
				}
				return true;
			}
			return false;
		}
		
		public SearchResult SearchForward (int fromOffset)
		{
			for (int i = 0; i < data.Document.Length - this.SearchPattern.Length; i++) {
				int offset = (fromOffset + i) % data.Document.Length;
				if (IsMatchAt (offset))
					return new SearchResult (offset, this.SearchPattern.Length, offset < fromOffset);
			}
			return null;
		}
		
		public SearchResult SearchBackward (int fromOffset)
		{
			for (int i = 0; i < data.Document.Length - this.SearchPattern.Length; i++) {
				int offset = (fromOffset + data.Document.Length * 2 - 1- i) % data.Document.Length;
				if (IsMatchAt (offset))
					return new SearchResult (offset, this.SearchPattern.Length, offset > fromOffset);
			}
			return null;
		}
		
	
	}
}
