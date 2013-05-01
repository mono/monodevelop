//
// TreePathComparer.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.Collections.Generic;

using Gtk;

namespace MonoDevelop.Debugger
{
	public class TreePathComparer : IComparer<TreePath>
	{
		bool reversed;

		public TreePathComparer (bool reversed)
		{
			this.reversed = reversed;
		}

		#region IComparer implementation

		static int TreePathCompare (TreePath x, TreePath y)
		{
			int depth = Math.Min (x.Depth, y.Depth);
			int i;

			for (i = 0; i < depth; i++) {
				if (x.Indices[i] < y.Indices[i])
					return -1;
				if (x.Indices[i] > y.Indices[i])
					return 1;
			}

			if (x.Depth < y.Depth)
				return -1;
			if (x.Depth > y.Depth)
				return 1;

			return 0;
		}

		public int Compare (TreePath x, TreePath y)
		{
			return reversed ? TreePathCompare (y, x) : TreePathCompare (x, y);
		}

		#endregion
	}
}
