//
// GroupComparer.cs
//
// Authors:
//  Helmut Duregger <helmutduregger@gmx.at>
//
// Copyright (c) 2011 Helmut Duregger
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
using System.Collections.Generic;

namespace MonoDevelop.DesignerSupport
{
	/// <summary>
	/// Compares items of type Group.
	/// </summary>
	public class GroupComparer : IComparer<Group>
	{
		/// <param name="a">
		/// A <see cref="Group"/> to compare.
		/// </param>
		/// <param name="b">
		/// A <see cref="Group"/> to compare.
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/> indicating which group is sorted higher up.
		/// </returns>
		public int Compare (Group a, Group b)
		{
			if (a.SortKey < b.SortKey) {
				return -1;
			} else if (a.SortKey == b.SortKey) {
				return 0;
			} else {
				return 1;
			}
		}
	}
}

