//
// ArrayListHandler.cs
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
	internal class ArrayListHandler: ICollectionHandler
	{
		public static ArrayListHandler Instance = new ArrayListHandler ();
		
		public Type GetItemType ()
		{
			return typeof(object);
		}
		
		public bool CanCreateInstance {
			get { return true; }
		}
		
		public object CreateCollection (out object position, int size)
		{
			position = 0;
			if (size != -1) return new ArrayList (size);
			else return new ArrayList ();
		}
		
		public void ResetCollection (object collection, out object position, int size)
		{
			position = 0;
		}
		
		public void AddItem (ref object collection, ref object position, object item)
		{
			((ArrayList) collection).Add (item);
			position = (int)position + 1;
		}
		
		public void SetItem (object collection, object position, object item)
		{
			((ArrayList) collection) [(int)position] = item;
		}
		
		public void FinishCreation (ref object collection, object position)
		{
		}
		
		public bool IsEmpty (object collection)
		{
			return collection == null || ((ArrayList)collection).Count == 0;
		}
		
		public object GetInitialPosition (object collection)
		{
			return -1;
		}
		
		public bool MoveNextItem (object collection, ref object position)
		{
			int i = (int) position;
			position = ++i;
			ArrayList ar = (ArrayList) collection;
			return i < ar.Count; 
		}
		
		public object GetCurrentItem (object collection, object position)
		{
			return ((ArrayList)collection) [(int)position];
		}
	}
}
