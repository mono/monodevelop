//
// UnitTestCollection.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MonoDevelop.UnitTesting
{
	public class UnitTestCollection: Collection<UnitTest>
	{
		UnitTestGroup owner;
		
		internal UnitTestCollection (UnitTestGroup owner)
		{
			this.owner = owner;
		}
		
		public UnitTestCollection ()
		{
		}
		
		public UnitTest this [string name] {
			get {
				for (int n=0; n<Items.Count; n++)
					if (Items [n].Name == name)
						return Items [n];
				return null;
			}
		}
		
		protected override void SetItem (int index, UnitTest item)
		{
			if (owner != null)
				this[index].SetParent (null);
			base.SetItem (index, item);
			if (owner != null)
				item.SetParent (owner);
		}
		
		protected override void RemoveItem (int index)
		{
			if (owner != null)
				this [index].SetParent (null);
			base.RemoveItem(index);
		}

		protected override void InsertItem (int index, UnitTest item)
		{
			base.InsertItem(index, item);
			if (owner != null)
				item.SetParent (owner);
		}

		protected override void ClearItems ()
		{
			if (owner != null) {
				foreach (UnitTest t in this)
					t.SetParent (null);
			}
			base.ClearItems();
		}
	}
}

