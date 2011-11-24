// DragNotebook.cs
//
// Author:
//   Todd Berman  <tberman@off.net>
//
// Copyright (c) 2004 Todd Berman
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

using Gdk;
using Gtk;
using System;
using Mono.TextEditor;

namespace MonoDevelop.Ide.Gui
{

	public delegate void TabsReorderedHandler (Widget widget, int oldPlacement, int newPlacement);

	public class DragNotebook : Notebook
    {
		public DragNotebook () {
			ButtonPressEvent += new ButtonPressEventHandler (OnButtonPress);
			ButtonReleaseEvent += new ButtonReleaseEventHandler (OnButtonRelease);
			AddEvents ((Int32) (EventMask.AllEventsMask));
			
			//FIXME: we make the tabs smaller by shrinking the border, but this looks ugly with the windows theme
			if (!MonoDevelop.Core.Platform.IsWindows)
				this.SetProperty ("tab-border", new GLib.Value (0));
		}
		
		Cursor fleurCursor = new Cursor (CursorType.Fleur);

		public event TabsReorderedHandler TabsReordered;

		bool DragInProgress;

		public int FindTabAtPosition (double cursorX, double cursorY) {

			int    dragNotebookXRoot;
			int    dragNotebookYRoot;
			Widget page;
			int    pageNumber        = CurrentPage;
			Widget tab;
			int    tabMaxX;
			int    tabMaxY;
			int    tabMinX;
			int    tabMinY;
			int? direction = null;

			ParentWindow.GetOrigin (out dragNotebookXRoot, out dragNotebookYRoot);
			
			// We cannot rely on the allocations being zero for tabs which are
			// offscreen. If we write the logic to walk from page 0 til NPages,
			// we can end up choosing the wrong page because pages which are
			// offscreen will match the mouse coordinates. We can work around
			// this by walking either up or down from the active page and choosing
			// the first page which is within the mouse x/y coordinates.
			while ((page = GetNthPage (pageNumber)) != null && pageNumber >= 0 && pageNumber <= NPages) {

				if ((tab = GetTabLabel (page)) == null)
					return -1;

				tabMinX = dragNotebookXRoot + tab.Allocation.X;
				tabMaxX = tabMinX + tab.Allocation.Width;

				tabMinY = dragNotebookYRoot + tab.Allocation.Y;
				tabMaxY = tabMinY + tab.Allocation.Height;

				if ((tabMinX <= cursorX) && (cursorX <= tabMaxX) &&
					(tabMinY <= cursorY) && (cursorY <= tabMaxY))
					return pageNumber;

				if (!direction.HasValue) {
					if (TabPos == PositionType.Top || TabPos == PositionType.Bottom)
						direction = cursorX > tabMaxX ? 1 : -1;
					else
						direction = cursorY > tabMaxY ? 1 : -1;
				}

				pageNumber += direction.Value;
			}

			return -1;
		}

		void MoveTab (int destinationPage)
		{
			if (destinationPage >= 0 && destinationPage != CurrentPage) {
				int oldPage = CurrentPage;
				ReorderChild (CurrentPageWidget, destinationPage);

				if (TabsReordered != null)
					TabsReordered (CurrentPageWidget, oldPage, destinationPage);
			}
		}

		[GLib.ConnectBefore]
		void OnButtonPress (object obj, ButtonPressEventArgs args) {

			if (DragInProgress || args.Event.TriggersContextMenu ())
				return;

			if (args.Event.Button == 1 && args.Event.Type == EventType.ButtonPress && FindTabAtPosition (args.Event.XRoot, args.Event.YRoot) >= 0)
				MotionNotifyEvent += new MotionNotifyEventHandler (OnMotionNotify);
		}
		
		public void LeaveDragMode (uint time)
		{
			if (DragInProgress) {
				Pointer.Ungrab (time);
				Grab.Remove (this);
			}
			MotionNotifyEvent -= new MotionNotifyEventHandler (OnMotionNotify);
			DragInProgress = false;
		}
		
		[GLib.ConnectBefore]
		void OnButtonRelease (object obj, ButtonReleaseEventArgs args) {
			LeaveDragMode (args.Event.Time);
		}


		[GLib.ConnectBefore]
		void OnMotionNotify (object obj, MotionNotifyEventArgs args) {

			if (!DragInProgress) {
				DragInProgress = true;
				Grab.Add (this);

				if (!Pointer.IsGrabbed)
					Pointer.Grab (ParentWindow, false, EventMask.Button1MotionMask | EventMask.ButtonReleaseMask, null, fleurCursor, args.Event.Time);	
			}

			MoveTab (FindTabAtPosition (args.Event.XRoot, args.Event.YRoot));
		}
		
		protected override void OnDestroyed ()
		{
			if (fleurCursor != null) {
				fleurCursor.Dispose ();
				fleurCursor = null;
			}
			base.OnDestroyed ();
		}
	}
}
