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
using Mono.Unix;
using Gtk;

namespace Gdl
{
	public delegate void DockItemMotionHandler (DockItem o, int x, int y);
	public delegate void DockItemDragBeginHandler (DockItem o);
	public delegate void DockItemDragEndHandler (DockItem o, bool cancelled);
	
	public class DockItem : DockObject
	{
		private const float SplitRatio = 0.4f;
		private Widget child = null;
		private DockItemBehavior behavior = DockItemBehavior.Normal;
		private Orientation orientation = Orientation.Vertical;
		private bool resize = false;
		private int dragoffX = 0;
		private int dragoffY = 0;
		private Menu menu = null;
		private DockItemGrip grip;
		private DockBar dockBar;
		private DockBarButton dockButton;
		private Widget tabLabel = null;
		private int preferredWidth = -1;
		private int preferredHeight = -1;
		private DockPlaceholder dockPlaceHolder = null;
		private int startX;
		private int startY;
		
		public event DockItemMotionHandler DockItemMotion;
		public event DockItemDragBeginHandler DockItemDragBegin;
		public event DockItemDragEndHandler DockItemDragEnd;
		
		public event EventHandler DockItemShown;
		public event EventHandler DockItemHidden;
				
		static DockItem ()
		{
			Rc.ParseString ("style \"gdl-dock-item-default\" {\n" +
						"xthickness = 0\n" +
						"ythickness = 0\n" + 
					"}\n" + 
					"class \"Gdl_DockItem\" " +
					"style : gtk \"gdl-dock-item-default\"\n");
		}
		
		protected DockItem ()
		{
			// remove NoWindow flag
			WidgetFlags &= ~(WidgetFlags.NoWindow);
			DockObjectFlags &= ~(DockObjectFlags.Automatic);
		
			if (HasGrip) {
				grip = new DockItemGrip (this);
				grip.Parent = this;
				grip.Show ();
			}
		}

		protected DockItem (IntPtr raw) : base (raw) { }
		
		public DockItem (string name, string longName, DockItemBehavior behavior) : this ()
		{
			Name = name;
			LongName = longName;
			Behavior = behavior;
			((Label) TabLabel).Markup = longName;
		}
		
		public DockItem (string name, string longName, string stockid,
				 DockItemBehavior behavior) : this (name, longName, behavior)
		{
			StockId = stockid;
		}
		
		public DockItemBehavior Behavior {
			get {
				return behavior;
			}
			set {
				DockItemBehavior oldBehavior = behavior;
				behavior = value;
				if (((oldBehavior ^ behavior) & DockItemBehavior.Locked) != 0) {
					if (Master != null)
						Master.EmitLayoutChangedEvent ();
				}
				EmitPropertyEvent ("Behavior");
			}
		}
		
		public bool CantClose {
			get { return ((Behavior & DockItemBehavior.CantClose) != 0) || Locked; }
		}
		
		public bool CantIconify {
			get { return ((Behavior & DockItemBehavior.CantIconify) != 0) || Locked; }
		}
		
		public new Widget Child {
			get { return child; }
			set { child = value; }
		}

		public DockBar DockBar {
			get { return dockBar; }
			set { dockBar = value; }
		}
		
		public DockBarButton DockBarButton {
			get { return dockButton; }
			set { dockButton = value;	}
		}
		
		public int DragOffX {
			get { return dragoffX; }
			set { dragoffX = value; }
		}
		
		public int DragOffY {
			get { return dragoffY; }
			set { dragoffY = value; }
		}
		
		public bool GripShown {
			get { return HasGrip; }
		}
		
		public virtual bool HasGrip {
			get { return !NoGrip; }
		}
		
		public bool Iconified
		{
			get { return ((DockObjectFlags & DockObjectFlags.Iconified) != 0); }
			set
			{ 
				if (value)
				{
					DockObjectFlags |= DockObjectFlags.Iconified;
				}
				else
				{
					DockObjectFlags &= ~(DockObjectFlags.Iconified);
				}
			}
		}
		
		public bool InDrag {
			get { return ((DockObjectFlags & DockObjectFlags.InDrag) != 0); }
		}
		
		public bool InPreDrag {
			get { return ((DockObjectFlags & DockObjectFlags.InPreDrag) != 0); }
		}
		
		public override bool IsCompound {
			get { return false; }
		}
		
		[Export]
		public bool Locked {
			get {
				return ((behavior & DockItemBehavior.Locked) != 0);
			}
			set {
				DockItemBehavior oldBehavior = behavior;
				if (value)
					behavior |= DockItemBehavior.Locked;
				else
					behavior &= ~(DockItemBehavior.Locked);

				if ((oldBehavior ^ behavior) != 0) {
					ShowHideGrip ();
					if (Master != null)
						Master.EmitLayoutChangedEvent ();
					EmitPropertyEvent ("Locked");
				}
			}
		}

		public bool NoGrip {
			get { return ((behavior & DockItemBehavior.NoGrip) != 0); }
			set {
				if (value)
					behavior |= DockItemBehavior.NoGrip;
				else
					behavior &= ~(DockItemBehavior.NoGrip);
			}
		}
		
		[Export]
		public Orientation Orientation {
			get { return orientation; }
			set { SetOrientation (value); }
		}
		
		public int PreferredHeight {
			get { return preferredHeight; }
			set { preferredHeight = value; }
		}
		
		public int PreferredWidth {
			get { return preferredWidth; }
			set { preferredWidth = value; }
		}
		
		public Requisition PreferredSize {
			get {
				Requisition req = new Requisition ();
				req.Width = Math.Max (preferredWidth, Allocation.Width);
				req.Height = Math.Max (preferredHeight, Allocation.Height);
				return req;
			}
		}
		
		public bool Resize {
			get { return resize; }
			set {
				resize = value;
				QueueResize ();
				EmitPropertyEvent ("Resize");
			}
		}
		
		public Widget TabLabel {
			get {
				if (tabLabel == null)
					tabLabel = new Label ();
				return tabLabel;
			}
			set { tabLabel = value; }
		}
		
		public bool UserAction {
			get { return ((DockObjectFlags & DockObjectFlags.UserAction) != 0); }
		}
		
		protected override void OnAdded (Widget widget)
		{
			if (widget is DockObject) {
				Console.WriteLine ("You can't add a DockObject to a DockItem");
				return;
			}
			
			if (Child != null) {
				Console.WriteLine ("This DockItem already has a child");
				return;
			}
			
			widget.Parent = this;
			Child = widget;
		}
		
		protected override void OnRemoved (Widget widget)
		{
			bool wasVisible = widget.Visible;

			if (grip == widget) {
				widget.Unparent ();
				grip = null;
				if (wasVisible)
					QueueResize ();
				return;
			} else if (widget != Child) {
				return;
			}

			if (InDrag)
				EndDrag (true);
			
			widget.Unparent ();
			Child = null;
			
			if (wasVisible)
				QueueResize ();
		}
		
		protected override void ForAll (bool include_internals, Callback cb)
		{
			if (include_internals && grip != null)
				cb (grip);
			if (Child != null)
				cb (Child);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Width = ((int)BorderWidth + Style.XThickness) * 2;
			requisition.Height = ((int)BorderWidth + Style.YThickness) * 2;
		
			Requisition childReq;
			// If our child is not visible, we still request its size, since
			// we won't have any useful hint for our size otherwise.
			if (Child != null) {
				childReq = Child.SizeRequest ();
			} else {
				childReq.Width = 0;
				childReq.Height = 0;
			}

			Requisition gripReq;
			gripReq.Width = gripReq.Height = 0;

			if (Orientation == Orientation.Horizontal) {
				if (GripShown) {
					gripReq = grip.SizeRequest ();
					requisition.Width = gripReq.Width;
				}
				
				if (Child != null) {
					requisition.Width += childReq.Width;
					requisition.Height = Math.Max (childReq.Height,
									gripReq.Height);
				}
			} else {
				if (GripShown) {
					gripReq = grip.SizeRequest ();
					requisition.Height = gripReq.Height;
				}
				
				if (Child != null) {
					requisition.Width = childReq.Width;
					requisition.Height += childReq.Height;
				}
			}

			requisition.Width += (int) (this.BorderWidth + this.Style.XThickness) * 2;
			requisition.Height += (int) (this.BorderWidth + this.Style.XThickness) * 2;
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			if (IsRealized) {
				GdkWindow.MoveResize (allocation.X, allocation.Y,
						      allocation.Width, allocation.Height);
			}
			
			if (Child != null && Child.Visible) {
				int bw = (int)BorderWidth;
				Gdk.Rectangle childAlloc;
				
				childAlloc.X = bw + Style.XThickness;
				childAlloc.Y = bw + Style.YThickness;
				childAlloc.Width = Math.Max (0, allocation.Width - 2 * (bw + Style.XThickness));
				childAlloc.Height = Math.Max (0, allocation.Height - 2 * (bw + Style.YThickness));
				
				if (GripShown) {
					Gdk.Rectangle gripAlloc = childAlloc;
					Requisition gripReq = grip.SizeRequest ();
					
					if (Orientation == Orientation.Horizontal) {
						childAlloc.X += gripReq.Width;
						childAlloc.Width = Math.Max (0, childAlloc.Width - gripReq.Width);
						gripAlloc.Width = gripReq.Width;
					} else {
						childAlloc.Y += gripReq.Height;
						childAlloc.Height = Math.Max (0, childAlloc.Height - gripReq.Height);
						gripAlloc.Height = gripReq.Height;
					}
					
					grip.SizeAllocate (gripAlloc);
				}

				Child.SizeAllocate (childAlloc);
			}
		}
		
		protected override void OnMapped ()
		{
			SetFlag (WidgetFlags.Mapped);
			
			GdkWindow.Show ();

			if (Child != null && Child.Visible && !Child.IsMapped)
				Child.Map ();
			if (grip != null && grip.Visible && !grip.IsMapped)
				grip.Map ();
		}
		
		protected override void OnUnmapped ()
		{
			ClearFlag (WidgetFlags.Mapped);
			
			GdkWindow.Hide ();
			
			if (grip != null)
				grip.Unmap ();
		}
		
		protected override void OnRealized ()
		{
			WidgetFlags |= WidgetFlags.Realized;
			
			Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
			attributes.X = Allocation.X;
			attributes.Y = Allocation.Y;
			attributes.Height = Allocation.Height;
			attributes.Width = Allocation.Width;
			attributes.WindowType = Gdk.WindowType.Child;
			attributes.Wclass = Gdk.WindowClass.InputOutput;
			attributes.Visual = Visual;
			attributes.Colormap = Colormap;
			attributes.EventMask = (int)(Events |
				Gdk.EventMask.ExposureMask |
				Gdk.EventMask.Button1MotionMask |
				Gdk.EventMask.ButtonPressMask |
				Gdk.EventMask.ButtonReleaseMask);
		
			Gdk.WindowAttributesType attributes_mask =
				Gdk.WindowAttributesType.X |
				Gdk.WindowAttributesType.Y |
				Gdk.WindowAttributesType.Colormap |
				Gdk.WindowAttributesType.Visual;
			GdkWindow = new Gdk.Window (ParentWindow, attributes, (int)attributes_mask);
			GdkWindow.UserData = Handle;

//			I don't know why the following line is needed, but it makes MD crash when
//			the gtk theme changes (e.g. when changing to Glider). It seems to work ok without it.
//			Style = Style.Attach (GdkWindow);

			Style.SetBackground (GdkWindow, State);
			
			GdkWindow.SetBackPixmap (null, true);
			
			if (Child != null)
				Child.ParentWindow = GdkWindow;
			if (grip != null)
				grip.ParentWindow = GdkWindow;
		}
		
		protected override void OnStyleSet (Style style)
		{
			if (IsRealized && !IsNoWindow) {
				Style.SetBackground (GdkWindow, State);
				if (IsDrawable)
					GdkWindow.Clear ();
			}
		}

		protected override void OnDestroyed ()
		{
			if (tabLabel != null)
				tabLabel = null;
			if (menu != null) {
				menu.Detach ();
				menu = null;
			}
			if (grip != null) {
				Remove (grip);
				grip = null;
			}
			if (dockPlaceHolder != null) {
				dockPlaceHolder.Dispose ();
				dockPlaceHolder = null;
			}
			base.OnDestroyed ();
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (IsDrawable && evnt.Window == GdkWindow) {
				Style.PaintBox (Style, GdkWindow, State,
						ShadowType.None, evnt.Area, this,
						"dockitem", 0, 0, -1, -1);
				base.OnExposeEvent (evnt);
			}

			return false;
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (!EventInGripWindow (evnt))
				return false;
			
			bool eventHandled = false;
			bool inHandle;
			Gdk.Cursor cursor = null;
			
			/* Check if user clicked on the drag handle. */      
			switch (Orientation) {
			case Orientation.Horizontal:
				inHandle = evnt.X < grip.Allocation.Width;
				break;
			case Orientation.Vertical:
				inHandle = evnt.Y < grip.Allocation.Height;
				break;
			default:
				inHandle = false;
				break;
			}
			
			/* Left mousebutton click on dockitem. */
			if (!Locked && evnt.Button == 1 && evnt.Type == Gdk.EventType.ButtonPress) {
				/* Set in_drag flag, grab pointer and call begin drag operation. */
				if (inHandle) {
					startX = (int)evnt.X;
					startY = (int)evnt.Y;
					DockObjectFlags |= DockObjectFlags.InPreDrag;
					cursor = new Gdk.Cursor (Display, Gdk.CursorType.Fleur);
					grip.TitleWindow.Cursor = cursor;
					eventHandled = true;
				}
			} else if (!Locked && evnt.Type == Gdk.EventType.ButtonRelease && evnt.Button == 1) {
				if (InDrag) {
					/* User dropped widget somewhere. */
					EndDrag (false);
					eventHandled = true;
				} else if (InPreDrag) {
					DockObjectFlags &= ~(DockObjectFlags.InPreDrag);
					eventHandled = true;
				}
				
				/* we check the window since if the item was redocked it's
				   been unrealized and maybe it's not realized again yet */
				if (grip.TitleWindow != null) {
					cursor = new Gdk.Cursor (Display, Gdk.CursorType.Hand2);
					grip.TitleWindow.Cursor = cursor;
				}
			} else if (evnt.Button == 3 && evnt.Type == Gdk.EventType.ButtonPress && inHandle) {
				DockPopupMenu (evnt.Button, evnt.Time);
				eventHandled = true;
			}

			return eventHandled;
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			return OnButtonPressEvent (evnt);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			if (!EventInGripWindow (evnt))
				return false;

			if (InPreDrag) {
				if (Drag.CheckThreshold (this, startX, startY,
							 (int)evnt.X, (int)evnt.Y)) {
					DockObjectFlags &= ~(DockObjectFlags.InPreDrag);
					dragoffX = startX;
					dragoffY = startY;
					StartDrag ();
				}
			}
			
			if (!InDrag)
				return false;
			
			int newX = (int)evnt.XRoot;
			int newY = (int)evnt.YRoot;
			
			OnDragMotion (newX, newY);
			DockItemMotionHandler handler = DockItemMotion;
			if (handler != null)
				handler (this, newX, newY);
			
			return true;
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (InDrag && evnt.Key == Gdk.Key.Escape) {
				EndDrag (false);
				return true;
			}
			
			return base.OnKeyPressEvent (evnt);
		}
		
		public override bool OnDockRequest (int x, int y, ref DockRequest request)
		{
			/* we get (x,y) in our allocation coordinates system */
			
			/* Get item's allocation. */
			Gdk.Rectangle alloc = Allocation;

			/* Get coordinates relative to our window. */
			int relX = x - alloc.X;
			int relY = y - alloc.Y;
			
			/* Location is inside. */
			if (relX > 0 && relX < alloc.Width &&
			    relY > 0 && relY < alloc.Height) {
				int divider = -1;
				
				/* these are for calculating the extra docking parameter */
				Requisition other = ((DockItem)request.Applicant).PreferredSize;
				Requisition my = PreferredSize;
				
				/* Calculate location in terms of the available space (0-100%). */
				float rx = (float) relX / alloc.Width;
				float ry = (float) relY / alloc.Height;
				
				/* Determine dock location. */
				if (rx < SplitRatio) {
					request.Position = DockPlacement.Left;
					divider = other.Width;
				} else if (rx > (1 - SplitRatio)) {
					request.Position = DockPlacement.Right;
					rx = 1 - rx;
					divider = Math.Max (0, my.Width - other.Width);
				} else if (ry < SplitRatio && ry < rx) {
					request.Position = DockPlacement.Top;
					divider = other.Height;
				} else if (ry > (1 - SplitRatio) && (1 - ry) < rx) {
					request.Position = DockPlacement.Bottom;
					divider = Math.Max (0, my.Height - other.Height);
				} else {
					request.Position = DockPlacement.Center;
				}
				
				/* Reset rectangle coordinates to entire item. */
				request.X = 0;
				request.Y = 0;
				request.Width = alloc.Width;
				request.Height = alloc.Height;
				
				/* Calculate docking indicator rectangle size for new locations.
				   Only do this when we're not over the item's current location. */
				if (request.Applicant != this) {
					switch (request.Position) {
					case DockPlacement.Top:
						request.Height = (int)(request.Height * SplitRatio);
						break;
					case DockPlacement.Bottom:
						request.Y += (int)(request.Height * (1 - SplitRatio));
						request.Height = (int)(request.Height * SplitRatio);
						break;
					case DockPlacement.Left:
						request.Width = (int)(request.Width * SplitRatio);
						break;
					case DockPlacement.Right:
						request.X += (int)(request.Width * (1 - SplitRatio));
						request.Width = (int)(request.Width * SplitRatio);
						break;
					case DockPlacement.Center:
						request.X = (int)(request.Width * SplitRatio / 2);
						request.Y = (int)(request.Height * SplitRatio / 2);
						request.Width = (int)(request.Width * (1 - SplitRatio / 2)) - request.X;
						request.Height = (int)(request.Height * (1 - SplitRatio / 2)) - request.Y;
						break;
					default:
						break;
					}
				}
				
				/* adjust returned coordinates so they have the same
				   origin as our window */
				request.X += alloc.X;
				request.Y += alloc.Y;
				
				/* Set possible target location and return true. */
				request.Target = this;

				/* fill-in other dock information */
				if (request.Position != DockPlacement.Center && divider >= 0)
					request.Extra = divider;

				return true;
			} else { /* No docking possible at this location. */
				return false;
			}
		}
		
		public override void OnDocked (DockObject requestor, DockPlacement position, object data)
		{
			DockObject parent = ParentObject;
			DockObject newParent = null;
			bool addOurselvesFirst = false;
			
			switch (position) {
			case DockPlacement.Top:
			case DockPlacement.Bottom:
				newParent = new DockPaned (Orientation.Vertical);
				addOurselvesFirst = (position == DockPlacement.Bottom);
				break;
			case DockPlacement.Left:
			case DockPlacement.Right:
				newParent = new DockPaned (Orientation.Horizontal);
				addOurselvesFirst = (position == DockPlacement.Right);
				break;
			case DockPlacement.Center:
				// If the parent is already a DockNotebook, we don't need
				// to create a new one.
				if (!(parent is DockNotebook)) {
					newParent = new DockNotebook ();
					addOurselvesFirst = true;
				}
				break;
			default:
				Console.WriteLine ("Unsupported docking strategy");
				return;
			}
			
			if (parent != null)
				parent.Freeze ();

			if (newParent != null) {
				DockObjectFlags |= DockObjectFlags.InReflow;
				Detach (false);
				newParent.Freeze ();
				newParent.Bind (Master);
				
				if (addOurselvesFirst) {
					newParent.Add (this);
					newParent.Add (requestor);
				} else {
					newParent.Add (requestor);
					newParent.Add (this);
				}
				
				if (parent != null)
					parent.Add (newParent);
			
				if (Visible)
					newParent.Show ();
			
				DockObjectFlags &= ~(DockObjectFlags.InReflow);
			
				newParent.Thaw ();
			} else {
				parent.Add (requestor);
			}

			if (position != DockPlacement.Center && data != null && data is System.Int32) {
				if (newParent is DockPaned)
					((DockPaned) newParent).Position = (int) data;
			}
			
			if (requestor.Parent is Notebook) {
				// Activate the page we just added
				Notebook notb = (Notebook) requestor.Parent;
				notb.Page = notb.PageNum (requestor);
			}
			
			if (parent != null)
				parent.Thaw ();
		}
		
		protected virtual void OnDragBegin ()
		{
		}
		
		protected virtual void OnDragEnd (bool cancelled)
		{
		}
		
		protected virtual void OnDragMotion (int x, int y)
		{
		}
		
		private void DetachMenu (Widget item, Menu menu)
		{
			if (item is DockItem)
				((DockItem)item).menu = null;
		}
		
		public void DockPopupMenu (uint button, uint time)
		{
			if (menu == null) {
				// Create popup menu and attach it to the dock item
				menu = new Menu ();
				menu.AttachToWidget (this, new MenuDetachFunc (DetachMenu));
				
				// Hide menuitem
				MenuItem mitem = new MenuItem (Catalog.GetString("Hide"));
				mitem.Activated += new EventHandler (ItemHideCb);
				menu.Append (mitem);

				// Lock menuitem
				CheckMenuItem citem = new CheckMenuItem (Catalog.GetString("Lock"));
				citem.Active = this.Locked;
				citem.Toggled += new EventHandler (ItemLockCb);
				menu.Append (citem);
			}

			menu.ShowAll ();
			menu.Popup (null, null, null, button, time);
		}
		
		private void ItemHideCb (object o, EventArgs e)
		{
			HideItem ();
		}

		private void ItemLockCb (object sender, EventArgs a)
		{
			this.Locked = ((CheckMenuItem)sender).Active;
		}
		
		private void StartDrag ()
		{
			if (!IsRealized)
				Realize ();
			
			DockObjectFlags |= DockObjectFlags.InDrag;
			
			/* grab the pointer so we receive all mouse events */
			Gdk.Cursor fleur = new Gdk.Cursor (Gdk.CursorType.Fleur);
			
			/* grab the keyboard & pointer */
			Grab.Add (this);
			
			OnDragBegin ();
			DockItemDragBeginHandler handler = DockItemDragBegin;
			if (handler != null)
				handler (this);
		}
		
		private void EndDrag (bool cancel)
		{
			/* Release pointer & keyboard. */
			Grab.Remove (Grab.Current);
			
			OnDragEnd (cancel);
			DockItemDragEndHandler handler = DockItemDragEnd;
			if (handler != null)
				handler (this, cancel);
			
			DockObjectFlags &= ~(DockObjectFlags.InDrag);
		}
		
		private void ShowHideGrip ()
		{
			DetachMenu (this, null);

			if (grip != null) {
				Gdk.Cursor cursor = null;

				if (GripShown && !Locked)
					cursor = new Gdk.Cursor (Display, Gdk.CursorType.Hand2);

				if (grip.TitleWindow != null)
					grip.TitleWindow.Cursor = cursor;

				if (GripShown)
					grip.Show ();
				else
					grip.Hide ();
			}
			QueueResize ();
		}
		
		public void DockTo (DockItem target, DockPlacement position)
		{
			if (target == null && position != DockPlacement.Floating)
				return;

			if (position == DockPlacement.Floating || target == null) {
				if (!IsBound) {
					Console.WriteLine ("Attempting to bind an unbound item");
					return;
				}

				// FIXME: save previous docking position for later re-docking?
				
				dragoffX = dragoffY = 0;
				((Dock)Master.Controller).AddFloatingItem (this, 0, 0, -1, -1);
			} else {
				target.Dock (this, position, null);
			}
			if (DockItemShown != null)
				DockItemShown (this, EventArgs.Empty);
		}
		
		public virtual void SetOrientation (Orientation orientation)
		{
			if (Orientation != orientation) {
				this.orientation = orientation;
				if (IsDrawable)
					QueueDraw ();
				QueueResize ();
				EmitPropertyEvent ("orientation");
			}
		}
		
		public void HideGrip ()
		{
			if (GripShown)
				ShowHideGrip ();
		}
		
		public void ShowGrip ()
		{
			if (!GripShown)
				ShowHideGrip ();
		}
		
		public void Bind (Dock dock)
		{
			if (dock == null)
				return;
			
			Bind (dock.Master);
		}
		
		public void HideItem ()
		{					
			if (!IsAttached)
				/* already hidden/detached */
				return;
			
			/* if the object is manual, create a new placeholder to be
			   able to restore the position later */
			if (!IsAutomatic) {
				if (dockPlaceHolder != null)
					dockPlaceHolder.Dispose ();
				dockPlaceHolder = new DockPlaceholder (this, false);
			}
			
			Freeze ();

			/* hide our children first, so they can also set placeholders */
			if (IsCompound)
				Foreach (new Callback (HideChildItem));
			
			Detach (true);
			Thaw ();
			
			if (DockItemHidden != null)
				DockItemHidden (this, EventArgs.Empty);
		}
		
		private void HideChildItem (Widget widget)
		{
			if (!(widget is DockItem))
				return;

			DockItem item = widget as DockItem;
			item.HideItem ();
		}
		
		public void IconifyItem ()
		{
			DockObjectFlags |= DockObjectFlags.Iconified;
			HideItem ();
		}

		public void ShowItem ()
		{
			DockObjectFlags &= ~(DockObjectFlags.Iconified);
			
			if (dockPlaceHolder != null) {
				dockPlaceHolder.Add (this);
				dockPlaceHolder.Dispose ();
				dockPlaceHolder = null;
			} else if (IsBound) {
				if (Master.Controller != null) {
					Master.Controller.Dock (this, DockPlacement.Floating, null);
				}
			}
			if (DockItemShown != null)
				DockItemShown (this, EventArgs.Empty);
		}
		
		public virtual void SetDefaultPosition (DockObject reference)
		{
			if (dockPlaceHolder != null)
				dockPlaceHolder.Dispose ();
			dockPlaceHolder = null;
			
			if (reference != null && reference.IsAttached) {
				if (reference is DockPlaceholder) {
					dockPlaceHolder = (DockPlaceholder)reference;
				} else {
					dockPlaceHolder = new DockPlaceholder (reference, true);
				}
			}
		}
		
		public DockPlaceholder DefaultPosition {
			get { return dockPlaceHolder; }
		}

		private bool EventInGripWindow (Gdk.Event evnt)
		{
			if (grip != null && grip.TitleWindow == evnt.Window)
				return true;
			else
				return false;
		}
	}
}
