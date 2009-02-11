// 
// ProjectItemCollection.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public class ProjectItemCollection: ProjectItemCollection<ProjectItem>
	{
		public ProjectItemCollection ()
		{
		}
		
		internal ProjectItemCollection (SolutionEntityItem parent): base (parent)
		{
		}
	}

	internal interface IItemListHandler
	{
		void InternalAdd (object item, bool comesFromParent);
		void InternalRemove (object item, bool comesFromParent);
		bool CanHandle (object item);
	}
	
	public class ProjectItemCollection<T>: ItemCollection<T>, IItemListHandler where T: ProjectItem
	{
		SolutionEntityItem parent;
		IItemListHandler parentCollection;
		List<IItemListHandler> subCollections;
		
		internal ProjectItemCollection (SolutionEntityItem parent)
		{
			this.parent = parent;
		}
		
		public ProjectItemCollection ()
		{
		}
		
		public void AddRange (IEnumerable<T> items)
		{
			foreach (T item in items)
				Add (item);
		}
		
		public void Bind<U> (ProjectItemCollection<U> subCollection) where U:T
		{
			if (subCollections == null)
				subCollections = new List<IItemListHandler> ();
			subCollections.Add (subCollection);
			subCollection.parentCollection = this;
			IItemListHandler list = subCollection;
			foreach (object ob in this) {
				if (list.CanHandle (ob))
					list.InternalAdd (ob, true);
			}
		}
		
		public void Unbind<U> (ProjectItemCollection<U> subCollection) where U:T
		{
			if (subCollections != null) {
				subCollections.Remove (subCollection);
				subCollection.parentCollection = null;
			}
		}
		
		public IEnumerable<U> GetAll<U> () where U:T
		{
			foreach (T it in this) {
				if (it is U)
					yield return (U) it;
			}
		}
		
		protected override void OnItemAdded (T item)
		{
			NotifyAdded (item, true);
			NotifyAdded (item, false);
		}
		
		protected override void OnItemRemoved (T item)
		{
			NotifyRemoved (item, true);
			NotifyRemoved (item, false);
		}
		
		void IItemListHandler.InternalAdd (object obj, bool comesFromParent)
		{
			Items.Add ((T) obj);
			NotifyAdded (obj, comesFromParent);
		}
		
		void IItemListHandler.InternalRemove (object obj, bool comesFromParent)
		{
			Items.Remove ((T) obj);
			NotifyRemoved (obj, comesFromParent);
		}
		
		bool IItemListHandler.CanHandle (object obj)
		{
			return obj is T;
		}
		
		void NotifyAdded (object item, bool comesFromParent)
		{
			if (comesFromParent) {
				if (subCollections != null) {
					foreach (IItemListHandler col in subCollections) {
						if (col.CanHandle (item))
							col.InternalAdd (item, true);
					}
				}
			} else {
				if (parentCollection != null)
					parentCollection.InternalAdd (item, false);
				if (parent != null)
					parent.OnItemAdded (item);
			}
		}
		
		void NotifyRemoved (object item, bool comesFromParent)
		{
			if (comesFromParent) {
				if (subCollections != null) {
					foreach (IItemListHandler col in subCollections) {
						if (col.CanHandle (item))
							col.InternalRemove (item, true);
					}
				}
			} else {
				if (parentCollection != null)
					parentCollection.InternalRemove (item, false);
				if (parent != null)
					parent.OnItemRemoved (item);
			}
		}
	}
}
