// IBuffer.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Text;

namespace Mono.TextEditor
{
	public interface IBuffer
	{
		int Length {
			get;
		}
		
		string Text {
			get;
			set;
		}
		
		void Insert (int offset, string value);
		void Remove (int offset, int count);
		void Remove (TextSegment segment);

		void Replace (int offset, int count, string value);
		
		string GetTextAt (int offset, int count);
		string GetTextAt (TextSegment segment);
		char GetCharAt (int offset);
		
		IEnumerable<int> SearchForward (string pattern, int startIndex);
		IEnumerable<int> SearchForwardIgnoreCase (string pattern, int startIndex);
		
		IEnumerable<int> SearchBackward (string pattern, int startIndex);
		IEnumerable<int> SearchBackwardIgnoreCase (string pattern, int startIndex);
	}
}
