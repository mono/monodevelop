// 
// TypedSegment.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
	public class TypedSegment : ISegment
	{
		public int Offset {
			get {
				return -1; //treeNode != null ? AbstractPartitioner.GetOffsetFromNode (treeNode) : -1;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		public int Length {
			get;
			set;
		}

		public int EndOffset {
			get {
				return Offset + Length;
			}
		}
		
		public string Type {
			get;
			set;
		}

		public TypedSegment (int length, string type)
		{
			this.Length = length;
			this.Type = type;
		}
		
		public bool Contains (int offset)
		{
			int o = Offset;
			return o <= offset && offset < o + Length;
		}

		public bool Contains (ISegment segment)
		{
			return segment != null && Offset <= segment.Offset && segment.EndOffset <= EndOffset;
		}
		
		public override string ToString ()
		{
			return string.Format ("[TypedSegment: Offset={0}, Length={1}, Type={2}]", Offset, Length, Type);
		}
	}
}
