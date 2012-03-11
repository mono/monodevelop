// 
// TextSegment.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
	public struct TextSegment : IEquatable<TextSegment>, ICSharpCode.NRefactory.Editor.ISegment
	{
		public static readonly TextSegment Invalid = new TextSegment (-1, 0);

		readonly int offset;
		public int Offset {
			get {
				return offset;
			}
		}

		readonly int length;
		public int Length {
			get {
				return length;
			}
		}

		public int EndOffset {
			get {
				return Offset + Length;
			}
		}

		public bool IsEmpty {
			get {
				return Length == 0;
			}
		}

		public bool IsInvalid {
			get {
				return Offset < 0;
			}
		}

		public TextSegment (int offset, int length)
		{
			this.offset = offset;
			this.length = length;
		}

		public static bool operator == (TextSegment left, TextSegment right)
		{
			return left.Equals (right);
		}

		public static bool operator != (TextSegment left, TextSegment right)
		{
			return !left.Equals (right);
		}

		public bool Equals (TextSegment left, TextSegment right)
		{
			return left.Offset == right.Offset && left.Length == right.Length;
		}

		public bool Contains (int offset)
		{
			return Offset <= offset && offset < EndOffset;
		}

		public bool Contains (TextSegment segment)
		{
			return Offset <= segment.Offset && segment.EndOffset <= EndOffset;
		}

		public bool Equals (TextSegment other)
		{
			return this == other;
		}

		public override bool Equals (object obj)
		{
			return obj is TextSegment && this.Equals ((TextSegment)obj);
		}

		public override int GetHashCode ()
		{
			return this.Offset ^ this.Length;
		}

		public override string ToString ()
		{
			return string.Format ("[TextSegment: Offset={0}, Length={1}]", Offset, Length);
		}
	}
}
