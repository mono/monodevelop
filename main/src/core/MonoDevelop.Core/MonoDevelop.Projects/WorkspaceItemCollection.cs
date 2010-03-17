// WorkspaceItemCollection.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MonoDevelop.Projects
{
	public class WorkspaceItemCollection: Collection<WorkspaceItem>
	{
		Workspace parent;
		
		public WorkspaceItemCollection ()
		{
		}
		
		internal WorkspaceItemCollection (Workspace parent)
		{
			this.parent = parent;
		}
		
		public WorkspaceItem[] ToArray ()
		{
			WorkspaceItem[] arr = new WorkspaceItem [Count];
			CopyTo (arr, 0);
			return arr;
		}
		
		internal void Replace (WorkspaceItem item, WorkspaceItem newItem)
		{
			int i = IndexOf (item);
			Items [i] = newItem;
			if (parent != null) {
				item.ParentWorkspace = null;
				newItem.ParentWorkspace = parent;
			}

			// Don't notify the parent workspace here since Replace is only
			// used internally when reloading items
		}
		
		protected override void ClearItems ()
		{
			if (parent != null) {
				List<WorkspaceItem> items = new List<WorkspaceItem> (this);
				foreach (WorkspaceItem it in items) {
					it.ParentWorkspace = null;
					parent.NotifyItemRemoved (new WorkspaceItemChangeEventArgs (it, false));
				}
			}
			else
				base.ClearItems ();
		}
		
		protected override void InsertItem (int index, WorkspaceItem item)
		{
			base.InsertItem (index, item);
			if (parent != null) {
				item.ParentWorkspace = parent;
				parent.NotifyItemAdded (new WorkspaceItemChangeEventArgs (item, false));
			}
		}
		
		protected override void RemoveItem (int index)
		{
			WorkspaceItem item = this [index];
			base.RemoveItem (index);
			if (parent != null) {
				item.ParentWorkspace = parent;
				parent.NotifyItemRemoved (new WorkspaceItemChangeEventArgs (item, false));
			}
		}
		
		protected override void SetItem (int index, WorkspaceItem item)
		{
			WorkspaceItem oldItem = this [index];
			base.SetItem (index, item);
			if (parent != null) {
				item.ParentWorkspace = parent;
				parent.NotifyItemRemoved (new WorkspaceItemChangeEventArgs (oldItem, false));
				parent.NotifyItemAdded (new WorkspaceItemChangeEventArgs (item, false));
			}
		}
	}
}
