// SearchRequest.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
//
//

using System;

namespace Mono.TextEditor
{
	public class SearchRequest
	{
		public string searchPattern;
		public bool caseSensitive;
		public bool wholeWordOnly;
		
		public bool WholeWordOnly {
			get {
				return wholeWordOnly;
			}
			set {
				if (wholeWordOnly != value) {
					wholeWordOnly = value;
					OnChanged ();
				}
			}
		}
		
		public string SearchPattern {
			get {
				return searchPattern;
			}
			set {
				if (searchPattern != value) {
					searchPattern = value;
					OnChanged ();
				}
			}
		}
		
		public bool CaseSensitive {
			get {
				return caseSensitive;
			}
			set {
				if (caseSensitive != value) {
					caseSensitive = value;
					OnChanged ();
				}
			}
		}
		
		public SearchRequest Clone ()
		{
			return new SearchRequest () {
				WholeWordOnly = this.WholeWordOnly,
				SearchPattern = this.SearchPattern,
				CaseSensitive = this.CaseSensitive
			};
		}
		
		void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public event EventHandler Changed;
	}
}
