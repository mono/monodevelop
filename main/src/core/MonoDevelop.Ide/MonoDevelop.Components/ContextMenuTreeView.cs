// 
// ContextMenuTreeView.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using Mono.TextEditor;

namespace MonoDevelop.Components
{
	/// <summary>
	/// TreeView with context menu support.
	/// </summary>
	public class ContextMenuTreeView : Gtk.TreeView
	{
		const Gdk.ModifierType selectionModifiers = Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask;
		
		public ContextMenuTreeView ()
		{
		}
		
		public ContextMenuTreeView (Gtk.TreeModel model) : base (model)
		{
		}
		
		public Action<Gdk.EventButton> DoPopupMenu { get; set; }
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (!evnt.TriggersContextMenu ()) {
				return base.OnButtonPressEvent (evnt);
			}

			//pass click to base it it can update the selection
			//unless the node is already selected, in which case we don't want to change the selection
			bool res = false;
			if (!IsClickedNodeSelected ((int)evnt.X, (int)evnt.Y)) {
				res = base.OnButtonPressEvent (evnt);
			}
			
			if (DoPopupMenu != null) {
				DoPopupMenu (evnt);
				return true;
			}
			
			return res;
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			bool res = base.OnButtonReleaseEvent (evnt);
			
			if (DoPopupMenu != null && evnt.IsContextMenuButton ()) {
				return true;
			}
			
			return res;
		}
		
		protected override bool OnPopupMenu ()
		{
			if (DoPopupMenu != null) {
				DoPopupMenu (null);
				return true;
			}
			return base.OnPopupMenu ();
		}
		
		bool IsClickedNodeSelected (int x, int y)
		{
			Gtk.TreePath path;
			if (this.GetPathAtPos (x, y, out path))
				return this.Selection.PathIsSelected (path);
			else
				return false;
		}
		
		bool MultipleNodesSelected ()
		{
			return this.Selection.GetSelectedRows ().Length > 1;
		}
	}
}