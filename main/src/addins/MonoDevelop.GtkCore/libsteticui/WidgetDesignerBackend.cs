//
// WidgetDesignerBackend.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections;
using System.Reflection;
using Gtk;
using Gdk;
using Stetic.Editor;

namespace Stetic
{
	internal class WidgetDesignerBackend: ScrolledWindow
	{
		ObjectWrapper wrapper;
		Gtk.Widget preview;
		ResizableFixed resizableFixed;
		
		static IObjectViewer defaultObjectViewer;
		
		public event EventHandler SelectionChanged;
		
		internal static IObjectViewer DefaultObjectViewer {
			get { return defaultObjectViewer; }
			set { defaultObjectViewer = value; }
		}
		
		public object Selection {
			get { 
				IObjectSelection sel = resizableFixed.GetSelection ();
				if (sel != null)
					return sel.DataObject;
				else
					return null;
			}
		}
		
		internal IDesignArea DesignArea {
			get { return resizableFixed; }
		}
		
		internal WidgetDesignerBackend (Gtk.Container container, int designWidth, int designHeight)
		{
			ShadowType = ShadowType.None;
			HscrollbarPolicy = PolicyType.Automatic;
			VscrollbarPolicy = PolicyType.Automatic;
			
			resizableFixed = new ResizableFixed ();
			resizableFixed.ObjectViewer = defaultObjectViewer;
			
			wrapper = ObjectWrapper.Lookup (container);
			TopLevelWindow window = container as TopLevelWindow;
			
			if (window != null) {
				preview = Stetic.Windows.Preview.Create (window);
				if (preview == null) {
					// Use a regular box.
					EventBox eventBox = new EventBox ();
					eventBox.Add (container);
					preview = eventBox;
				}
			} else {
				EventBox eventBox = new EventBox ();
				eventBox.Add (container);
				preview = eventBox;
			}
			
			resizableFixed.Put (preview, container);

			if (designWidth != -1) {
				preview.WidthRequest = designWidth;
				preview.HeightRequest = designHeight;
				resizableFixed.AllowResize = true;
			} else {
				resizableFixed.AllowResize = false;
			}

			preview.SizeAllocated += new Gtk.SizeAllocatedHandler (OnResized);

			AddWithViewport (resizableFixed);
			
			if (wrapper != null)
				wrapper.AttachDesigner (resizableFixed);
				
			resizableFixed.SelectionChanged += OnSelectionChanged;
		}
		
		protected override void OnDestroyed ()
		{
			if (preview != null) {
				if (wrapper != null)
					wrapper.DetachDesigner (resizableFixed);
				preview.SizeAllocated -= new Gtk.SizeAllocatedHandler (OnResized);
				resizableFixed.SelectionChanged -= OnSelectionChanged;
				resizableFixed = null;
				preview = null;
				wrapper = null;
			}
			base.OnDestroyed ();
		}
		
		public IObjectViewer ObjectViewer {
			get { return resizableFixed.ObjectViewer; }
			set { resizableFixed.ObjectViewer = value; }
		}
		
		public void UpdateObjectViewers ()
		{
			// This method has to be called to ensure that the property
			// and signal viewers show information about the object
			// selected in this designer.
			resizableFixed.UpdateObjectViewers ();
		}
		
		void OnSelectionChanged (object ob, EventArgs a)
		{
			if (SelectionChanged != null)
				SelectionChanged (this, a);
		}
		
		protected override void OnParentSet (Gtk.Widget previousParent)
		{
			base.OnParentSet (previousParent);
			
			if (previousParent != null)
				previousParent.Realized -= OnParentRealized;
			
			if (Parent != null)
				Parent.Realized += OnParentRealized;
		}
		
		void OnParentRealized (object s, EventArgs args)
		{
			if (Parent != null) {
				Parent.Realized -= OnParentRealized;
				ShowAll ();
				
				// Make sure everything is in place before continuing
				while (Gtk.Application.EventsPending ())
					Gtk.Application.RunIteration ();
			}
		}
		
		void OnResized (object s, Gtk.SizeAllocatedArgs a)
		{
			Stetic.Wrapper.Container cont = wrapper as Stetic.Wrapper.Container;
			if (cont != null) {
				if (cont.DesignHeight != DesignHeight)
					cont.DesignHeight = DesignHeight;
				if (cont.DesignWidth != DesignWidth)
					cont.DesignWidth = DesignWidth;
			}
			
			if (DesignSizeChanged != null)
				DesignSizeChanged (this, a);
		}
		
		public int DesignWidth {
			get { return preview.WidthRequest; }
		}
		
		public int DesignHeight {
			get { return preview.HeightRequest; }
		}
		
		public event EventHandler DesignSizeChanged;
	}
	
	class ResizableFixed: EventBox, IDesignArea
	{
		Gtk.Widget child;
		int difX, difY;
		bool resizingX;
		bool resizingY;
		Fixed fixd;
		Gtk.Container container;
		
		Cursor cursorX = new Cursor (CursorType.RightSide);
		Cursor cursorY = new Cursor (CursorType.BottomSide);
		Cursor cursorXY = new Cursor (CursorType.BottomRightCorner);
		
		const int padding = 6;
		const int selectionBorder = 6;
		
		Requisition currentSizeRequest;
		
		SelectionHandleBox selectionBox;
		Gtk.Widget selectionWidget;
		ObjectSelection currentObjectSelection;
		ArrayList topLevels = new ArrayList ();
		ArrayList trackingSize = new ArrayList ();
		
		internal IObjectViewer ObjectViewer;
		internal bool AllowResize;
		
		public event EventHandler SelectionChanged;
		
		public ResizableFixed ()
		{
			GtkWorkarounds.FixContainerLeak (this);
			
			fixd = new Fixed ();
			Add (fixd);
			this.CanFocus = true;
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask | EventMask.KeyPressMask;
//			fixd.ModifyBg (Gtk.StateType.Normal, this.Style.Mid (Gtk.StateType.Normal));
//			VisibleWindow = false;
			selectionBox = new SelectionHandleBox (this);
			selectionBox.Show ();
		}
		
		protected override void OnDestroyed ()
		{
			if (cursorX != null) {
				cursorX.Dispose ();
				cursorXY.Dispose ();
				cursorY.Dispose ();
				cursorX = cursorXY = cursorY = null;
			}
			base.OnDestroyed ();
		}
		
		internal void Put (Gtk.Widget child, Gtk.Container container)
		{
			this.child = child;
			this.container = container;
			fixd.Put (child, selectionBorder + padding, selectionBorder + padding);
			child.SizeRequested += new SizeRequestedHandler (OnSizeReq);
		}
		
		public override void Dispose ()
		{
			if (child != null)
				child.SizeRequested -= new SizeRequestedHandler (OnSizeReq);
			base.Dispose ();
		}
		
		public bool IsSelected (Gtk.Widget widget)
		{
			return selectionWidget == widget;
		}
		
		public IObjectSelection GetSelection (Gtk.Widget widget)
		{
			if (selectionWidget == widget)
				return currentObjectSelection;
			else
				return null;
		}
		
		public IObjectSelection GetSelection ()
		{
			return currentObjectSelection;
		}
		
		public IObjectSelection SetSelection (Gtk.Widget widget, object obj)
		{
			return SetSelection (widget, obj, true);
		}
		
		public IObjectSelection SetSelection (Gtk.Widget widget, object obj, bool allowDrag)
		{
			if (currentObjectSelection != null) {
				currentObjectSelection.Dispose ();
				currentObjectSelection = null;
			}

			if (widget != null) {
				currentObjectSelection = new ObjectSelection (this, widget, obj);
				currentObjectSelection.AllowDrag = allowDrag;
			}
			else
				currentObjectSelection = null;
			
			PlaceSelectionBox (widget);
			// Make sure the selection box is shown before doing anything else.
			// The UI looks more responsive in this way.
//			while (Gtk.Application.EventsPending ())
//				Gtk.Application.RunIteration ();
			
			UpdateObjectViewers ();

			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
			
			return currentObjectSelection;
		}
		
		void PlaceSelectionBox (Gtk.Widget widget)
		{
			if (selectionWidget != null) {
				foreach (Gtk.Widget sw in trackingSize)
					sw.SizeAllocated -= SelectionSizeAllocated;
				selectionWidget.Destroyed -= SelectionDestroyed;
				trackingSize.Clear ();
			}
			
			selectionWidget = widget;
			
			if (widget != null) {
				selectionBox.Hide ();
				
				// This call ensures that the old selection box is removed
				// before the new one is shown
//				while (Gtk.Application.EventsPending ())
//					Gtk.Application.RunIteration ();

				// The selection may have changed while dispatching events
//				if (selectionWidget != widget)
//					return;

				// Track the size changes of the widget and all its parents
				Gtk.Widget sw = selectionWidget;
				while (sw != this && sw != null) {
					sw.SizeAllocated += SelectionSizeAllocated;
					trackingSize.Add (sw);
					sw = sw.Parent;
				}
				
				selectionWidget.Destroyed += SelectionDestroyed;
				PlaceSelectionBoxInternal (selectionWidget);
				selectionBox.ObjectSelection = currentObjectSelection;
				selectionBox.Show ();
			} else {
				selectionBox.Hide ();
			}
		}
		
		public void ResetSelection (Gtk.Widget widget)
		{
			if (selectionWidget == widget || widget == null) {
				PlaceSelectionBox (null);
				if (currentObjectSelection != null) {
					currentObjectSelection.FireDisposed ();
					currentObjectSelection = null;
					
					// This makes the property editor to flicker
					// when changing widget selection
					// UpdateObjectViewers ();
				}
				if (widget == null) {
					Gtk.Container cc = this as Gtk.Container;
					while (cc.Parent != null)
						cc = cc.Parent as Gtk.Container;
					if (cc is Gtk.Window) {
						((Gtk.Window)cc).Focus = this;
					}
				}
			}
		}
		
		public void UpdateObjectViewers ()
		{
			object obj;
			
			if (currentObjectSelection == null)
				obj = null;
			else
				obj = currentObjectSelection.DataObject;
			
			if (ObjectViewer != null)
				ObjectViewer.TargetObject = obj;
		}
		
		public void AddWidget (Gtk.Widget w, int x, int y)
		{
			w.Parent = this;
			TopLevelChild info = new TopLevelChild ();
			info.X = x;
			info.Y = y;
			info.Child = w;
			topLevels.Add (info);
		}
		
		public void RemoveWidget (Gtk.Widget w)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == w) {
					w.Unparent ();
					topLevels.Remove (info);
					break;
				}
			}
		}
		
		public void MoveWidget (Gtk.Widget w, int x, int y)
		{
			foreach (TopLevelChild info in topLevels) {
				if (info.Child == w) {
					info.X = x;
					info.Y = y;
					QueueResize ();
					break;
				}
			}
		}
		
		public Gdk.Rectangle GetCoordinates (Gtk.Widget w)
		{
			int px, py;
			if (!w.TranslateCoordinates (this, 0, 0, out px, out py))
				return new Gdk.Rectangle (0,0,0,0);

			Gdk.Rectangle rect = w.Allocation;
			rect.X = px - Allocation.X;
			rect.Y = py - Allocation.Y;
			return rect;
		}
		
		void SelectionSizeAllocated (object obj, Gtk.SizeAllocatedArgs args)
		{
			PlaceSelectionBoxInternal (selectionWidget);
		}
		
		void SelectionDestroyed (object obj, EventArgs args)
		{
			ResetSelection ((Gtk.Widget)obj);
		}

		void PlaceSelectionBoxInternal (Gtk.Widget widget)
		{
			int px, py;
			if (!widget.TranslateCoordinates (this, 0, 0, out px, out py))
				return;

			Gdk.Rectangle rect = widget.Allocation;
			rect.X = px;
			rect.Y = py;
			selectionBox.Reposition (rect);
		}
		
		
		void OnSizeReq (object o, SizeRequestedArgs a)
		{
			if (!AllowResize) {
				a.RetVal = false;
				QueueDraw ();
				return;
			}

			currentSizeRequest = a.Requisition;
			
			Rectangle alloc = child.Allocation;
			int nw = alloc.Width;
			int nh = alloc.Height;
			
			if (a.Requisition.Width > nw) nw = a.Requisition.Width;
			if (a.Requisition.Height > nh) nh = a.Requisition.Height;
			
			if (nw != alloc.Width || nh != alloc.Height) {
				int ow = child.WidthRequest;
				int oh = child.HeightRequest;
				child.SetSizeRequest (nw, nh);
				if (ow > nw)
					child.WidthRequest = ow;
				if (oh > nh)
					child.HeightRequest = oh;
				QueueDraw ();
			}
		}
		
		protected override void OnSizeRequested (ref Requisition req)
		{
			// The Toplevel check is done to ensure that this widget is anchored,
			// otherwise the Realize call will fail.
			if (!child.IsRealized && Toplevel is Gtk.Window)
				child.Realize ();
			
			req = child.SizeRequest ();
			// Make some room for the border
			req.Width += padding * 2 + selectionBorder * 2;
			req.Height += padding * 2 + selectionBorder * 2;
			selectionBox.SizeRequest ();
			if (selectionBox.Allocation.Width > req.Width)
				req.Width = selectionBox.Allocation.Width;
			if (selectionBox.Allocation.Height > req.Height)
				req.Height = selectionBox.Allocation.Height;

			foreach (TopLevelChild tchild in topLevels) {
				Gtk.Requisition treq = tchild.Child.SizeRequest ();
				if (tchild.X + treq.Width > req.Width)
					req.Width = tchild.X + treq.Width;
				if (tchild.Y + treq.Height > req.Height)
					req.Height = tchild.Y + treq.Height;
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle rect)
		{
			base.OnSizeAllocated (rect);

			if (selectionWidget != null)
				PlaceSelectionBoxInternal (selectionWidget);
			
			foreach (TopLevelChild child in topLevels) {
				Gtk.Requisition req = child.Child.SizeRequest ();
				child.Child.SizeAllocate (new Gdk.Rectangle (rect.X + child.X, rect.Y + child.Y, req.Width, req.Height));
			}
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			base.ForAll (include_internals, callback);
			foreach (TopLevelChild child in topLevels)
				callback (child.Child);
			if (include_internals)
				selectionBox.ForAll (include_internals, callback);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion ev)
		{
			if (resizingX || resizingY) {
				if (resizingX) {
					int nw = (int)(ev.X - difX - padding - selectionBorder);
					if (nw < currentSizeRequest.Width) nw = currentSizeRequest.Width;
					child.WidthRequest = nw;
				}
				
				if (resizingY) {
					int nh = (int)(ev.Y - difY - padding - selectionBorder);
					if (nh < currentSizeRequest.Height) nh = currentSizeRequest.Height;
					child.HeightRequest = nh;
				}
				QueueDraw ();
			} else if (AllowResize) {
				if (GetAreaResizeXY ().Contains ((int) ev.X, (int) ev.Y))
					GdkWindow.Cursor = cursorXY;
				else if (GetAreaResizeX ().Contains ((int) ev.X, (int) ev.Y))
					GdkWindow.Cursor = cursorX;
				else if (GetAreaResizeY ().Contains ((int) ev.X, (int) ev.Y))
					GdkWindow.Cursor = cursorY;
				else
					GdkWindow.Cursor = null;
			}
			
			return base.OnMotionNotifyEvent (ev);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			Gdk.Rectangle rectArea = child.Allocation;
			rectArea.Inflate (selectionBorder, selectionBorder);
			
			if (rectArea.Contains ((int) ev.X, (int) ev.Y)) {
				Stetic.Wrapper.Widget gw = Stetic.Wrapper.Widget.Lookup (container);
				if (gw != null)
					gw.Select ();
				else
					ResetSelection (null);
					
				if (AllowResize) {
					Rectangle rect = GetAreaResizeXY ();
					if (rect.Contains ((int) ev.X, (int) ev.Y)) {
						resizingX = resizingY = true;
						difX = (int) (ev.X - rect.X);
						difY = (int) (ev.Y - rect.Y);
						GdkWindow.Cursor = cursorXY;
					}
					
					rect = GetAreaResizeY ();
					if (rect.Contains ((int) ev.X, (int) ev.Y)) {
						resizingY = true;
						difY = (int) (ev.Y - rect.Y);
						GdkWindow.Cursor = cursorY;
					}
					
					rect = GetAreaResizeX ();
					if (rect.Contains ((int) ev.X, (int) ev.Y)) {
						resizingX = true;
						difX = (int) (ev.X - rect.X);
						GdkWindow.Cursor = cursorX;
					}
				}
			} else {
				Stetic.Wrapper.Widget gw = Stetic.Wrapper.Widget.Lookup (container);
				if (gw != null)
					gw.Project.Selection = null;
			}
			
			return base.OnButtonPressEvent (ev);
		}
		
		Rectangle GetAreaResizeY ()
		{
			Gdk.Rectangle rect = child.Allocation;
			return new Gdk.Rectangle (rect.X - selectionBorder, rect.Y + rect.Height, rect.Width + selectionBorder, selectionBorder);
		}
		
		Rectangle GetAreaResizeX ()
		{
			Gdk.Rectangle rect = child.Allocation;
			return new Gdk.Rectangle (rect.X + rect.Width, rect.Y - selectionBorder, selectionBorder, rect.Height + selectionBorder);
		}
		
		Rectangle GetAreaResizeXY ()
		{
			Gdk.Rectangle rect = child.Allocation;
			return new Gdk.Rectangle (rect.X + rect.Width, rect.Y + rect.Height, selectionBorder, selectionBorder);
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton ev)
		{
			resizingX = resizingY = false;
			GdkWindow.Cursor = null;
			return base.OnButtonReleaseEvent (ev);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			bool r = base.OnExposeEvent (ev);
			//FIXME Disabled checkerboard background because it's very inefficient and makes the control *very* slow to resize
			// It should take the EventExpose area into account, invalidate more selectively during resizes (GTK viewport 
			// code would probably be a good start), and take advantage of the flat block color the parent is rendering.
			/*
			int size = 8;
			bool squareColor = true;
			bool startsquareColor = true;
			int x1 = 0;
			int x2 = Allocation.Width;
			int y1 = 0;
			int y2 = Allocation.Height;
			for (int y = y1; y < y2; y += size) {
				squareColor = startsquareColor;
				startsquareColor = !startsquareColor;
				for (int x = x1; x < x2; x += size) {
					GdkWindow.DrawRectangle (squareColor ? Style.DarkGC (StateType.Normal) : Style.DarkGC (StateType.Active), true, x, y, size, size);
					squareColor = !squareColor;
				}
			}

			foreach (Widget cw in Children)
				PropagateExpose (cw, ev);*/

			Gdk.Rectangle rect = child.Allocation;
			GdkWindow.DrawRectangle (this.Style.BackgroundGC (StateType.Normal), true, rect.X, rect.Y, rect.Width, rect.Height);
			
			Pixbuf sh = Shadow.AddShadow (rect.Width, rect.Height);
			GdkWindow.DrawPixbuf (this.Style.BackgroundGC (StateType.Normal), sh, 0, 0, rect.X - 5, rect.Y - 5, sh.Width, sh.Height, RgbDither.None, 0, 0); 
			return r;
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey ev)
		{
			switch (ev.Key) {
			case Gdk.Key.Delete:
			case Gdk.Key.KP_Delete:
				IObjectSelection sel = GetSelection ();
				if (sel != null && sel.DataObject != null) {
					Wrapper.Widget wrapper = Wrapper.Widget.Lookup (sel.DataObject) as Wrapper.Widget;
					if (wrapper != null && !wrapper.IsTopLevel)
						wrapper.Delete ();
				}
				return true;
			default:
				return base.OnKeyPressEvent (ev);
			}
		}

		class TopLevelChild
		{
			public int X;
			public int Y;
			public Gtk.Widget Child;
		}
	}
	
	enum BoxPos
	{
		Start,
		Center,
		End,
	}
	
	enum BoxFill
	{
		Box,
		VLine,
		HLine
	}
	
	class SelectionHandleBox
	{
		const int selectionHandleSize = 4;
		const int topSelectionHandleSize = 8;
		public const int selectionLineWidth = 2;
		
		ArrayList selection = new ArrayList ();
		Gdk.Rectangle allocation;
		SelectionHandlePart dragHandlePart;
		public ObjectSelection ObjectSelection;
		
		public SelectionHandleBox (Gtk.Widget parent)
		{
			// Lines
			selection.Add (new SelectionHandlePart (BoxFill.HLine, BoxPos.Start, BoxPos.Start, 0, -selectionLineWidth, BoxPos.End, BoxPos.Start, 0, 0));
			selection.Add (new SelectionHandlePart (BoxFill.HLine, BoxPos.Start, BoxPos.End, 0, 0, BoxPos.End, BoxPos.End, 0, selectionLineWidth));
			selection.Add (new SelectionHandlePart (BoxFill.VLine, BoxPos.Start, BoxPos.Start, -selectionLineWidth, 0, BoxPos.Start, BoxPos.End, 0, 0));
			selection.Add (new SelectionHandlePart (BoxFill.VLine, BoxPos.End, BoxPos.Start, 0, 0, BoxPos.End, BoxPos.End, selectionLineWidth, 0));
			
			// Handles
			selection.Add (new SelectionHandlePart (BoxFill.Box, BoxPos.Start, BoxPos.Start, -selectionHandleSize, -selectionHandleSize, BoxPos.Start, BoxPos.Start, 0, 0));
			selection.Add (new SelectionHandlePart (BoxFill.Box, BoxPos.End, BoxPos.Start, 0, -selectionHandleSize, BoxPos.End, BoxPos.Start, selectionHandleSize, 0));
			selection.Add (new SelectionHandlePart (BoxFill.Box, BoxPos.Start, BoxPos.End, -selectionHandleSize, 0, BoxPos.Start, BoxPos.End, 0, selectionHandleSize));
			selection.Add (new SelectionHandlePart (BoxFill.Box, BoxPos.End, BoxPos.End, 0, 0, BoxPos.End, BoxPos.End, selectionHandleSize, selectionHandleSize));
			
			selection.Add (new SelectionHandlePart (BoxFill.Box, BoxPos.Center, BoxPos.Start, -selectionHandleSize/2, -selectionHandleSize, BoxPos.Center, BoxPos.Start, selectionHandleSize/2, 0));
			selection.Add (new SelectionHandlePart (BoxFill.Box, BoxPos.Center, BoxPos.End, -selectionHandleSize/2, 0, BoxPos.Center, BoxPos.End, selectionHandleSize/2, selectionHandleSize));
			selection.Add (new SelectionHandlePart (BoxFill.Box, BoxPos.Start, BoxPos.Center, -selectionHandleSize, -selectionHandleSize/2, BoxPos.Start, BoxPos.Center, 0, selectionHandleSize/2));
			selection.Add (new SelectionHandlePart (BoxFill.Box, BoxPos.End, BoxPos.Center, 0, -selectionHandleSize/2, BoxPos.End, BoxPos.Center, selectionHandleSize, selectionHandleSize/2));
			
			dragHandlePart = new SelectionHandlePart (BoxFill.Box, BoxPos.Center, BoxPos.Start, -topSelectionHandleSize/2, -topSelectionHandleSize, BoxPos.Center, BoxPos.Start, topSelectionHandleSize/2, 0);
			selection.Add (dragHandlePart);
			
			foreach (SelectionHandlePart s in selection) {
				s.Parent = parent;
				s.ParentBox = this;
			}
		}
		
		public void Show ()
		{
			foreach (Gtk.Widget s in selection)
				s.Show ();
			dragHandlePart.Visible = (ObjectSelection != null && ObjectSelection.AllowDrag);
		}
		
		public void Hide ()
		{
			foreach (Gtk.Widget s in selection)
				s.Hide ();
		}
		
		public void Reposition (Gdk.Rectangle rect)
		{
			bool firstRect = true;
			
			foreach (SelectionHandlePart s in selection) {
				s.Reposition (rect);
				Gdk.Rectangle r = s.Allocation;
				if (firstRect) {
					allocation = r;
					firstRect = false;
				} else
					allocation = allocation.Union (r);
			}
		}
		
		public Gdk.Rectangle Allocation {
			get { return allocation; }
		}
		
		public void ForAll (bool include_internals, Gtk.Callback callback)
		{
			foreach (Gtk.Widget s in selection)
				callback (s);
		}
		
		public void SizeRequest ()
		{
			foreach (Gtk.Widget s in selection)
				s.SizeRequest ();
		}
	}
	
	class SelectionHandlePart: EventBox
	{
		BoxPos hpos, vpos;
		BoxPos hposEnd, vposEnd;
		int x, y;
		int xEnd, yEnd;
		BoxFill fill;
		public SelectionHandleBox ParentBox;
		int clickX, clickY;
		int localClickX, localClickY;
		int ox, oy;
		
		public SelectionHandlePart (BoxFill fill, BoxPos hpos, BoxPos vpos, int x, int y, BoxPos hposEnd, BoxPos vposEnd, int xEnd, int yEnd)
		{
			this.fill = fill;
			this.hpos = hpos;
			this.vpos = vpos;
			this.x = x;
			this.y = y;
			
			this.hposEnd = hposEnd;
			this.vposEnd = vposEnd;
			this.xEnd = xEnd;
			this.yEnd = yEnd;
		}
		
		public void Reposition (Gdk.Rectangle rect)
		{
			int px, py;
			int pxEnd, pyEnd;
			
			CalcPos (rect, hpos, vpos, out px, out py);
			px += x;
			py += y;
			
			CalcPos (rect, hposEnd, vposEnd, out pxEnd, out pyEnd);
			pxEnd += xEnd;
			pyEnd += yEnd;
			
			Allocation = new Gdk.Rectangle (px, py, pxEnd - px, pyEnd - py);
			ox = rect.X;
			oy = rect.Y;
		}
		
		void CalcPos (Gdk.Rectangle rect, BoxPos hp, BoxPos vp, out int px, out int py)
		{
			if (vp == BoxPos.Start)
				py = rect.Y;
			else if (vp == BoxPos.End)
				py = rect.Bottom;
			else
				py = rect.Y + rect.Height / 2;
			
			if (hp == BoxPos.Start)
				px = rect.X;
			else if (hp == BoxPos.End)
				px = rect.Right;
			else
				px = rect.X + rect.Width / 2;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			int w, h;
			this.GdkWindow.GetSize (out w, out h);
			
			if (fill == BoxFill.Box) {
				this.GdkWindow.DrawRectangle (this.Style.WhiteGC, true, 0, 0, w, h);
				this.GdkWindow.DrawRectangle (this.Style.BlackGC, false, 0, 0, w-1, h-1);
			} else if (fill == BoxFill.HLine) {
				using (Gdk.GC gc = new Gdk.GC (this.GdkWindow)) {
					gc.SetDashes (0, new sbyte [] { 1, 1 }, 2);
					gc.SetLineAttributes (SelectionHandleBox.selectionLineWidth, Gdk.LineStyle.OnOffDash, Gdk.CapStyle.NotLast, Gdk.JoinStyle.Miter);
					gc.Foreground = this.Style.Black;
					this.GdkWindow.DrawLine (gc, 0, h / 2, w, h / 2);
					gc.Foreground = this.Style.White;
					this.GdkWindow.DrawLine (gc, 1, h/2, w, h/2);
				}
			} else {
				using (Gdk.GC gc = new Gdk.GC (this.GdkWindow)) {
					gc.SetDashes (0, new sbyte [] { 1, 1 }, 2);
					gc.SetLineAttributes (SelectionHandleBox.selectionLineWidth, Gdk.LineStyle.OnOffDash, Gdk.CapStyle.NotLast, Gdk.JoinStyle.Miter);
					gc.Foreground = this.Style.Black;
					this.GdkWindow.DrawLine (gc, w / 2, 0, w / 2, h);
					gc.Foreground = this.Style.White;
					this.GdkWindow.DrawLine (gc, w / 2, 1, w/2, h);
				}
			}
			
			return true;
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evb)
		{
			if (evb.Type == Gdk.EventType.ButtonPress && evb.Button == 1 && !GtkWorkarounds.TriggersContextMenu (evb)) {
				clickX = (int)evb.XRoot;
				clickY = (int)evb.YRoot;
				localClickX = (int) evb.X;
				localClickY = (int) evb.Y;
			}
			return true;
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evm)
		{
			if ((evm.State & Gdk.ModifierType.Button1Mask) == 0)
				return false;

			if (!Gtk.Drag.CheckThreshold (this, clickX, clickY, (int)evm.XRoot, (int)evm.YRoot))
				return false;

			if (ParentBox.ObjectSelection != null && ParentBox.ObjectSelection.AllowDrag) {
				int dx = Allocation.X - ox + localClickX;
				int dy = Allocation.Y - oy + localClickY;
				ParentBox.ObjectSelection.FireDrag (evm, dx, dy);
			}

			return true;
		}
	}
	
	class ObjectSelection: IObjectSelection
	{
		ResizableFixed box;
		Gtk.Widget widget;
		object dataObject;
		bool allowDrag = true;
		
		public ObjectSelection (ResizableFixed box, Gtk.Widget widget, object dataObject)
		{
			this.box = box;
			this.widget = widget;
			this.dataObject = dataObject;
		}
		
		public Gtk.Widget Widget {
			get { return widget; }
		}
		
		public object DataObject {
			get { return dataObject; }
		}
		
		public void Dispose ()
		{
			box.ResetSelection (widget);
		}
		
		internal void FireDisposed ()
		{
			if (Disposed != null)
				Disposed (this, EventArgs.Empty);
		}
		
		internal void FireDrag (Gdk.EventMotion evt, int dx, int dy)
		{
			if (Drag != null)
				Drag (evt, dx, dy);
		}
		
		public bool AllowDrag {
			get { return allowDrag; }
			set { allowDrag = value; }
		}
		
		public event DragDelegate Drag;
		public event EventHandler Disposed;
	}
}
