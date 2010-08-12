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
		void Remove (ISegment segment);
		
		void Replace (int offset, int count, string value);
		
		string GetTextAt (int offset, int count);
		string GetTextAt (ISegment segment);
		char GetCharAt (int offset);
		
		IEnumerable<int> SearchForward (string pattern, int startIndex);
		IEnumerable<int> SearchForwardIgnoreCase (string pattern, int startIndex);
		
		IEnumerable<int> SearchBackward (string pattern, int startIndex);
		IEnumerable<int> SearchBackwardIgnoreCase (string pattern, int startIndex);
	}

	/// <summary>
	/// Simple implementation of the buffer interface to support fast read-only documents.
	/// </summary>
	public class StringBuffer : IBuffer
	{
		string buffer;

		public StringBuffer (string buffer)
		{
			this.buffer = buffer;
		}

		#region IBuffer Members
		int IBuffer.Length {
			get { return buffer.Length; }
		}

		string IBuffer.Text {
			get { return buffer; }
			set { buffer = value; }
		}

		void IBuffer.Insert (int offset, string value)
		{
			throw new NotSupportedException ("Operation not supported on this buffer.");
		}

		void IBuffer.Remove (int offset, int count)
		{
			throw new NotSupportedException ("Operation not supported on this buffer.");
		}

		void IBuffer.Remove (ISegment segment)
		{
			throw new NotSupportedException ("Operation not supported on this buffer.");
		}

		void IBuffer.Replace (int offset, int count, string value)
		{
			throw new NotSupportedException ("Operation not supported on this buffer.");
		}

		string IBuffer.GetTextAt (int offset, int count)
		{
			return buffer.Substring (offset, count);
		}

		string IBuffer.GetTextAt (ISegment segment)
		{
			return buffer.Substring (segment.Offset, segment.Length);
		}

		char IBuffer.GetCharAt (int offset)
		{
			return buffer[offset];
		}

		IEnumerable<int> IBuffer.SearchForward (string pattern, int startIndex)
		{
			throw new NotImplementedException();
		}

		IEnumerable<int> IBuffer.SearchForwardIgnoreCase (string pattern, int startIndex)
		{
			throw new NotImplementedException();
		}

		IEnumerable<int> IBuffer.SearchBackward (string pattern, int startIndex)
		{
			throw new NotImplementedException();
		}

		IEnumerable<int> IBuffer.SearchBackwardIgnoreCase (string pattern, int startIndex)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
