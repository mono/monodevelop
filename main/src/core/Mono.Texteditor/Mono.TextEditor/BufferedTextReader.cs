// BufferedTextReader.cs
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
using System.IO;

namespace Mono.TextEditor
{
	/// <summary>
	/// Wraps the IBuffer interface to a System.IO.TextReader model.
	/// </summary>
	public class BufferedTextReader : System.IO.TextReader
	{
		int position = 0;
		IBuffer buffer;
		
		public BufferedTextReader (IBuffer buffer)
		{
			this.buffer = buffer;
		}
		
		protected override void Dispose (bool disposing)
		{
			if (disposing)
				buffer = null;
		}
		
		public override void Close ()
		{
			Dispose ();
		}
		
		public override int Peek ()
		{
			if (position < 0 || position >= buffer.Length)
				return -1;
			return buffer.GetCharAt (position);
		}
		
		public override int Read ()
		{
			if (position < 0 || position >= buffer.Length)
				return -1;
			return buffer.GetCharAt (position++);
		}
		
		public override int Read (char[] buffer, int index, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException ();
			int lastOffset = System.Math.Min (this.buffer.Length, position + count);
			int length     = lastOffset - position;
			if (length <= 0)
				return 0;
			string text = this.buffer.GetTextAt (position, length);
			text.CopyTo (0, buffer, index, count);
			position += length;
			return length;
		}
		
		public override string ReadToEnd ()
		{
			if (position < 0 || position >= buffer.Length)
				return "";
			string result = this.buffer.GetTextAt (position, buffer.Length - position);
			position = buffer.Length;
			return result;
		}
	}
}
