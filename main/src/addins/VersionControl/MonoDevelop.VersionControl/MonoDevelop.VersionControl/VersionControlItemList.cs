// VersionControlItemList.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
//

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	public class VersionControlItemList: List<VersionControlItem>
	{
		public VersionControlItemList[] SplitByRepository ()
		{
			Dictionary<Repository, VersionControlItemList> t = new Dictionary<Repository, VersionControlItemList> ();
			foreach (VersionControlItem it in this) {
				VersionControlItemList list;
				if (!t.TryGetValue (it.Repository, out list)) {
					list = new VersionControlItemList ();
					t [it.Repository] = list;
				}
				list.Add (it);
			}

			return t.Values.ToArray ();
		}

		public VersionControlItemList GetFiles ()
		{
			VersionControlItemList paths = new VersionControlItemList ();
			foreach (VersionControlItem it in this) {
				if (!it.IsDirectory)
					paths.Add (it);
			}
			return paths;
		}

		public VersionControlItemList GetDirectories ()
		{
			VersionControlItemList paths = new VersionControlItemList ();
			foreach (VersionControlItem it in this) {
				if (it.IsDirectory)
					paths.Add (it);
			}
			return paths;
		}

		public FilePath[] Paths {
			get {
				return this.Select (v => v.Path).ToArray ();
			}
		}

		// Finds the most specific ancestor path of a set of version control items.
		// Returns FilePath.Null if no parent is found.
		public FilePath FindMostSpecificParent ()
		{
			return FilePath.GetCommonRootPath (Paths);
		}
	}
}
