// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.Internal.Project
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
	}
}
