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

namespace MonoDevelop.Components
{
	/// <summary>
	/// TreeView with context menu support.
	/// </summary>
	public class ContextMenuTreeView : Gtk.TreeView
	{
		public ContextMenuTreeView ()
		{
		}

		public ContextMenuTreeView (Gtk.TreeModel model) : base (model)
		{
		}

		public Action<Gdk.EventButton> DoPopupMenu { get; set; }

		Gtk.TreePath buttonPressPath;
		bool selectOnRelease;

		// Workaround for Bug 31712 - Solution pad doesn't refresh properly after resizing application window
		// If the treeview size is modified while the pad is unrealized (autohidden), the treeview
		// doesn't update its internal vertical offset. This can lead to items becoming offset outside the 
		// visible area and therefore becoming unreachable. The only way to force the treeview to recalculate
		// this offset is by setting the Vadjustment.Value, but it ignores values the same as the current value.
		// Therefore we simply set it to something slightly different then back again.

		// See also MonoDevelop.Ide.Gui.Components.PadTreeView for same fix.
		bool forceInternalOffsetUpdate;

		protected override void OnUnrealized ()
		{
			base.OnUnrealized ();
			forceInternalOffsetUpdate = true;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (forceInternalOffsetUpdate && IsRealized) {
				forceInternalOffsetUpdate = false;
				var v = Vadjustment.Value;
				int delta = v > 2? 0 : 1;
				Vadjustment.Value = v + delta;
				Vadjustment.Value = v;
			}
		}

		protected override void OnDragBegin (Gdk.DragContext context)
		{
			//If user starts dragging don't do any selection
			//useful in case user press Esc to abort dragging and
			//didn't release mouse button yet
			selectOnRelease = false;
			base.OnDragBegin (context);
		}

		protected override void OnRowActivated (Gtk.TreePath path, Gtk.TreeViewColumn column)
		{
			// This is to work around an issue in ContextMenuTreeView, when we set the
			// SelectFunction to block selection then it doesn't seem to always get
			// properly unset.
			//   https://bugzilla.xamarin.com/show_bug.cgi?id=40469
			this.Selection.SelectFunction = (s, m, p, b) => {
				return true;
			};
			base.OnRowActivated (path, column);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			selectOnRelease = false;
			if (!evnt.TriggersContextMenu ()) {
				//Because we are blocking selection changes with SelectFunction base.OnButtonPressEvent
				//can be called so expanders work. Another good effect is when expander is clicked
				//SelectFunction is not called so selectOnRelease remains false.
				//Which means no selection operation is performed in OnButtonReleaseEvent.
				//When Shift is pressed we don't do our magic becasue:
				//a) it works as expected((item is still selected when dragging starts
				//(it's by nature of Shift selecting))
				//b) we would have to simulate Shift selecting in OnButtonReleaseEvent
				//which would mean we have to implement all selecting logic...
				//Also notice that our magic is requiered only when item is selected.
				if (GetPathAtPos ((int)evnt.X, (int)evnt.Y, out buttonPressPath) &&
				    ((evnt.State & Gdk.ModifierType.ShiftMask) == 0) &&
				    Selection.PathIsSelected (buttonPressPath)) {
					this.Selection.SelectFunction = (s, m, p, b) => {
						selectOnRelease = true;
						//Always returning false means we are blocking base.OnButtonPressEvent
						//from doing any changes to selectiong we will do changes in OnButtonReleaseEvent
						return false;
					};
				} else {
					this.Selection.SelectFunction = (s, m, p, b) => {
						return true;
					};
				}
				return base.OnButtonPressEvent (evnt);
			}
			//pass click to base so it can update the selection
			//unless the node is already selected, in which case we don't want to change the selection(deselect multi selection)
			bool res = false;
			if (!this.IsClickedNodeSelected ((int)evnt.X, (int)evnt.Y)) {
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
			this.Selection.SelectFunction = (s, m, p, b) => {
				return true;
			};
			Gtk.TreePath buttonReleasePath;
			//If OnButtonPressEvent attempted on making deselection and dragging was not started
			//check if we are on same item as when we clicked(could be different if dragging is disabled)
			if (selectOnRelease &&
			    GetPathAtPos ((int)evnt.X, (int)evnt.Y, out buttonReleasePath) &&
			    buttonPressPath.Compare (buttonReleasePath) == 0) {

				//Simulate what would happen in OnButtonPressEvent if we were not blocking selection
				//notice that item is currently 100% selected since this check was performed in OnButtonPressEvent
				if (Selection.Mode == Gtk.SelectionMode.Multiple &&
				    (evnt.State & Gdk.ModifierType.ControlMask) > 0) {
					Selection.UnselectPath (buttonReleasePath);
				} else {
					//UnselectAll in case multiple were selected we want only our item to be selected now
					//if it was clicked but not dragged
					Selection.UnselectAll ();
					Selection.SelectPath (buttonReleasePath);
				}
				buttonPressPath = null;
			}
			selectOnRelease = false;

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

		bool MultipleNodesSelected ()
		{
			return Selection.GetSelectedRows ().Length > 1;
		}
	}
}