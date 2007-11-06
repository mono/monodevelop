//  CombineEntryCollection.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects
{
	public interface ICombineEntryCollection: IEnumerable
	{
		int Count { get; }
		CombineEntry this [int n] { get; }
	}
	
	public class CombineEntryCollection: ICombineEntryCollection
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
