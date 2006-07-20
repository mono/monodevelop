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
using System.Collections.Generic;
using System.Xml;
using Gtk;

namespace Gdl
{
	public class Dock : DockObject
	{
		private readonly float SplitRatio = 0.3f;
		private DockObject root = null;
		private bool floating;
		private Widget window;
		private bool autoTitle;
		private int floatX;
		private int floatY;
		private int width = -1;
		private int height = -1;
		private Gdk.GC xorGC;
		private string title;

		protected Dock (IntPtr raw) : base (raw) { }

		public Dock () : this (null, false)
		{
		}

		public Dock (Dock original, bool floating)
			: this (original, floating, 0, 0, -1, -1)
		{
		}

		public Dock (Dock original, bool floating, int x, int y, int width, int height)
		{
			floatX = x;
			floatY = y;
			this.width = width;
			this.height = height;
			this.floating = floating;
			
			SetFlag (WidgetFlags.NoWindow);
			if (original != null)
				Bind (original.Master);

			/* create a master for the dock if none was provided */
			if (Master == null) {
				DockObjectFlags &= ~(DockObjectFlags.Automatic);
				Bind (new DockMaster ());
			}

			if (floating) {
				/* create floating window for this dock */
				window = new Window (WindowType.Toplevel);
				Window wnd = window as Window;

				/* set position and default size */
				wnd.WindowPosition = WindowPosition.Mouse;
				wnd.SetDefaultSize (width, height);
				wnd.TypeHint = Gdk.WindowTypeHint.Normal;
				
				/* metacity ignores this */
				wnd.Move (floatX, floatY);

				/* connect to the configure event so we can track down window geometry */
				wnd.ConfigureEvent += new ConfigureEventHandler (OnFloatingConfigure);
				
				/* set the title */
				SetWindowTitle ();

				/* set transient for the first dock if that is a non-floating dock */
				DockObject controller = Master.Controller;
				if (controller != null && controller is Dock) {
					if (!((Dock)controller).Floating) {
						if (controller.Toplevel != null && controller.Toplevel is Window) {
							wnd.TransientFor = (Window)controller.Toplevel;
						}
					}
				}
				
				wnd.Add (this);
				wnd.DeleteEvent += new DeleteEventHandler (OnFloatingDelete);
			}

			DockObjectFlags |= DockObjectFlags.Attached;
		}
		
		[Export]
		public bool Floating {
			get {
				return floating;
			}
			set {
				floating = value;
			}
		}

		[Export]
		public int FloatX {
			get {
				return floatX;
			}
			set {
				floatX = value;
				if (floating && window != null && window is Window)
					((Window)window).Resize (width, height);
			}
		}
		
		[Export]
		public int FloatY {
			get {
				return floatY;
			}
			set {
				floatY = value;
				if (floating && window != null && window is Window)
					((Window)window).Resize (width, height);
			}
		}
		
		[Export]
		public int Height {
			get {
				return height;
			}
			set {
				height = value;
				if (floating && window != null && window is Window)
					((Window)window).Resize (width, height);
			}
		}
		
		private bool IsController {
			get {
				if (Master == null)
					return false;
				return Master.Controller == this;
			}
		}

		public ICollection<DockObject> NamedItems {
			get {
				return Master.DockObjects;
			}
		}
		
		public DockObject Root {
			get {
				return root;
			}
			set {
				root = value;
			}
		}
		
		public string Title {
			get {
				return title;
			}
			set {
				title = value;
			}
		}
		
		[Export]
		public int Width {
			get { return width; }
			set {
				width = value;
				if (floating && window != null && window is Window)
					((Window)window).Resize (width, height);
			}
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			// make request to root
			if (root != null && root.Visible)
				requisition = root.SizeRequest ();
			else
				requisition.Width = requisition.Height = 0;

			requisition.Width += 2 * (int)BorderWidth;
			requisition.Height += 2 * (int)BorderWidth;
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
		
			// reduce allocation by border width
			int bw = (int)BorderWidth;
			allocation.X += bw;
			allocation.Y += bw;
			allocation.Width = Math.Max (1, allocation.Width - 2 * bw);
			allocation.Height = Math.Max (1, allocation.Height - 2 * bw);

			if (root != null && root.Visible)
				root.SizeAllocate (allocation);
		}
		
		protected override void OnMapped ()
		{
			base.OnMapped ();

			if (root != null && root.Visible && !root.IsMapped)
				root.Map ();
		}
		
		protected override void OnUnmapped ()
		{
			base.OnUnmapped ();

			if (root != null && root.Visible && root.IsMapped)
				root.Unmap ();

			if (window != null)
				window.Unmap ();
		}
		
		protected override void OnShown ()
		{
			base.OnShown ();

			if (floating && window != null)
				window.ShowAll ();

			if (IsController) {
				foreach (DockObject item in Master.TopLevelDocks) {
					if (item == this)
						continue;
					if (item.IsAutomatic)
						item.Show ();
				}
			}
		}
		
		protected override void OnHidden ()
		{
			base.OnHidden ();
			
			if (floating && window != null)
				window.Hide ();

			if (IsController) {
				foreach (DockObject item in Master.TopLevelDocks) {
					if (item == this)
						continue;
					if (item.IsAutomatic)
						item.Hide ();
				}
			}
		}

		protected override void OnDestroyed ()
		{
			if (window != null) {
				window.Destroy ();
				floating = false;
				window = null;
			}

			// destroy the xor gc
			if (xorGC != null)
				xorGC = null;

			base.OnDestroyed ();
		}
		
		protected override void OnAdded (Widget widget)
		{
			DockItem child = widget as DockItem;
			AddItem (child, DockPlacement.Top);
		}
		
		protected override void OnRemoved (Widget widget)
		{
			bool wasVisible = widget.Visible;

			if (root == widget) {
				root.DockObjectFlags &= ~(DockObjectFlags.Attached);
				root = null;
				widget.Unparent ();

				if (wasVisible && Visible)
					QueueResize ();
			}
		}
		
		protected override void ForAll (bool include_internals, Callback cb)
		{
			if (root != null)
				cb (root);
		}
		
		public override void OnDetached (bool recursive)
		{
			if (recursive && root != null)
				root.Detach (recursive);

			DockObjectFlags &= ~(DockObjectFlags.Attached);
		}
		
		public override void OnReduce ()
		{
			if (root != null)
				return;

			if (IsAutomatic) {
				Destroy ();
			} else if (!IsAttached) {
				if (floating)
					Hide ();
				else if (Parent != null && Parent is Container)
					((Container)Parent).Remove (this);
			}
		}
		
		public override bool OnDockRequest (int x, int y, ref DockRequest request)
		{
			bool mayDock = false;
		
			/* we get (x,y) in our allocation coordinates system */
			
			/* Get dock size. */
			Gdk.Rectangle alloc = Allocation;
			int bw = (int)BorderWidth;

			/* Get coordinates relative to our allocation area. */
			int relX = x - alloc.X;
			int relY = y - alloc.Y;

			/* Check if coordinates are in GdlDock widget. */
			if (relX > 0 && relX < alloc.Width &&
			    relY > 0 && relY < alloc.Height) {
			    
				/* It's inside our area. */
				mayDock = true;

				/* Set docking indicator rectangle to the Dock size. */
				request.X = alloc.X + bw;
				request.Y = alloc.Y + bw;
				request.Width = alloc.Width - 2 * bw;
				request.Height = alloc.Height - 2 * bw;
				
				/* If Dock has no root item yet, set the dock
				   itself as possible target. */
				if (root == null) {
					request.Position = DockPlacement.Top;
					request.Target = this;
				} else {
					request.Target = root;
					
					/* See if it's in the BorderWidth band. */
					if (relX < bw) {
						request.Position = DockPlacement.Left;
						request.Width = (int)(request.Width * SplitRatio);
					} else if (relX > alloc.Width - bw) {
						request.Position = DockPlacement.Right;
						request.X += (int)(request.Width * (1 - SplitRatio));
						request.Width = (int)(request.Width * SplitRatio);
					} else if (relY < bw) {
						request.Position = DockPlacement.Top;
						request.Height = (int)(request.Height * SplitRatio);
					} else if (relY > alloc.Height - bw) {
						request.Position = DockPlacement.Bottom;
						request.Y += (int)(request.Height * (1 - SplitRatio));
						request.Height = (int)(request.Height * SplitRatio);
					} else {
						/* Otherwise try our children. */
						/* give them allocation coordinates
						   (we are a NoWindow widget) */
						mayDock = root.OnDockRequest (x, y, ref request);
					}
				}
			}
			
			return mayDock;
		}
		
		public override void OnDocked (DockObject requestor, DockPlacement position, object data)
		{
			/* only dock items allowed at this time */
			if (!(requestor is DockItem))
				return;

			if (position == DockPlacement.Floating) {
				int x = 0, y = 0, width = -1, height = -1;
				if (data != null && data is Gdk.Rectangle) {
					Gdk.Rectangle rect = (Gdk.Rectangle)data;
					x = rect.X;
					y = rect.Y;
					width = rect.Width;
					height = rect.Height;
				}
				
				AddFloatingItem ((DockItem)requestor, x, y, width, height);
			} else if (root != null) {
				/* This is somewhat a special case since we know
				   which item to pass the request on because we only
				   have one child */
				root.Dock (requestor, position, null);
				SetWindowTitle ();
			} else { /* Item about to be added is root item. */
				root = requestor;
				root.DockObjectFlags |= DockObjectFlags.Attached;
				root.Parent = this;
				((DockItem)root).ShowGrip ();
				
				/* Realize the item (create its corresponding GdkWindow)
			           when the Dock has been realized. */
				if (IsRealized)
					root.Realize ();
				
				/* Map the widget if it's visible and the parent is
			           visible and has been mapped. This is done to make
			           sure that the GdkWindow is visible. */
				if (Visible && root.Visible) {
					if (IsMapped)
						root.Map ();
					
					/* Make the widget resize. */
					root.QueueResize ();
				}
				
				SetWindowTitle ();
			}
		}
		
		public override bool OnReorder (DockObject requestor, DockPlacement position, object data)
		{
			if (Floating && position == DockPlacement.Floating && root == requestor) {
				if (window != null && data != null && data is Gdk.Rectangle) {
					Gdk.Rectangle rect = (Gdk.Rectangle)data;
					((Window)window).Move (rect.X, rect.Y);
					return true;
				}
			}

			return false;
		}
		
		public override bool OnChildPlacement (DockObject child, ref DockPlacement placement)
		{
			if (root == child) {
				if (placement == DockPlacement.None ||
				    placement == DockPlacement.Floating)
					placement = DockPlacement.Top;
				return true;
			}
				
			return false;
		}
		
		public override void GetRelativeChildPlacement (DockObject child, out DockObject relativeObject, out DockPlacement relativePlacement)
		{
			// Since a Dock can only have one child, we use "this" as relative object, and Center as relative position.
			
			if (child == root) {
				relativeObject = this;
				relativePlacement = DockPlacement.Center;
			} else {
				relativeObject = null;
				relativePlacement = DockPlacement.None;
			}
		}
		
		public override DockObject GetChildFromRelative (DockObject relativeObject, DockPlacement relativePlacement)
		{
			// Since a Dock can only have one child, we use "this" as relative object, and Center as relative position.
			
			if (relativeObject == this && relativePlacement == DockPlacement.Center)
				return root;
			else
				return null;
		}
		
		public override DockObject GetObjectFromRelativePlacement (DockPlacement relativePlacement)
		{
			// Since a Dock can only have one child, we use "this" as relative object, and Center as relative position.
			
			if (relativePlacement == DockPlacement.Center)
				return root;
			else
				return null;
		}
		
		public override void OnPresent (DockObject child)
		{
			if (Floating && window != null && window is Window)
				((Window)window).Present ();
		}
		
		public void AddItem (DockItem item, DockPlacement placement)
		{
			if (item == null)
				return;

			if (placement == DockPlacement.Floating)
				AddFloatingItem (item, 0, 0, -1, -1);
			else
				Dock (item, placement, null);
		}
		
		public void AddFloatingItem (DockItem item, int x, int y, int width, int height)
		{
			Dock dock = new Dock (this, true, x, y, width, height);
			
			if (Visible) {
				dock.Show ();
				if (IsMapped)
					dock.Map ();
				dock.QueueResize ();
			}
			
			dock.AddItem (item, DockPlacement.Top);
		}
		
		public DockItem GetItemByName (string name)
		{
			if (name == null)
				return null;

			DockObject found = Master.GetObject (name);
			if (found != null && found is DockItem)
				return (DockItem)found;
			else
				return null;
		}
		
		public DockPlaceholder GetPlaceholderByName (string name)
		{
			if (name == null)
				return null;

			DockObject found = Master.GetObject (name);
			if (found != null && found is DockPlaceholder)
				return (DockPlaceholder)found;
			else
				return null;
		}
		
		public static Dock GetTopLevel (DockObject obj)
		{
			DockObject parent = obj;
			while (parent != null && !(parent is Dock))
				parent = parent.ParentObject;

			return parent != null ? (Dock)parent : null;
		}
		
		public void XorRect (Gdk.Rectangle rect)
		{
			if (xorGC == null) {
				if (IsRealized) {
					Gdk.GCValues values = new Gdk.GCValues ();
					values.Function = Gdk.Function.Invert;
					values.SubwindowMode = Gdk.SubwindowMode.IncludeInferiors;
					xorGC = new Gdk.GC (GdkWindow);
					xorGC.SetValues (values, Gdk.GCValuesMask.Function |
							 Gdk.GCValuesMask.Subwindow);
				} else {
					return;
				}
			}

			xorGC.SetLineAttributes (1, Gdk.LineStyle.OnOffDash,
						 Gdk.CapStyle.NotLast,
						 Gdk.JoinStyle.Bevel);
			xorGC.SetDashes (1, new sbyte[] { 1, 1}, 2);
			
			GdkWindow.DrawRectangle (xorGC, false, rect.X, rect.Y,
						 rect.Width, rect.Height);

			xorGC.SetDashes (0, new sbyte[] { 1, 1}, 2);

			GdkWindow.DrawRectangle (xorGC, false, rect.X + 1,
						 rect.Y + 1, rect.Width - 2,
						 rect.Height - 2);
		}
		
		private void SetWindowTitle ()
		{
			if (window == null)
				return;
		
			if (!autoTitle && LongName != null)
				title = LongName;
			else if (Master != null)
				title = Master.DefaultTitle;
			
			if (title == null && root != null)
				title = root.LongName;
			
			if (title == null) {
				autoTitle = true;
				title = "Dock " + Master.DockNumber++;
				LongName = title;
			}
			
			((Window)window).Title = title;
		}

		[GLib.ConnectBefore]
		private void OnFloatingConfigure (object o, ConfigureEventArgs e)
		{
			floatX = e.Event.X;
			floatY = e.Event.Y;
			width = e.Event.Width;
			height = e.Event.Height;

			e.RetVal = false;
		}

		private void OnFloatingDelete (object o, DeleteEventArgs e)
		{
			if (root != null)
				/* this will call reduce on ourselves, hiding
				   the window if appropiate */
				((DockItem)root).HideItem ();

			e.RetVal = true;
		}
	}
}
