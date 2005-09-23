//
// GenericCollectionHandler.cs
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
using System.Reflection;

namespace MonoDevelop.Internal.Serialization
{
	internal abstract class GenericCollectionHandler: ICollectionHandler
	{
		protected Type type;
		protected Type elementType;
		protected MethodInfo addMethod;
		protected object[] itemParam = new object [1];
		bool hasPublicConstructor;
		
		protected GenericCollectionHandler (Type type, Type elemType, MethodInfo addMethod)
		{
			this.type = type;
			this.elementType = elemType;
			this.addMethod = addMethod;
			
			hasPublicConstructor = (type.GetConstructor (Type.EmptyTypes) != null);
		}
		
		public static ICollectionHandler CreateHandler (Type t)
		{
			Type elemType;
			
			MethodInfo addMethod = t.GetMethod ("Add");
			if (addMethod == null) return null;

			ParameterInfo[] pars = addMethod.GetParameters();
			if (pars.Length != 1) return null;
			elemType = pars[0].ParameterType;

			PropertyInfo indexerProp = null;
			PropertyInfo countProp = null;
			
			PropertyInfo[] props = t.GetProperties (BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo prop in props)
			{
				if (!prop.CanRead) continue;
				ParameterInfo[] pi = prop.GetIndexParameters ();
				if (prop.CanWrite && pi != null && pi.Length == 1 && pi[0].ParameterType == typeof(int))
					indexerProp = prop;
				else if (prop.Name == "Count" && prop.PropertyType == typeof(int))
					countProp = prop;
			}
			
			if (indexerProp != null && countProp != null && indexerProp.PropertyType == elemType)
				return new IndexedCollectionHandler (t, elemType, addMethod, indexerProp, countProp);

			if (!typeof(IEnumerable).IsAssignableFrom (t))
				return null;
			
			return new EnumerableCollectionHandler (t, elemType, addMethod, countProp);
		}

		public bool CanCreateInstance {
			get { return hasPublicConstructor; }
		}
		
		public Type GetItemType ()
		{
			return elementType;
		}
		
		public object CreateCollection (out object position, int size)
		{
			position = 0;
			return Activator.CreateInstance (type);
		}
		
		public void ResetCollection (object collection, out object position, int size)
		{
			position = 0;
		}
		
		public void AddItem (ref object collection, ref object position, object item)
		{
			itemParam [0] = item;
			addMethod.Invoke (collection, itemParam);
			position = (int)position + 1;
		}
		
		public abstract void SetItem (object collection, object position, object item);
		
		public void FinishCreation (ref object collection, object position)
		{
		}
		
		public abstract object GetInitialPosition (object collection);
		public abstract bool MoveNextItem (object collection, ref object position);
		public abstract object GetCurrentItem (object collection, object position);
		public abstract bool IsEmpty (object collection);
	}
	
	internal class IndexedCollectionHandler: GenericCollectionHandler
	{
		PropertyInfo indexer;
		PropertyInfo count;
		
		internal IndexedCollectionHandler (Type type, Type elemType, MethodInfo addMethod, PropertyInfo indexerProp, PropertyInfo countProp)
		: base (type, elemType, addMethod)
		{
			indexer = indexerProp;
			count = countProp;
		}
		
		public override void SetItem (object collection, object position, object item)
		{
			itemParam [0] = position;
			indexer.SetValue (collection, item, itemParam);
		}
		
		public override object GetInitialPosition (object collection)
		{
			return -1;
		}
		
		public override bool MoveNextItem (object collection, ref object position)
		{
			int i = ((int) position) + 1;
			position = i;
			return i < (int) count.GetValue (collection, null); 
		}
		
		public override object GetCurrentItem (object collection, object position)
		{
			itemParam [0] = position;
			return indexer.GetValue (collection, itemParam);
		}
		
		public override bool IsEmpty (object collection)
		{
			return collection == null || (int) count.GetValue (collection, null) == 0;
		}
	}
	
	internal class EnumerableCollectionHandler: GenericCollectionHandler
	{
		PropertyInfo count;
		
		internal EnumerableCollectionHandler (Type type, Type elemType, MethodInfo addMethod, PropertyInfo count)
			: base (type, elemType, addMethod)
		{
			this.count = count;
		}
		
		public override void SetItem (object collection, object position, object item)
		{
			AddItem (ref collection, ref position, item);
		}
		
		public override object GetInitialPosition (object collection)
		{
			return ((IEnumerable)collection).GetEnumerator ();
		}
		
		public override bool MoveNextItem (object collection, ref object position)
		{
			return ((IEnumerator)position).MoveNext ();
		}
		
		public override object GetCurrentItem (object collection, object position)
		{
			return ((IEnumerator)position).Current;
		}
		
		public override bool IsEmpty (object collection)
		{
			if (collection == null) return true;
			if (count != null) return (int) count.GetValue (collection, null) == 0;
			IEnumerator en = (IEnumerator) ((IEnumerable)collection).GetEnumerator ();
			return !en.MoveNext ();
		}
	}
}
