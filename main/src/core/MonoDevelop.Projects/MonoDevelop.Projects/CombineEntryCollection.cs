// CombineEntryCollection.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects
{
	public class CombineEntryCollection: IEnumerable
	{
		ArrayList list = new ArrayList ();
		Combine parentCombine;
		
		internal CombineEntryCollection ()
		{
		}
		
		internal CombineEntryCollection (Combine combine)
		{
			parentCombine = combine;
		}
		
		public int Count
		{
			get { return list.Count; }
		}
		
		public CombineEntry this [int n]
		{
			get { return (CombineEntry) list[n]; }
		}
		
		public CombineEntry this [string name]
		{
			get {
			for (int n=0; n<list.Count; n++)
				if (((CombineEntry)list[n]).Name == name)
					return (CombineEntry)list[n];
			return null;
			}
		}
		
		public IEnumerator GetEnumerator ()
		{
			return list.GetEnumerator ();
		}
		
		public void Add (CombineEntry entry)
		{
			list.Add (entry);
			if (parentCombine != null) {
				entry.SetParentCombine (parentCombine);
				parentCombine.NotifyEntryAdded (entry);
			}
		}
		
		public void Remove (CombineEntry entry)
		{
			list.Remove (entry);
			if (parentCombine != null) {
				entry.SetParentCombine (null);
				parentCombine.NotifyEntryRemoved (entry);
			}
		}
		
		internal void Replace (CombineEntry entry, CombineEntry newEntry)
		{
			int i = IndexOf (entry);
			list [i] = newEntry;
			if (parentCombine != null) {
				entry.SetParentCombine (null);
				newEntry.SetParentCombine (parentCombine);
			}

			// Don't notify the parent combine here since Replace is only
			// used internally when reloading entries
		}
		
		public int IndexOf (CombineEntry entry)
		{
			return list.IndexOf (entry);
		}
		
		public bool Contains (CombineEntry entry)
		{
			return IndexOf (entry) != -1;
		}
		
		public int IndexOf (string name)
		{
			for (int n=0; n<list.Count; n++)
				if (((CombineEntry)list[n]).Name == name)
					return n;
			return -1;
		}
		
		public void Clear ()
		{
			list.Clear ();
		}
		
		public void CopyTo (Array array)
		{
			list.CopyTo (array);
		}
		
		public void CopyTo (Array array, int index)
		{
			list.CopyTo (array, index);
		}
	}
}
