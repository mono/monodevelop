//
// TreeNodeCollection.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

namespace Mono.Addins
{
	class TreeNodeCollection: IEnumerable
	{
		ArrayList list;
		
		internal static TreeNodeCollection Empty = new TreeNodeCollection (null);
		
		public TreeNodeCollection (ArrayList list)
		{
			this.list = list;
		}
		
		public IEnumerator GetEnumerator ()
		{
			if (list != null)
				return list.GetEnumerator ();
			else
				return Type.EmptyTypes.GetEnumerator ();
		}
		
		public TreeNode this [int n] {
			get { 
				if (list != null)
					return (TreeNode) list [n];
				else
					throw new System.IndexOutOfRangeException ();
			}
		}
		
		public int IndexOfNode (string id)
		{
			for (int n=0; n<Count; n++) {
				if (this [n].Id == id)
					return n;
			}
			return -1;
		}
		
		public int Count {
			get { return list != null ? list.Count : 0; }
		}
		
		public TreeNodeCollection Clone ()
		{
			if (list != null)
				return new TreeNodeCollection ((ArrayList) list.Clone ());
			else
				return Empty;
		}
	}
}
