// List<VersionControlItem>.cs
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
	public static class VersionControlItemListExtensions
	{
		public static List<VersionControlItem>[] SplitByRepository (this IList<VersionControlItem> items)
		{
			var t = new Dictionary<Repository, List<VersionControlItem>> ();
			foreach (VersionControlItem it in items) {
				List<VersionControlItem> list;
				if (!t.TryGetValue (it.Repository, out list)) {
					list = new List<VersionControlItem> ();
					t [it.Repository] = list;
				}
				list.Add (it);
			}

			return t.Values.ToArray ();
		}

		public static List<VersionControlItem> GetFiles (this IList<VersionControlItem> items)
		{
			var paths = new List<VersionControlItem> ();
			foreach (VersionControlItem it in items) {
				if (!it.IsDirectory)
					paths.Add (it);
			}
			return paths;
		}

		public static List<VersionControlItem> GetDirectories (this IList<VersionControlItem> items)
		{
			var paths = new List<VersionControlItem> ();
			foreach (VersionControlItem it in items) {
				if (it.IsDirectory)
					paths.Add (it);
			}
			return paths;
		}

		public static FilePath[] GetPaths (this IList<VersionControlItem> items)
		{
			return items.Select (v => v.Path).ToArray ();
		}

		// Finds the most specific ancestor path of a set of version control items.
		// Returns FilePath.Null if no parent is found.
		public static FilePath FindMostSpecificParent (this IList<VersionControlItem> items)
		{
			return FilePath.GetCommonRootPath (items.GetPaths ());
		}
	}
}
