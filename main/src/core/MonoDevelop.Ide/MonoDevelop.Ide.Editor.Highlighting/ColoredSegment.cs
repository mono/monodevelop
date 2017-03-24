//
// ColoredSegment.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	/// <summary>
	/// A colored segment is used in the highlighter to specify a color scheme style to a specfic part of text.
	/// </summary>
	public sealed class ColoredSegment : AbstractSegment
	{
		readonly ScopeStack scopeStack;

		/// <summary>
		/// Gets the color style. The style is looked up in the current color scheme.
		/// </summary>
		public string ColorStyleKey {
			get {
				return scopeStack.IsEmpty ? "" : scopeStack.Peek ();
			}
		}

		internal ScopeStack ScopeStack {
			get {
				return scopeStack;
			}
		}

		public ColoredSegment (int offset, int length, ScopeStack scopeStack) : base (offset, length)
		{
			this.scopeStack = scopeStack;
		}

		public ColoredSegment (ISegment segment, ScopeStack scopeStack) : base (segment)
		{
			this.scopeStack = scopeStack;
		}

		public ColoredSegment WithOffsetAndLength (int offset, int length)
		{
			return new ColoredSegment (offset, length, scopeStack);
		}

		public ColoredSegment WithOffset (int offset)
		{
			return new ColoredSegment (offset, Length, scopeStack);
		}

		public ColoredSegment WithLength (int length)
		{
			return new ColoredSegment (Offset, length, scopeStack);
		}

		public override string ToString ()
		{
			return string.Format ("[ColoredSegment: Offset={0}, Length={1},ColorStyleKey={2}]", Offset, Length, ColorStyleKey);
		}

}
}