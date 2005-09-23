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
using System.Collections;

namespace MonoDevelop.NUnit
{
	public class UnitTestCollection: CollectionBase
	{
		UnitTest owner;
		
		internal UnitTestCollection (UnitTest owner)
		{
			this.owner = owner;
		}
		
		public UnitTestCollection ()
		{
		}
		
		public new UnitTest this [int n] {
			get { return (UnitTest) List [n]; }
		}
		
		public new UnitTest this [string name] {
			get {
				for (int n=0; n<List.Count; n++)
					if (((UnitTest)List [n]).Name == name)
						return (UnitTest) List [n];
				return null;
			}
		}
		
		public void Add (UnitTest test)
		{
			((IList)this).Add (test);
		}
		
		public void CopyTo (UnitTest[] array, int index)
		{
			List.CopyTo (array, index);
		}
		
		protected override void OnInsert (int index, object value)
		{
			if (owner != null)
				((UnitTest)value).SetParent (owner);
		}
		
		protected override void OnSet (int index, object oldValue, object newValue)
		{
			if (owner != null) {
				((UnitTest)oldValue).SetParent (null);
				((UnitTest)newValue).SetParent (owner);
			}
		}
	}
}

