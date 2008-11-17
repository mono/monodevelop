// SearchResult.cs
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
//

using System;

namespace MonoDevelop.Ide.Gui.Search
{
	public class SearchResult
	{
		public string FileName {
			get;
			internal set;
		}
		
		public IDocumentInformation DocumentInformation {
			get;
			internal set;
		}
		
		public int Position {
			get;
			internal set;
		}
		
		public int DocumentOffset {
			get;
			internal set;
		}

		public int Length {
			get;
			internal set;
		}

		public int Line { get; internal set; }
		public int Column {get; internal set; }
		
		public SearchResult (ITextIterator iter, int length)
		{
			Position = iter.Position;
			DocumentOffset = iter.DocumentOffset;
			Line = iter.Line + 1;
			Column = iter.Column + 1;
			this.Length = length;
			this.DocumentInformation = iter.DocumentInformation;
			this.FileName = DocumentInformation.FileName;
		}

		public virtual string TransformReplacePattern (string pattern)
		{
			return pattern;
		}
		
		public override string ToString ()
		{
			return string.Format("[SearchResult: FileName={0}, DocumentInformation={1}, Position={2}, DocumentOffset={3}, Length={4}, Line={5}, Column={6}]", FileName, DocumentInformation, Position, DocumentOffset, Length, Line, Column);
		}
	}
}
