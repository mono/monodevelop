/*
 * Copyright (C) 2004 Todd Berman <tberman@off.net>
 * Copyright (C) 2004 Jeroen Zwartepoorte <jeroen@xs4all.nl>
 * Copyright (C) 2005 John Luke <john.luke@gmail.com>
 *
 * based on work by:
 * Copyright (C) 2002 Gustavo Giráldez <gustavo.giraldez@gmx.net>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */
using System;
using System.Collections;
using Gtk;

namespace Gdl
{
	public class DockMaster
	{
		private Hashtable dockObjects = new Hashtable ();
		private ArrayList toplevelDocks = new ArrayList ();
		private DockObject controller = null;
		private int dockNumber = 1;

		// for naming nameless manual objects
		private int number = 1;
		private string defaultTitle;

		private Gdk.GC rootXorGC;
		private bool rectDrawn;
		private Dock rectOwner;

		private DockRequest request;

		// hashes to quickly calculate the overall locked status:
		// if size(unlocked_items) == 0 then locked = 1
		// else if size(locked_items) == 0 then locked = 0
		// else locked = -1
		private Hashtable lockedItems = new Hashtable ();
		private Hashtable unlockedItems = new Hashtable ();
		
		public event EventHandler LayoutChanged;
		internal event EventHandler NotifyLocked;

		public DockMaster () 
		{
		}
		
		public string DefaultTitle {
			get {
				return defaultTitle;
			}
			set {
				defaultTitle = value;
			}
		}
		
		public int DockNumber {
			get {
				return dockNumber;
			}
			set {
				dockNumber = value;
			}
		}
		
		public ICollection DockObjects {
			get {
				return dockObjects.Values;
			}
		}
		
		// 1  = all the dock items bound to the master are locked
		// 0  = all the dock items bound to the master are unlocked
		// -1 = inconsistent
		public int Locked {
			get {
				return ComputeLocked ();
			}
			set {
				if (value >= 0)
					LockUnlock (value > 0);
				EmitNotifyLocked ();
			}
		}
		
		public ArrayList TopLevelDocks {
			get {
				return toplevelDocks;
			}
		}

		protected void ForeachLockUnlock (DockItem item, bool locked)
		{
			item.Locked = locked;
			if (item.IsCompound) {
				foreach (Widget w in item.Children) {
					DockItem i = w as DockItem;
					if (i != null)
						ForeachLockUnlock (i, locked);
				}
			}
		}
		
		public void LockUnlock (bool locked)
		{
			foreach (Dock dock in toplevelDocks) {
				if (dock.Root != null && dock.Root is DockItem)
					ForeachLockUnlock ((DockItem)dock.Root, locked);
			}

			// FIXME: not sure about which list to foreach here
			// just to be sure hidden items are set too
			foreach (Widget w in toplevelDocks) {
				DockItem i = w as DockItem;
				if (i != null)
					ForeachLockUnlock (i, locked);
			}
		}
		
		public void Add (DockObject obj)
		{
			if (obj == null)
				return;

			if (!obj.IsAutomatic) {
				/* create a name for the object if it doesn't have one */
				if (obj.Name == null)
					obj.Name = "__dock_" + number++;

				/* add the object to our hash list */
				if (dockObjects.Contains (obj.Name))
					Console.WriteLine ("Unable to add object, name \"{0}\" taken", obj.Name);
				else
					dockObjects.Add (obj.Name, obj);
			}
			
			if (obj is Dock) {
				/* if this is the first toplevel we are adding, name it controller */
				if (toplevelDocks.Count == 0)
					controller = obj;
				
				/* add dock to the toplevel list */
				if (((Dock)obj).Floating)
					toplevelDocks.Insert (0, obj);
				else
					toplevelDocks.Add (obj);
				
				/* we are interested in the dock request this toplevel
				 * receives to update the layout */
				obj.Docked += new DockedHandler (OnItemDocked);
			} else if (obj is DockItem) {
				DockItem item = obj as DockItem;
				
				/* we need to connect the item's events */
				item.Detached += new DetachedHandler (OnItemDetached);
				item.Docked += new DockedHandler (OnItemDocked);
				item.DockItemDragBegin += new DockItemDragBeginHandler (OnDragBegin);
				item.DockItemMotion += new DockItemMotionHandler (OnDragMotion);
				item.DockItemDragEnd += new DockItemDragEndHandler (OnDragEnd);

				/* register to "locked" notification if the item has a grip,
				 * and add the item to the corresponding hash */
				item.PropertyChanged += new PropertyChangedHandler (OnItemPropertyChanged);

				/* post a layout_changed emission if the item is not automatic
				 * (since it should be added to the items model) */
				if (!item.IsAutomatic) {
					EmitLayoutChangedEvent ();
				}
			}
		}
		
		public void Remove (DockObject obj)
		{
			if (obj == null)
				return;
	
			// remove from locked/unlocked hashes and property change if that's the case
			if (obj is DockItem && ((DockItem)obj).HasGrip) {
				int locked = Locked;
				if (lockedItems.Contains (obj)) {
					lockedItems.Remove (obj);
					if (Locked != locked)
						EmitNotifyLocked ();
				}
				if (unlockedItems.Contains (obj)) {
					unlockedItems.Remove (obj);
					if (Locked != locked)
						EmitNotifyLocked ();
				}
			}
			
			if (obj is Dock) {
				toplevelDocks.Remove (obj);
				obj.Docked -= new DockedHandler (OnItemDocked);

				if (obj == controller) {
					DockObject newController = null;

					// now find some other non-automatic toplevel to use as a
					// new controller.  start from the last dock, since it's
					// probably a non-floating and manual
					ArrayList reversed = toplevelDocks;
					reversed.Reverse ();

					foreach (DockObject item in reversed) {
						if (!item.IsAutomatic) {
							newController = item;
							break;
						}
					}

					if (newController != null) {
						controller = newController;
					} else {
						// no controller, no master
						controller = null;
					}
				}
			}
			
			// disconnect the signals
			if (obj is DockItem) {
				DockItem item = obj as DockItem;
				item.Detached -= new DetachedHandler (OnItemDetached);
				item.Docked -= new DockedHandler (OnItemDocked);
				item.DockItemDragBegin -= new DockItemDragBeginHandler (OnDragBegin);
				item.DockItemMotion -= new DockItemMotionHandler (OnDragMotion);
				item.DockItemDragEnd -= new DockItemDragEndHandler (OnDragEnd);
				item.PropertyChanged -= new PropertyChangedHandler (OnItemPropertyChanged);
			}
			
			// remove the object from the hash if it is there
			if (obj.Name != null && dockObjects.Contains (obj.Name))
				dockObjects.Remove (obj.Name);
			
			/* post a layout_changed emission if the item is not automatic
			 * (since it should be removed from the items model) */
			if (!obj.IsAutomatic)
				EmitLayoutChangedEvent ();
		}
		
		public DockObject GetObject (string name)
		{
			if (name == null)
				return null;
			return (DockObject)dockObjects[name];
		}
		
		public DockObject Controller {
			get { return controller; }
			set {
				if (value != null) {
					if (value.IsAutomatic)
						Console.WriteLine ("New controller is automatic, only manual dock objects should be named controller");
					// check that the controller is in the toplevel list
					if (!toplevelDocks.Contains (value))
						Add (value);
					controller = value;
				} else {
					// no controller, no master
					controller = null;
				}
			}
		}
		
		internal void EmitLayoutChangedEvent ()
		{
			if (LayoutChanged != null)
				LayoutChanged (this, EventArgs.Empty);
		}
		
		private void OnItemDetached (object o, DetachedArgs args)
		{
			DockItem obj = o as DockItem;
			if (!obj.InReflow && !obj.IsAutomatic)
				EmitLayoutChangedEvent ();
		}
		
		private void OnItemDocked (object o, DockedArgs args)
		{
			DockItem requestor = args.Requestor as DockItem;

			// here we are in fact interested in the requestor, since it's
			// assumed that object will not change its visibility... for the
			// requestor, however, could mean that it's being shown
			if (!requestor.InReflow && !requestor.IsAutomatic)
				EmitLayoutChangedEvent ();
		}
		
		private void OnItemPropertyChanged (object o, string name)
		{
			DockItem item = o as DockItem;
			int locked = ComputeLocked ();
			bool item_locked = item.Locked;

			if (item_locked) {
				if (unlockedItems.ContainsKey (item))
					unlockedItems.Remove (item);
				if (!lockedItems.ContainsKey (item))
					lockedItems.Add (item, 1);
			}
			else {
				if (lockedItems.ContainsKey (item))
					lockedItems.Remove (item);
				if (!unlockedItems.ContainsKey (item))
					unlockedItems.Add (item, 1);
			}

			if (ComputeLocked () != locked)
				EmitNotifyLocked ();
		}

		private int ComputeLocked ()
		{
			if (unlockedItems.Count == 0)
				return 1;
			else if (lockedItems.Count == 0)
				return 0;
			else
				return -1;
		}		

		private void OnDragBegin (DockItem item)
		{
			/* Set the target to itself so it won't go floating with just a click. */
			request = new DockRequest ();
			request.Applicant = item;
			request.Target = item;
			request.Position = DockPlacement.Floating;
			request.Extra = IntPtr.Zero;

			rectDrawn = false;
			rectOwner = null;
		}
		
		private void OnDragEnd (DockItem item, bool cancelled)
		{
			if (item != request.Applicant)  {
				Console.WriteLine ("Dragged item is not the same as the one we started with");
				return;
			}
			
			/* Erase previously drawn rectangle */
			if (rectDrawn)
				XorRect ();
			
			/* cancel conditions */
			if (cancelled || request.Applicant == request.Target)
				return;

			// dock object to the requested position
			request.Target.Dock (request.Applicant,
					     request.Position,
					     request.Extra);
			
			EmitLayoutChangedEvent ();
		}
		
		private void OnDragMotion (DockItem item, int rootX, int rootY)
		{
			Dock dock = null;
			int winX, winY;
			int x, y;
			bool mayDock = false;
			DockRequest myRequest = new DockRequest (request);

			if (item != request.Applicant)  {
				Console.WriteLine ("Dragged item is not the same as the one we started with");
				return;
			}
			
			/* first look under the pointer */
			Gdk.Window window = Gdk.Window.AtPointer (out winX, out winY);
			if (window != null && window.UserData != IntPtr.Zero) {
				/* ok, now get the widget who owns that window and see if we can
				   get to a Dock by walking up the hierarchy */
				Widget widget = GLib.Object.GetObject (window.UserData, false) as Widget;
				while (widget != null && (!(widget is Dock) ||
				       (widget is DockObject && ((DockObject)widget).Master != this)))
						widget = widget.Parent;
				
				if (widget != null) {
					int winW, winH, depth;
					
					/* verify that the pointer is still in that dock
					   (the user could have moved it) */
					widget.GdkWindow.GetGeometry (out winX, out winY,
								      out winW, out winH,
								      out depth);
					widget.GdkWindow.GetOrigin (out winX, out winY);
					if (rootX >= winX && rootX < winX + winW &&
					    rootY >= winY && rootY < winY + winH)
						dock = widget as Dock;
				}
			}
			
			if (dock != null) {
				/* translate root coordinates into dock object coordinates
				   (i.e. widget coordinates) */
				dock.GdkWindow.GetOrigin (out winX, out winY);
				x = rootX - winX;
				y = rootY - winY;
				mayDock = dock.OnDockRequest (x, y, ref myRequest);
			} else {
				/* try to dock the item in all the docks in the ring in turn */
				foreach (Dock topDock in toplevelDocks) {
					if (topDock.GdkWindow == null)
						Console.WriteLine ("Dock has no GdkWindow: {0}, {1}", topDock.Name, topDock);
					/* translate root coordinates into dock object
					   coordinates (i.e. widget coordinates) */
					topDock.GdkWindow.GetOrigin (out winX, out winY);
					x = rootX - winX;
					y = rootY - winY;
					mayDock = topDock.OnDockRequest (x, y, ref myRequest);
					if (mayDock)
						break;
				}
			}

			if (!mayDock) {
				dock = null;
				
				myRequest.Target = Dock.GetTopLevel (item);
				myRequest.Position = DockPlacement.Floating;
				Requisition preferredSize = item.PreferredSize;
				myRequest.Width = preferredSize.Width;
				myRequest.Height = preferredSize.Height;
				myRequest.X = rootX - item.DragOffX;
				myRequest.Y = rootY - item.DragOffY;
				
				Gdk.Rectangle rect = new Gdk.Rectangle (myRequest.X,
									myRequest.Y,
									myRequest.Width,
									myRequest.Height);

				// setup extra docking information
				myRequest.Extra = rect;
			}
			
			if (!(myRequest.X == request.X &&
			      myRequest.Y == request.Y &&
			      myRequest.Width == request.Width &&
			      myRequest.Height == request.Height &&
			      dock == rectOwner)) {
			      
				/* erase the previous rectangle */
				if (rectDrawn)
					XorRect ();
			}
			
			// set the new values
			request = myRequest;
			rectOwner = dock;
			
			/* draw the previous rectangle */
			if (!rectDrawn)
				XorRect ();
		}

		private void XorRect ()
		{
			rectDrawn = !rectDrawn;

			if (rectOwner != null) {
				Gdk.Rectangle rect = new Gdk.Rectangle (request.X,
								        request.Y,
								        request.Width,
								        request.Height);
				rectOwner.XorRect (rect);
				return;
			}
			
			Gdk.Window window = Gdk.Global.DefaultRootWindow;
			
			if (rootXorGC == null) {
				Gdk.GCValues values = new Gdk.GCValues ();
				values.Function = Gdk.Function.Invert;
				values.SubwindowMode = Gdk.SubwindowMode.IncludeInferiors;

				rootXorGC = new Gdk.GC (window);
				rootXorGC.SetValues (values, Gdk.GCValuesMask.Function |
						     Gdk.GCValuesMask.Subwindow);
			}
			
			rootXorGC.SetLineAttributes (1, Gdk.LineStyle.OnOffDash,
						     Gdk.CapStyle.NotLast,
						     Gdk.JoinStyle.Bevel);

			rootXorGC.SetDashes (1, new sbyte[] {1, 1}, 2);
			
			window.DrawRectangle (rootXorGC, false, request.X, request.Y,
					      request.Width, request.Height);
			
			rootXorGC.SetDashes (0, new sbyte[] {1, 1}, 2);

			window.DrawRectangle (rootXorGC, false, request.X + 1,
					      request.Y + 1, request.Width - 2,
					      request.Height - 2);
		}

		void EmitNotifyLocked ()
		{
			if (NotifyLocked != null)
				NotifyLocked (this, EventArgs.Empty);
		}
	}
}
