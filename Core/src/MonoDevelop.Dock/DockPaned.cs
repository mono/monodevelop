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
using System.Xml;
using Gtk;

namespace Gdl
{
	public class DockPaned : DockItem
	{
		private const float SplitRatio = 0.3f;
		bool positionChanged = false;

		protected DockPaned (IntPtr raw) : base (raw) { }

		public DockPaned () : this (Orientation.Horizontal)
		{
			// loading a layout from xml may need to change orientation later
			this.PropertyChanged += new PropertyChangedHandler (OnPropertyChanged); 
		}

		public DockPaned (Orientation orientation)
		{
			this.Orientation = orientation;
			DockObjectFlags &= ~(DockObjectFlags.Automatic);
			CreateChild ();
		}
		
		public override bool HasGrip {
			get { return false; }
		}
		
		public override bool IsCompound {
			get { return true; }
		}
		
		[After]
		[Export]
		public int Position {
			get {
				if (Child != null && Child is Paned) {
					return ((Paned)Child).Position;
				}
				return 0;
			}
			set {
				if (Child != null && Child is Paned) {
					((Paned)Child).Position = value;
				}
			}
		}
		
		private void CreateChild ()
		{
			if (Child != null)
				Child.Unparent ();
			
			/* create the container paned */
			if (this.Orientation == Orientation.Horizontal)
				Child = new HPaned ();
			else
				Child = new VPaned ();
			
			Child.AddNotification ("position", new GLib.NotifyHandler (OnNotifyPosition));
			Child.ButtonReleaseEvent += new ButtonReleaseEventHandler (OnButtonReleased);
			Child.KeyPressEvent += new KeyPressEventHandler (OnKeyPressed);
												
			Child.Parent = this;
			Child.Show ();
		}

		protected override void OnAdded (Widget widget)
		{
			if (Child == null)
				return;
		
			Paned paned = Child as Paned;
			if (paned.Child1 != null && paned.Child2 != null)
				return;
			
			DockItem item = widget as DockItem;
			DockPlacement pos = DockPlacement.None;			
			if (paned.Child1 == null)
				pos = (Orientation == Orientation.Horizontal ?
				       DockPlacement.Left : DockPlacement.Top);
			else
				pos = (Orientation == Orientation.Horizontal ?
				       DockPlacement.Right : DockPlacement.Bottom);
			
			if (pos != DockPlacement.None)
				Dock (item, pos, null);
		}

		public override bool OnChildPlacement (DockObject child, ref DockPlacement placement)
		{
			DockPlacement pos = DockPlacement.None;
			if (this.Child != null) {
				Paned paned = this.Child as Paned;
				if (child == paned.Child1)
					pos = this.Orientation == Orientation.Horizontal ? DockPlacement.Left : DockPlacement.Top;
				else if (child == paned.Child2)
					pos = this.Orientation == Orientation.Horizontal ? DockPlacement.Right : DockPlacement.Bottom;
			}

			if (pos != DockPlacement.None) {
				placement = pos;
				return true;
			}

			return base.OnChildPlacement (child, ref pos);
		}

		protected override void OnDestroyed ()
		{
			// this first
			base.OnDestroyed ();

			// after that we can remove the Paned child
			if (Child != null) {
				Child.ButtonReleaseEvent -= new ButtonReleaseEventHandler (OnButtonReleased);
				Child.KeyPressEvent -= new KeyPressEventHandler (OnKeyPressed);
				Child.Unparent ();
				Child = null;
			}
		}	
	
		protected override void ForAll (bool include_internals, Callback cb)
		{
			if (include_internals) {
				base.ForAll (include_internals, cb);
			} else {
				if (Child != null) {
					((Paned)Child).Foreach (cb);
				}
			}
		}
		
		public override void OnDocked (DockObject requestor, DockPlacement position, object data)
		{
			if (Child == null)
				return;
		
			Paned paned = (Paned)Child;
			bool done = false;
			
			/* see if we can dock the item in our paned */
			switch (Orientation) {
			case Orientation.Horizontal:
				if (paned.Child1 == null && position == DockPlacement.Left) {
					paned.Pack1 (requestor, false, false);
					done = true;
				} else if (paned.Child2 == null && position == DockPlacement.Right) {
					paned.Pack2 (requestor, true, true);
					done = true;
				}
				break;
			case Orientation.Vertical:
				if (paned.Child1 == null && position == DockPlacement.Top) {
					paned.Pack1 (requestor, true, true);
					done = true;
				} else if (paned.Child2 == null && position == DockPlacement.Bottom) {
					paned.Pack2 (requestor, false, false);
					done = true;
				}
				break;
			}
			
			if (!done) {
				/* this will create another paned and reparent us there */
				base.OnDocked (requestor, position, data);
			} else {
				((DockItem)requestor).ShowGrip ();
				requestor.DockObjectFlags |= DockObjectFlags.Attached;
			}
		}
		
		public override bool OnDockRequest (int x, int y, ref DockRequest request)
		{
			bool mayDock = false;

			/* we get (x,y) in our allocation coordinates system */
			
			/* Get item's allocation. */
			Gdk.Rectangle alloc = Allocation;
			int bw = (int)BorderWidth;

			/* Get coordinates relative to our window. */
			int relX = x - alloc.X;
			int relY = y - alloc.Y;
			
			/* Check if coordinates are inside the widget. */
			if (relX > 0 && relX < alloc.Width &&
			    relY > 0 && relY < alloc.Height) {
			    	int divider = -1;
			    
				/* It's inside our area. */
				mayDock = true;

				/* these are for calculating the extra docking parameter */
				Requisition other = ((DockItem)request.Applicant).PreferredSize;
				Requisition my = PreferredSize;
				
				/* Set docking indicator rectangle to the Dock size. */
				request.X = bw;
				request.Y = bw;
				request.Width = alloc.Width - 2 * bw;
				request.Height = alloc.Height - 2 * bw;
				request.Target = this;

				/* See if it's in the BorderWidth band. */
				if (relX < bw) {
					request.Position = DockPlacement.Left;
					request.Width = (int)(request.Width * SplitRatio);
					divider = other.Width;
				} else if (relX > alloc.Width - bw) {
					request.Position = DockPlacement.Right;
					request.X += (int)(request.Width * (1 - SplitRatio));
					request.Width = (int)(request.Width * SplitRatio);
					divider = Math.Max (0, my.Width - other.Width);
				} else if (relY < bw) {
					request.Position = DockPlacement.Top;
					request.Height = (int)(request.Height * SplitRatio);
					divider = other.Height;
				} else if (relY > alloc.Height - bw) {
					request.Position = DockPlacement.Bottom;
					request.Y += (int)(request.Height * (1 - SplitRatio));
					request.Height = (int)(request.Height * SplitRatio);
					divider = Math.Max (0, my.Height - other.Height);
				} else { /* Otherwise try our children. */
					mayDock = false;
					DockRequest myRequest = new DockRequest (request);
					foreach (DockObject item in Children) {
						if (item.OnDockRequest (relX, relY, ref myRequest)) {
							mayDock = true;
							request = myRequest;
							break;
						}
					}
					
					if (!mayDock) {
						/* the pointer is on the handle, so snap
						   to top/bottom or left/right */
						mayDock = true;
						
						if (Orientation == Orientation.Horizontal) {
							if (relY < alloc.Height / 2) {
								request.Position = DockPlacement.Top;
								request.Height = (int)(request.Height * SplitRatio);
								divider = other.Height;
							} else {
								request.Position = DockPlacement.Bottom;
								request.Y += (int)(request.Height * (1 - SplitRatio));
								request.Height = (int)(request.Height * SplitRatio);
								divider = Math.Max (0, my.Height - other.Height);
							}
						} else {
							if (relX < alloc.Width / 2) {
								request.Position = DockPlacement.Left;
								request.Width = (int)(request.Width * SplitRatio);
								divider = other.Width;
							} else {
								request.Position = DockPlacement.Right;
								request.X += (int)(request.Width * (1 - SplitRatio));
								request.Width = (int)(request.Width * SplitRatio);
								divider = Math.Max (0, my.Width - other.Width);
							}
						}
					}
				}
				
				if (divider >= 0 && request.Position != DockPlacement.Center)
					request.Extra = divider;

				if (mayDock) {				
					/* adjust returned coordinates so they are
					   relative to our allocation */
					request.X += alloc.X;
					request.Y += alloc.Y;
				}
			}

			return mayDock;
		}

		void OnNotifyPosition (object sender, GLib.NotifyArgs a)
		{
			positionChanged = true;
		}

		[GLib.ConnectBefore]
		void OnKeyPressed (object sender, KeyPressEventArgs a)
		{
			// eat Shift|F8, see http://bugzilla.ximian.com/show_bug.cgi?id=61113
			if (a.Event.Key == Gdk.Key.F8 && a.Event.State == Gdk.ModifierType.ShiftMask)
				a.RetVal = true;
		}

		[GLib.ConnectBefore]
		void OnButtonReleased (object sender, ButtonReleaseEventArgs a)
		{
			if (a.Event.Button == 1 && positionChanged) {
				Master.EmitLayoutChangedEvent ();
				positionChanged = false;
			}
		}

		void OnPropertyChanged (object sender, string name)
		{
			if (name == "orientation") {
				CreateChild ();
				this.PropertyChanged -= new PropertyChangedHandler (OnPropertyChanged);
			}
		}
	}
}
