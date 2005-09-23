using Gdk;
using Gtk;
using System;

namespace MonoDevelop.Gui.Widgets
{

	public delegate void TabsReorderedHandler (Widget widget, int oldPlacement, int newPlacement);

	public class DragNotebook : Notebook
    {

		public event TabsReorderedHandler TabsReordered;

		bool DragInProgress;

		public DragNotebook () {
			//ButtonPressEvent += new ButtonPressEventHandler (OnButtonPress);
			//ButtonReleaseEvent += new ButtonReleaseEventHandler (OnButtonRelease);
			//AddEvents ((Int32) (EventMask.AllEventsMask));
		}

		int FindTabAtPosition (double cursorX, double cursorY) {

			int    dragNotebookXRoot;
			int    dragNotebookYRoot;
			Widget page              = GetNthPage (0);
			int    pageNumber        = 0;
			Widget tab;
			int    tabMaxX;
			int    tabMaxY;
			int    tabMinX;
			int    tabMinY;

			ParentWindow.GetOrigin (out dragNotebookXRoot, out dragNotebookYRoot);

			while (page != null) {

				if ((tab = GetTabLabel (page)) == null)
					return -1;

				tabMinX = dragNotebookXRoot + tab.Allocation.X;
				tabMaxX = tabMinX + tab.Allocation.Width;

				tabMinY = dragNotebookYRoot + tab.Allocation.Y;
				tabMaxY = tabMinY + tab.Allocation.Height;

				if ((tabMinX <= cursorX) && (cursorX <= tabMaxX) &&
					(tabMinY <= cursorY) && (cursorY <= tabMaxY))
					return pageNumber;

				page = GetNthPage (++pageNumber);
			}

			return -1;
		}

		void MoveTab (int destinationPage) {

			if (destinationPage >= 0 && destinationPage != CurrentPage) {
				ReorderChild (CurrentPageWidget, destinationPage);

				if (TabsReordered != null)
					TabsReordered (CurrentPageWidget, CurrentPage, destinationPage);
			}
		}

		[GLib.ConnectBefore]
		void OnButtonPress (object obj, ButtonPressEventArgs args) {

			if (DragInProgress)
				return;

			if (args.Event.Button == 1 && args.Event.Type == EventType.ButtonPress && FindTabAtPosition (args.Event.XRoot, args.Event.YRoot) >= 0)
				MotionNotifyEvent += new MotionNotifyEventHandler (OnMotionNotify);
		}

		void OnButtonRelease (object obj, ButtonReleaseEventArgs args) {
			if (Pointer.IsGrabbed) {
				Pointer.Ungrab (args.Event.Time);
				Grab.Remove (this);
			}

			MotionNotifyEvent -= new MotionNotifyEventHandler (OnMotionNotify);
			DragInProgress = false;
		}

		[GLib.ConnectBefore]
		void OnMotionNotify (object obj, MotionNotifyEventArgs args) {

			if (!DragInProgress) {
				DragInProgress = true;
				Grab.Add (this);

				if (!Pointer.IsGrabbed)
					Pointer.Grab (ParentWindow, false, EventMask.Button1MotionMask | EventMask.ButtonReleaseMask, null, new Cursor (CursorType.Fleur), args.Event.Time);	
			}

			MoveTab (FindTabAtPosition (args.Event.XRoot, args.Event.YRoot));
		}
	}
}
