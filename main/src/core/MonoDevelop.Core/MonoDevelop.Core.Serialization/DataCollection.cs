//
// DataCollection.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Core.Serialization
{
	public class DataCollection: IEnumerable
	{
		List<DataNode> list = new List<DataNode> ();
		
		public DataCollection ()
		{
		}
		
		protected List<DataNode> List {
			get {
				if (list == null)
					list = new List<DataNode> ();
				return list;
			}
		}
		
		public int Count
		{
			get { return list == null ? 0 : list.Count; }
		}
		
		public virtual DataNode this [int n]
		{
			get { return List[n]; }
		}
		
		public virtual DataNode this [string name]
		{
			get {
				DataCollection col;
				int i = FindData (name, out col, false);
				if (i != -1) return col.List [i];
				else return null;
			}
		}
		
		int FindData (string name, out DataCollection colec, bool buildTree)
		{
			if (list == null) {
				colec = null;
				return -1;
			}
			
			if (name.IndexOf ('/') == -1) {
				for (int n=0; n<list.Count; n++) {
					DataNode data = list [n];
					if (data.Name == name) {
						colec = this;
						return n;
					}
				}
				colec = this;
				return -1;
			} else {
				string[] names = name.Split ('/');
				int pos = -1;
				colec = this;
				DataNode data = null;

				for (int p=0; p<names.Length; p++)
				{
					if (p > 0) {
						DataItem item = data as DataItem;
						if (item != null) {
							colec = item.ItemData;
						} else if (buildTree) {
							item = new DataItem ();
							item.Name = names [p - 1];
							colec.Add (item);
							colec = item.ItemData;
						} else {
							colec = null;
							return -1;
						}
					}
					
					pos = -1;
					for (int n=0; n<colec.List.Count; n++) {
						data = colec.List [n];
						if (data.Name == names [p]) {
							pos = n; break;
						}
					}
				}
				return pos;
			}
		}
		
		public virtual IEnumerator GetEnumerator ()
		{
			return list == null ? Type.EmptyTypes.GetEnumerator() : list.GetEnumerator ();
		}
		
		public void AddRange (DataCollection col)
		{
			foreach (DataNode node in col)
				Add (node);
		}
		
		public virtual void Add (DataNode entry)
		{
			if (entry == null)
				throw new ArgumentNullException ("entry");
				
			List.Add (entry);
		}
		
		public virtual void Insert (int index, DataNode entry)
		{
			if (entry == null)
				throw new ArgumentNullException ("entry");

			List.Insert (index, entry);
		}
		
		public virtual void Add (DataNode entry, string itemPath)
		{
			if (entry == null)
				throw new ArgumentNullException ("entry");
				
			DataCollection col;
			FindData (itemPath + "/", out col, true);
			col.List.Add (entry);
		}
		
		public virtual void Remove (DataNode entry)
		{
			if (list != null)
				list.Remove (entry);
		}
		
		public DataNode Extract (string name)
		{
			DataCollection col;
			int i = FindData (name, out col, false);
			if (i != -1) {
				DataNode data = col.List [i];
				col.list.RemoveAt (i);
				return data;
			}
			return null;
		}
		
		public int IndexOf (DataNode entry)
		{
			if (list == null) return -1;
			return list.IndexOf (entry);
		}
		
		public virtual void Clear ()
		{
			if (list != null)
				list.Clear ();
		}
		
		public void Merge (DataCollection col)
		{
			ArrayList toAdd = new ArrayList ();
			
			foreach (DataNode node in col) {
				DataNode current = this [node.Name];
				if (current == null)
					toAdd.Add (node);
				else if (node is DataItem && current is DataItem) {
					((DataItem)current).ItemData.Merge (((DataItem)node).ItemData);
				}
			}
			foreach (DataNode node in toAdd)
				Add (node);
		}
		
		// Sorts the list using the specified key order
		public void Sort (Dictionary<string,int> nameToPosition)
		{
			list.Sort (delegate (DataNode x, DataNode y) {
				int p1, p2;
				if (!nameToPosition.TryGetValue (x.Name, out p1))
					p1 = int.MaxValue;
				if (!nameToPosition.TryGetValue (y.Name, out p2))
					p2 = int.MaxValue;
				return p1.CompareTo (p2);
			});
		}
	}
}
