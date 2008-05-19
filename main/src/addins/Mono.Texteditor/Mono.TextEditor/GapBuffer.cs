// GapBuffer.cs
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
using System.Diagnostics;
using System.Text;

namespace Mono.TextEditor
{
	public class GapBuffer : AbstractBuffer
	{
		char[] buffer = new char[0];
		
		int gapBegin  = 0;
		int gapEnd    = 0;
		int gapLength = 0;
		
		const int minGapLength = 16 * 1024;
		const int maxGapLength = 256 * 1024;
		
		public override int Length {
			get {
				return buffer.Length - gapLength;
			}
		}
		
		public override string Text {
			get {
				return GetTextAt (0, Length);
			}
			set {
				buffer = value != null ? value.ToCharArray () : new char[0];
				gapBegin = gapEnd = gapLength = 0;
			}
		}
		
		public override void Dispose ()
		{
			buffer = null;
		}
		
		public override char GetCharAt (int offset)
		{
#if DEBUG
			if (offset < 0) 
				Debug.Assert (false, "offset was '" + offset +"' value must be >= 0." + Environment.NewLine + Environment.StackTrace);
			if (offset >= Length) 
				Debug.Assert (false, "offset was '" + offset +"' value must be < Length = " + Length + "." + Environment.NewLine + Environment.StackTrace);
#endif
			return buffer[offset < gapBegin ? offset : offset + gapLength];
		}
		
		public override string GetTextAt (int offset, int count)
		{
#if DEBUG
			if (offset < 0) 
				Debug.Assert (false, "offset was '" + offset +"' value must be >= 0." + Environment.NewLine + Environment.StackTrace);
			if (offset > Length) 
				Debug.Assert (false, "offset was '" + offset +"' value must be <= Length = " + Length + "." + Environment.NewLine + Environment.StackTrace);
			if (count < 0) 
				Debug.Assert (false, "count was '" + count +"' value must be >= 0." + Environment.NewLine + Environment.StackTrace);
			if (offset + count > Length) 
				Debug.Assert (false, "count was '" + count +"' value must be offset + count <= Length = " + Length + " offset was " + offset + " and count was " + count + Environment.NewLine + Environment.StackTrace);
#endif
			
			int end = offset + count;
			if (end < gapBegin) 
				return new string (buffer, offset, count);
			if (offset > gapBegin) 
				return new string (buffer, offset + gapLength, count);
		
			int leftBlockSize = gapBegin - offset;
			int rightBlockSize = end - gapBegin;
			char[] result = new char [leftBlockSize + rightBlockSize];
			Array.Copy (buffer, offset, result, 0, leftBlockSize);
			Array.Copy (buffer, gapEnd, result, leftBlockSize, rightBlockSize);
			return new string (result);
		}
		
		public override void Replace (int offset, int count, StringBuilder text)
		{
#if DEBUG
			if (offset < 0) 
				Debug.Assert (false, "offset was '" + offset +"' value must be >= 0." + Environment.NewLine + Environment.StackTrace);
			if (offset > Length) 
				Debug.Assert (false, "offset was '" + offset +"' value must be <= Length = " + Length + "." + Environment.NewLine + Environment.StackTrace);
			if (count < 0) 
				Debug.Assert (false, "count was '" + count +"' value must be >= 0." + Environment.NewLine + Environment.StackTrace);
			if (offset + count > Length) 
				Debug.Assert (false, "count was '" + count +"' value must be offset + count <= Length = " + Length + " offset was " + offset + " and count was " + count + Environment.NewLine + Environment.StackTrace);
#endif
			
			if (text != null) { 
				PlaceGap (offset, text.Length - count);
				text.CopyTo (0, buffer, offset, text.Length);
				gapBegin += text.Length;
			} else {
				PlaceGap (offset, count);
			}
			gapEnd   += count; 
			gapLength = gapEnd - gapBegin;
			if (gapLength > maxGapLength) 
				CreateBuffer (gapBegin, minGapLength);
		}
		
		void PlaceGap (int newOffset, int minLength)
		{
			if (gapLength < minLength) {
				CreateBuffer (newOffset, minLength);
				return;
			}
			
			int delta = gapBegin - newOffset;
			if (delta > 0) {
				Array.Copy (buffer, newOffset, buffer, gapEnd - delta, delta);
			} else {
				Array.Copy (buffer, gapEnd, buffer, gapBegin, -delta);
			}
			gapBegin -= delta;
			gapEnd   -= delta;
		}
		
		void CreateBuffer (int gapOffset, int gapLength)
		{
			gapLength = System.Math.Max (minGapLength, gapLength);
			
			char[] newBuffer = new char[Length + gapLength];
			if (gapOffset < gapBegin) {
				Array.Copy (buffer, 0, newBuffer, 0, gapOffset);
				Array.Copy (buffer, gapOffset, newBuffer, gapOffset + gapLength, gapBegin - gapOffset);
				Array.Copy (buffer, gapEnd, newBuffer, newBuffer.Length - (buffer.Length - gapEnd), buffer.Length - gapEnd);
			} else {
				Array.Copy (buffer, 0, newBuffer, 0, gapBegin);
				Array.Copy (buffer, gapEnd, newBuffer, gapBegin, gapOffset - gapBegin);
				int lastPartLength = newBuffer.Length - (gapOffset + gapLength);
				Array.Copy (buffer, buffer.Length - lastPartLength, newBuffer, gapOffset + gapLength, lastPartLength);
			}
			
			gapBegin  = gapOffset;
			gapEnd    = gapOffset + gapLength;
			this.gapLength = gapLength;
			buffer    = newBuffer;
		}
	}
}