//
// ConfigurationCollection.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public interface IItemConfigurationCollection: ICollection<ItemConfiguration>
	{
		ItemConfiguration this [string name] { get; }
	}
	
	public class ItemConfigurationCollection<T> : ItemCollection<T>, IItemConfigurationCollection where T: ItemConfiguration
	{
		public T this [string name] {
			get {
				foreach (T c in this)
					if (c.Id == name)
						return c;
				return null;
			}
		}
		
		public void Remove (string name)
		{
			for (int n=0; n<Count; n++) {
				if (this [n].Id == name) {
					RemoveAt (n);
					return;
				}
			}
		}

#region IItemConfigurationCollection implementation
		
		ItemConfiguration IItemConfigurationCollection.this [string name] {
			get {
				return this [name];
			}
		}

		bool ICollection<ItemConfiguration>.IsReadOnly {
			get {
				return false;
			}
		}

		IEnumerator<ItemConfiguration> IEnumerable<ItemConfiguration>.GetEnumerator ()
		{
			foreach (ItemConfiguration item in this)
				yield return item;
		}

		void ICollection<ItemConfiguration>.Add (ItemConfiguration item)
		{
			Add ((T)item);
		}

		void ICollection<ItemConfiguration>.Clear ()
		{
			Clear ();
		}

		bool ICollection<ItemConfiguration>.Contains (ItemConfiguration item)
		{
			return Contains ((T)item);
		}

		void ICollection<ItemConfiguration>.CopyTo (ItemConfiguration[] array, int arrayIndex)
		{
			for (int n=0; n<Count; n++)
				array [arrayIndex + n] = this [n];
		}

		bool ICollection<ItemConfiguration>.Remove (ItemConfiguration item)
		{
			return Remove ((T)item);
		}
		
#endregion
		
		protected override void OnItemAdded (T conf)
		{
			if (ConfigurationAdded != null)
				ConfigurationAdded (this, new ConfigurationEventArgs (null, conf));
		}
		
		protected override void OnItemRemoved (T conf)
		{
			if (ConfigurationRemoved != null)
				ConfigurationRemoved (this, new ConfigurationEventArgs (null, conf));
		}
		
		public event ConfigurationEventHandler ConfigurationAdded;
		public event ConfigurationEventHandler ConfigurationRemoved;
	}
}
