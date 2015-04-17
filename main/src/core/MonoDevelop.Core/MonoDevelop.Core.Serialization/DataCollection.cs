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
using System.Collections.ObjectModel;

namespace MonoDevelop.Core.Serialization
{
	[Serializable]
	public sealed class DataCollection: Collection<DataNode>
	{
		public DataNode this [string name]
		{
			get {
				DataCollection col;
				int i = FindData (name, out col, false);
				if (i != -1) return col [i];
				else return null;
			}
		}
		
		int FindData (string name, out DataCollection colec, bool buildTree)
		{
			if (name.IndexOf ('/') == -1) {
				for (int n=0; n<Items.Count; n++) {
					DataNode data = Items [n];
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
					for (int n=0; n<colec.Count; n++) {
						data = colec [n];
						if (data.Name == names [p]) {
							pos = n; break;
						}
					}
				}
				return pos;
			}
		}
		
		public void Add (DataNode entry, string itemPath)
		{
			if (entry == null)
				throw new ArgumentNullException ("entry");
				
			DataCollection col;
			FindData (itemPath + "/", out col, true);
			Add (entry);
		}
		
		public DataNode Extract (string name)
		{
			DataCollection col;
			int i = FindData (name, out col, false);
			if (i != -1) {
				DataNode data = col [i];
				col.RemoveAt (i);
				return data;
			}
			return null;
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
	}
}
