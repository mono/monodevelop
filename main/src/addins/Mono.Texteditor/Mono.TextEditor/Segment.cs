// Segment.cs
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

namespace Mono.TextEditor
{
	public class Segment : ISegment
	{
		protected int offset;
		protected int length;
		
		public virtual int Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}

		public virtual int Length {
			get {
				return length;
			}
			set {
				length = value;
			}
		}
		
		public int EndOffset {
			get {
				return Offset + Length;
			}
		}

		protected Segment ()
		{
		}
		
		public Segment (int offset, int length)
		{
			this.offset = offset;
			this.length = length;
		}
		
		public static bool Equals (ISegment left, ISegment right)
		{
			return left != null && right != null && left.Offset == right.Offset && left.Length == right.Length;
		}
		
		public bool Contains (int offset)
		{
			return Offset <= offset && offset < EndOffset;
		}
		public bool Contains (ISegment segment)
		{
			return  segment != null && Offset <= segment.Offset && segment.EndOffset <= EndOffset;
		}
		
		public override string ToString ()
		{
			return String.Format ("[Segment: Offset={0}, Length={1}]", this.offset, this.length);
		}
	}
}
