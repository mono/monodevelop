//
// DockItemContainer.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Components.Docking
{
	class DockItemContainer
	{
		IDockItemContainerControl containerControl;
		DockItem item;

		public DockItemContainer (DockFrame frame, DockItem item)
		{
			containerControl = new DockItemContainerControl ();
			this.item = item;

			containerControl.Initialize(this, frame, item);
		}

		public DockVisualStyle VisualStyle {
			get { return containerControl.VisualStyle; }
			set { containerControl.VisualStyle = value; }
		}

		 internal IDockItemContainerControl ContainerControl {
		 	get {
		 		return containerControl;
		 	}
		 }

		public Gtk.Requisition SizeRequest ()
		{
			return containerControl.SizeRequest();
		}

		public void SizeAllocate (Gdk.Rectangle rect)
		{
			containerControl.SizeAllocate(rect);
		}

		public void QueueResize ()
		{
			containerControl.QueueResize();
		}

		public void UpdateContent ()
		{
			containerControl.UpdateContent ();
		}
		/*
		public void GetOriginInWindow (out int x, out int y)
		{
			containerControl.GetOriginInWindow(out x, out y);
		}
*/
		public void Show ()
		{
			containerControl.Show();
		}

		public void Hide ()
		{
			containerControl.Hide ();
		}

		public bool Visible {
			get {
				return containerControl.Visible;
			}

			set {
				containerControl.Visible = value;
			}
		}

		public Control Control {
			get {
				return containerControl.AsControl;
			}
		}

		public Gdk.Rectangle FloatRect {
			get {
				return containerControl.FloatRect;
			}
		}

		public void RemoveFromParent ()
		{
			containerControl.RemoveFromParent();
		}

		public bool ContainsPointer {
			get {
				return containerControl.ContainsPointer;
			}
		}

		public bool HasFocusChild {
			get {
				return containerControl.HasFocusChild;
			}
		}

		public void ClearFocus ()
		{
			containerControl.ClearFocus();
		}

		public bool ContentVisible {
			get {
				return containerControl.ContentVisible;
			}
		}

		public void FocusContent ()
		{
			containerControl.FocusContent();
		}

		public bool GetContentHasFocus (DockItemStatus status)
		{
			return containerControl.GetContentHasFocus(status);
		}

		public event EventHandler<EventArgs> Shown;
		public event EventHandler<EventArgs> Hidden;
		public event EventHandler<EventArgs> ParentSet;

		internal void OnShown ()
		{
			Shown?.Invoke (this, EventArgs.Empty);
		}

		internal void OnHidden ()
		{
			Hidden?.Invoke (this, EventArgs.Empty);
		}

		internal void OnParentSet ()
		{
			ParentSet?.Invoke (this, EventArgs.Empty);
		}
	}

	internal interface IDockItemContainerControl {
		void Initialize (DockItemContainer parentContainer, DockFrame frame, DockItem item);
		DockVisualStyle VisualStyle { get; set; }
		bool Visible { get; set; }
		Gdk.Rectangle FloatRect { get; }

		void UpdateContent ();

		Gtk.Requisition SizeRequest ();
		void SizeAllocate (Gdk.Rectangle rect);
		void QueueResize ();
		//void GetOriginInWindow (out int x, out int y);
		void Show ();
		void Hide ();
		void RemoveFromParent ();

		bool ContainsPointer { get; }
		bool HasFocusChild { get; }
		void ClearFocus ();
		void FocusContent ();
		bool ContentVisible { get; }
		bool GetContentHasFocus (DockItemStatus status);

		Control AsControl { get; }
	}

	class DockItemContainerControl : EventBox, IDockItemContainerControl
	{
		DockItem item;
		Widget widget;
		Container borderFrame;
		Box contentBox;
		VBox mainBox;
		DockItemContainer parentContainer;

		public DockItemContainerControl ()
		{
			
		}

		public void Initialize (DockItemContainer parentContainer, DockFrame frame, DockItem item)
		{
			this.item = item;
			item.LabelChanged += UpdateAccessibilityLabel;

			this.parentContainer = parentContainer;

			mainBox = new VBox ();
			mainBox.Accessible.SetShouldIgnore (false);
			UpdateAccessibilityLabel (null, null);
			Add (mainBox);

			mainBox.ResizeMode = Gtk.ResizeMode.Queue;
			mainBox.Spacing = 0;

			ShowAll ();

			IDockItemToolbarControl toolbar;
			toolbar = item.GetToolbar (DockPositionType.Top).Toolbar;
			Widget tbWidget = toolbar as Widget;

			if (tbWidget == null) {
				throw new ToolkitMismatchException ();
			}
			mainBox.PackStart (tbWidget, false, false, 0);

			HBox hbox = new HBox ();
			hbox.Accessible.SetTitle ("Hbox");
			hbox.Show ();

			toolbar = item.GetToolbar (DockPositionType.Left).Toolbar;
			tbWidget = toolbar as Widget;

			if (tbWidget == null) {
				throw new ToolkitMismatchException ();
			}
			hbox.PackStart (tbWidget, false, false, 0);

			contentBox = new HBox ();
			hbox.Accessible.SetTitle ("Content");
			contentBox.Show ();
			hbox.PackStart (contentBox, true, true, 0);

			toolbar = item.GetToolbar (DockPositionType.Right).Toolbar;
			tbWidget = toolbar as Widget;

			if (tbWidget == null) {
				throw new ToolkitMismatchException ();
			}
			hbox.PackStart (tbWidget, false, false, 0);

			mainBox.PackStart (hbox, true, true, 0);

			toolbar = item.GetToolbar (DockPositionType.Bottom).Toolbar;
			tbWidget = toolbar as Widget;

			if (tbWidget == null) {
				throw new ToolkitMismatchException ();
			}
			mainBox.PackStart (tbWidget, false, false, 0);
		}

		public Control AsControl {
			get {
				return (Widget)this;
			}
		}

		void UpdateAccessibilityLabel (object sender, EventArgs args)
		{
			mainBox.Accessible.SetTitle (Core.GettextCatalog.GetString ("{0} Pad", item.Label));
		}

		DockVisualStyle visualStyle;
		public DockVisualStyle VisualStyle {
			get {
				return visualStyle;
			}
			set {
				visualStyle = value;
				UpdateVisualStyle();
			}
		}

        protected override void OnDestroyed()
        {
			item.LabelChanged -= UpdateAccessibilityLabel;
			base.OnDestroyed();
        }

        public void UpdateContent ()
		{
			if (widget != null)
				contentBox.Remove (widget);
			widget = item.Content;

			if (item.DrawFrame) {
				if (borderFrame == null) {
					borderFrame = new CustomFrame (1, 1, 1, 1);
					borderFrame.Show ();
					contentBox.Add (borderFrame);
				}
				if (widget != null) {
					borderFrame.Add (widget);
					widget.Show ();
				}
			}
			else if (widget != null) {
				if (borderFrame != null) {
					contentBox.Remove (borderFrame);
					borderFrame = null;
				}
				contentBox.Add (widget);
				widget.Show ();
			}
			UpdateVisualStyle ();
		}

		void UpdateVisualStyle ()
		{
			if (VisualStyle != null) {
				if (widget != null)
					SetTreeStyle (widget);

				item.GetToolbar (DockPositionType.Top).SetStyle (VisualStyle);
				item.GetToolbar (DockPositionType.Left).SetStyle (VisualStyle);
				item.GetToolbar (DockPositionType.Right).SetStyle (VisualStyle);
				item.GetToolbar (DockPositionType.Bottom).SetStyle (VisualStyle);

				if (VisualStyle.TabStyle == DockTabStyle.Normal)
					ModifyBg (StateType.Normal, VisualStyle.PadBackgroundColor.Value.ToGdkColor ());
				else 
					ModifyBg (StateType.Normal, Style.Background(StateType.Normal));
			}
		}

		public bool ContainsPointer {
			get {
				int px, py;
				GetPointer (out px, out py);
				if (Visible && IsRealized && Allocation.Contains (px + Allocation.X, py + Allocation.Y))
					return true;
				return false;
			}
		}

		public bool HasFocusChild {
			get {
				return FocusChild != null;
			}
		}

		public void ClearFocus ()
		{
			FocusChild = null;
		}

		public void FocusContent ()
		{
			GtkUtil.SetFocus (widget);
		}

		public bool ContentVisible {
			get {
				// FIXME: Should this just check the mapped status?
				// this check will say the content is visible if the parent is not visible.
				return Parent != null && Visible;
			}
		}

		public bool GetContentHasFocus (DockItemStatus status)
		{
			if (widget.HasFocus || HasFocus)
				return true;

			Gtk.Window win = widget.Toplevel as Gtk.Window;
			if (win != null) {
				if (status == DockItemStatus.AutoHide)
					return win.HasToplevelFocus;
				return (win.HasToplevelFocus && win.Focus?.IsChildOf (widget) == true);
			}

			return false;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (VisualStyle.TabStyle == DockTabStyle.Normal) {
				Gdk.GC gc = new Gdk.GC (GdkWindow);
				gc.RgbFgColor = VisualStyle.PadBackgroundColor.Value.ToGdkColor ();
				evnt.Window.DrawRectangle (gc, true, Allocation);
				gc.Dispose ();
			}
			return base.OnExposeEvent (evnt);
		}

		void SetTreeStyle (Gtk.Widget w)
		{
			if (w is Gtk.TreeView) {
				if (w.IsRealized)
					OnTreeRealized (w, null);
				else
					w.Realized += OnTreeRealized;
			}
			else {
				var c = w as Gtk.Container;
				if (c != null) {
					foreach (var cw in c.Children)
						SetTreeStyle (cw);
				}
			}
		}

		void OnTreeRealized (object sender, EventArgs e)
		{
			var w = (Gtk.TreeView)sender;
			if (VisualStyle.TreeBackgroundColor != null) {
				w.ModifyBase (StateType.Normal, VisualStyle.TreeBackgroundColor.Value.ToGdkColor ());
				w.ModifyBase (StateType.Insensitive, VisualStyle.TreeBackgroundColor.Value.ToGdkColor ());
			} else {
				w.ModifyBase (StateType.Normal, Parent.Style.Base (StateType.Normal));
				w.ModifyBase (StateType.Insensitive, Parent.Style.Base (StateType.Insensitive));
			}
		}

		void OnClickDock (object s, EventArgs a)
		{
			if (item.Status == DockItemStatus.AutoHide || item.Status == DockItemStatus.Floating)
				item.Status = DockItemStatus.Dockable;
			else
				item.Status = DockItemStatus.AutoHide;
		}

		/*
		public void GetOriginInWindow (out int x, out int y)
		{
			TranslateCoordinates(Toplevel, 0, 0, out x, out y);
		}
*/
		public Gdk.Rectangle FloatRect {
			get {
				int x, y;
				TranslateCoordinates(Toplevel, 0, 0, out x, out y);

				Gtk.Window win = Toplevel as Gtk.Window;
				if (win != null) {
					int wx, wy;
					win.GetPosition (out wx, out wy);
					return new Gdk.Rectangle (wx + x, wy + y, Allocation.Width, Allocation.Height);
				}

				return Gdk.Rectangle.Zero;
			}
		}

		public void RemoveFromParent ()
		{
			if (Parent == null) {
				return;
			}

			((Gtk.Container)Parent).Remove (this);
		}

		protected override void OnShown ()
		{
			parentContainer.OnShown ();
			base.OnShown ();
		}

		protected override void OnHidden ()
		{
			parentContainer.OnHidden ();
			base.OnHidden ();
		}

		protected override void OnParentSet (Widget previous_parent)
		{
			parentContainer.OnParentSet ();
			base.OnParentSet (previous_parent);
		}
	}

	class CustomFrame: Bin
	{
		Gtk.Widget child;
		int topMargin;
		int bottomMargin;
		int leftMargin;
		int rightMargin;
		
		int topPadding;
		int bottomPadding;
		int leftPadding;
		int rightPadding;

		Gdk.Color backgroundColor;
		Gdk.Color borderColor;
		bool backgroundColorSet, borderColorSet;
		
		public CustomFrame ()
		{
		}
		
		public CustomFrame (int topMargin, int bottomMargin, int leftMargin, int rightMargin)
		{
			SetMargins (topMargin, bottomMargin, leftMargin, rightMargin);
		}

		protected override void OnStyleSet (Style previous_style)
		{
			base.OnStyleSet (previous_style);
			if (!borderColorSet)
				borderColor = Style.Dark (Gtk.StateType.Normal);
		}
		
		public void SetMargins (int topMargin, int bottomMargin, int leftMargin, int rightMargin)
		{
			this.topMargin = topMargin;
			this.bottomMargin = bottomMargin;
			this.leftMargin = leftMargin;
			this.rightMargin = rightMargin;
		}
		
		public void SetPadding (int topPadding, int bottomPadding, int leftPadding, int rightPadding)
		{
			this.topPadding = topPadding;
			this.bottomPadding = bottomPadding;
			this.leftPadding = leftPadding;
			this.rightPadding = rightPadding;
		}
		
		public bool GradientBackround { get; set; }

		public Gdk.Color BackgroundColor {
			get { return backgroundColor; }
			set { backgroundColor = value; backgroundColorSet = true; }
		}

		public Gdk.Color BorderColor {
			get { return borderColor; }
			set { borderColor = value; borderColorSet = true; }
		}

		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			child = widget;
		}
		
		protected override void OnRemoved (Widget widget)
		{
			base.OnRemoved (widget);
			child = null;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (child != null) {
				requisition = child.SizeRequest ();
				requisition.Width += leftMargin + rightMargin + leftPadding + rightPadding;
				requisition.Height += topMargin + bottomMargin + topPadding + bottomPadding;
			} else {
				requisition.Width = 0;
				requisition.Height = 0;
			}
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (allocation.Width > leftMargin + rightMargin + leftPadding + rightPadding) {
				allocation.X += leftMargin + leftPadding;
				allocation.Width -= leftMargin + rightMargin + leftPadding + rightPadding;
			}
			if (allocation.Height > topMargin + bottomMargin + topPadding + bottomPadding) {
				allocation.Y += topMargin + topPadding;
				allocation.Height -= topMargin + bottomMargin + topPadding + bottomPadding;
			}
			if (child != null)
				child.SizeAllocate (allocation);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Gdk.Rectangle rect = Allocation;
			
			//Gdk.Rectangle.Right and Bottom are inconsistent
			int right = rect.X + rect.Width, bottom = rect.Y + rect.Height;

			var bcolor = backgroundColorSet ? BackgroundColor : Style.Background (Gtk.StateType.Normal);
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
			
				if (GradientBackround) {
					cr.NewPath ();
					cr.MoveTo (rect.X, rect.Y);
					cr.RelLineTo (rect.Width, 0);
					cr.RelLineTo (0, rect.Height);
					cr.RelLineTo (-rect.Width, 0);
					cr.RelLineTo (0, -rect.Height);
					cr.ClosePath ();

					// FIXME: VV: Remove gradient features
					using (Cairo.Gradient pat = new Cairo.LinearGradient (rect.X, rect.Y, rect.X, bottom)) {
						pat.AddColorStop (0, bcolor.ToCairoColor ());
						Xwt.Drawing.Color gcol = bcolor.ToXwtColor ();
						gcol.Light -= 0.1;
						if (gcol.Light < 0)
							gcol.Light = 0;
						pat.AddColorStop (1, gcol.ToCairoColor ());
						cr.SetSource (pat);
						cr.Fill ();
					}
				} else {
					if (backgroundColorSet) {
						Gdk.GC gc = new Gdk.GC (GdkWindow);
						gc.RgbFgColor = bcolor;
						evnt.Window.DrawRectangle (gc, true, rect.X, rect.Y, rect.Width, rect.Height);
						gc.Dispose ();
					}
				}
			
			}
			base.OnExposeEvent (evnt);

			using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.SetSourceColor (BorderColor.ToCairoColor ());
				
				double y = rect.Y + topMargin / 2d;
				cr.LineWidth = topMargin;
				cr.Line (rect.X, y, right, y);
				cr.Stroke ();
				
				y = bottom - bottomMargin / 2d;
				cr.LineWidth = bottomMargin;
				cr.Line (rect.X, y, right, y);
				cr.Stroke ();
				
				double x = rect.X + leftMargin / 2d;
				cr.LineWidth = leftMargin;
				cr.Line (x, rect.Y, x, bottom);
				cr.Stroke ();
				
				x = right - rightMargin / 2d;
				cr.LineWidth = rightMargin;
				cr.Line (x, rect.Y, x, bottom);
				cr.Stroke ();

				return false;
			}
		}
	}
}
