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

namespace MonoDevelop.Internal.Serialization
{
	public class DataCollection: IEnumerable
	{
		ArrayList list = new ArrayList ();
		
		public DataCollection ()
		{
		}
		
		protected ArrayList List {
			get {
				if (list == null) list = new ArrayList ();
				return list;
			}
		}
		
		public int Count
		{
			get { return list == null ? 0 : list.Count; }
		}
		
		public virtual DataNode this [int n]
		{
			get { return (DataNode) List[n]; }
		}
		
		public virtual DataNode this [string name]
		{
			get {
				DataCollection col;
				int i = FindData (name, out col, false);
				if (i != -1) return (DataNode) col.List [i];
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
					DataNode data = (DataNode) list [n];
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
						data = (DataNode) colec.List [n];
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
		
		public virtual void Add (DataNode entry)
		{
			if (entry == null)
				throw new ArgumentNullException ("entry");
				
			List.Add (entry);
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
				DataNode data = (DataNode) col.List [i];
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
	}
}
