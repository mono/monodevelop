//
// ArrayHandler.cs
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
using System.Reflection;

namespace MonoDevelop.Internal.Serialization
{
	internal class ArrayHandler: ICollectionHandler
	{
		Type _elementType;
		
		public ArrayHandler (Type type)
		{
			_elementType = type.GetElementType (); 
		}
		
		public Type GetItemType ()
		{
			return _elementType;
		}
		
		public bool CanCreateInstance {
			get { return true; }
		}
		
		public object CreateCollection (out object position, int size)
		{
			position = 0;
			return Array.CreateInstance (_elementType, size != -1 ? size : 5);
		}
		
		public void ResetCollection (object collection, out object position, int size)
		{
			throw new InvalidOperationException ("Array instance could not be reused.");
		}
		
		public void AddItem (ref object collection, ref object position, object item)
		{
			int i = (int)position;
			Array ar = (Array) collection;
			if (i >= ar.Length) {
				Array newArray = Array.CreateInstance (_elementType, ar.Length + 5);
				Array.Copy (ar, newArray, ar.Length);
				collection = newArray;
				newArray.SetValue (item, i);
			}
			else
				ar.SetValue (item, i);
			position = i + 1;
		}
		
		public void SetItem (object collection, object position, object item)
		{
			int i = (int)position;
			((Array) collection).SetValue (item, i);
		}
		
		public void FinishCreation (ref object collection, object position)
		{
			int i = (int)position;
			Array ar = (Array) collection;
			if (i < ar.Length) {
				Array newArray = Array.CreateInstance (_elementType, i);
				Array.Copy (ar, newArray, i);
				collection = newArray;
			}
		}
		
		public bool IsEmpty (object collection)
		{
			return collection == null || ((Array)collection).Length == 0;
		}
		
		public object GetInitialPosition (object collection)
		{
			return -1;
		}
		
		public bool MoveNextItem (object collection, ref object position)
		{
			int i = (int) position;
			position = ++i;
			Array ar = (Array) collection;
			return i < ar.Length; 
		}
		
		public object GetCurrentItem (object collection, object position)
		{
			return ((Array)collection).GetValue ((int)position);
		}
	}
}
