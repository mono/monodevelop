//
// ProjectedSegment.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Editor.Projection
{
	public struct ProjectedSegment 
	{
		public int Offset {
			get;
			private set;
		}

		public int ProjectedOffset {
			get;
			private set;
		}

		public int Length {
			get;
			private set;
		}

		public ProjectedSegment (int offset, int projectedOffset, int length)
			: this ()
		{
			this.Offset = offset;
			this.ProjectedOffset = projectedOffset;
			this.Length = length;
		}

		public bool ContainsOriginal (int offset)
		{
			return Offset <= offset && offset < Offset + Length;
		}

		public bool ContainsProjected (int offset)
		{
			return ProjectedOffset <= offset && offset <= ProjectedOffset + Length;
		}

		public bool IsInOriginal (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");

			return segment.Contains(Offset) && segment.Contains (Offset + Length); 
		}

		public ISegment FromOriginalToProjected (ISegment segment)
		{
			return new TextSegment (segment.Offset - Offset + ProjectedOffset, segment.Length);
		}

		public int FromOriginalToProjected (int offset)
		{
			return offset - Offset + ProjectedOffset;
		}

		public int FromProjectedToOriginal (int offset)
		{
			return offset + Offset - ProjectedOffset;
		}

		public override string ToString ()
		{
			return string.Format ("[ProjectedSegment: Offset={0}, ProjectedOffset={1}, Length={2}]", Offset, ProjectedOffset, Length);
		}
	}
}