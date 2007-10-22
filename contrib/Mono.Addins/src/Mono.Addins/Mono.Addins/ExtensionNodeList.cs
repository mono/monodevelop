//
// ExtensionNodeList.cs
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
	public class ExtensionNodeList: IEnumerable
	{
		internal ArrayList list;
		
		internal static ExtensionNodeList Empty = new ExtensionNodeList (new ArrayList ());
		
		internal ExtensionNodeList (ArrayList list)
		{
			this.list = list;
		}
		
		public ExtensionNode this [int n] {
			get {
				if (list == null)
					throw new System.IndexOutOfRangeException ();
				else
					return (ExtensionNode) list [n];
			}
		}
		
		public ExtensionNode this [string id] {
			get {
				if (list == null)
					return null;
				else {
					for (int n = list.Count - 1; n >= 0; n--)
						if (((ExtensionNode) list [n]).Id == id)
							return (ExtensionNode) list [n];
					return null;
				}
			}
		}
		
		public IEnumerator GetEnumerator () 
		{
			return list.GetEnumerator ();
		}
		
		public int Count {
			get { return list == null ? 0 : list.Count; }
		}
		
		public void CopyTo (Array array, int index)
		{
			list.CopyTo (array, index);
		}
	}
}
