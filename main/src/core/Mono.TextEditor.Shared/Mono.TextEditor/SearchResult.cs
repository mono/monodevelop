//
// SearchResult.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using MonoDevelop.Core.Text;

namespace Mono.TextEditor
{
	class SearchResult
	{
		public ISegment Segment { get; set; }
		public bool SearchWrapped { get; set; }
		
		public int Offset {
			get {
				return Segment.Offset;
			}
		}

		public int Length {
			get {
				return Segment.Length;
			}
		}

		public int EndOffset {
			get {
				return Segment.EndOffset;
			}
		}

		public SearchResult (ISegment segment, bool searchWrapped)
		{
			this.Segment = segment;
			this.SearchWrapped = searchWrapped;
		}

		public SearchResult (int offset, int length, bool searchWrapped) : this (new TextSegment (offset, length), searchWrapped)
		{
		}

		public override string ToString ()
		{
			return string.Format ("[SearchResult: Offset={0}, Length={1}, SearchWrapped={2}]", this.Offset, this.Length, this.SearchWrapped);
		}
	}
}
