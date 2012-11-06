//
// TabStrip.cs
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

using Gtk; 

using System;
using MonoDevelop.Ide.Gui;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.Docking
{
	class TabStrip: Gtk.EventBox
	{
		int currentTab = -1;
		HBox box = new HBox ();
		DockFrame frame;
		Label bottomFiller = new Label ();
		DockVisualStyle visualStyle;

		public TabStrip (DockFrame frame)
		{
			this.frame = frame;
			VBox vbox = new VBox ();
			box = new TabStripBox () { TabStrip = this };
			vbox.PackStart (box, false, false, 0);
		//	vbox.PackStart (bottomFiller, false, false, 0);
			Add (vbox);
			ShowAll ();
			bottomFiller.Hide ();
			BottomPadding = 3;
			WidthRequest = 0;
			box.Removed += HandleRemoved;
		}

		public int BottomPadding {
			get { return bottomFiller.HeightRequest; }
			set {
				bottomFiller.HeightRequest = value;
				bottomFiller.Visible = value > 0;
			}
		}

		public DockVisualStyle VisualStyle {
			get { return visualStyle; }
			set {
				visualStyle = value;
				box.QueueDraw ();
			}
		}
		
		public void AddTab (Tab tab)
		{
			if (tab.Parent != null)
				((Gtk.Container)tab.Parent).Remove (tab);

			//box.PackStart (tab, true, true, 0);
			box.PackStart (tab, false, false, 0);
			tab.WidthRequest = tab.LabelWidth;
			if (currentTab == -1)
				CurrentTab = box.Children.Length - 1;
			else {
				tab.Active = false;
				tab.Page.Hide ();
			}
			
			tab.ButtonPressEvent += OnTabPress;
		}

		void HandleRemoved (object o, RemovedArgs args)
		{
			Gtk.Widget w = args.Widget;
			w.ButtonPressEvent -= OnTabPress;
			if (currentTab >= box.Children.Length)
				currentTab = box.Children.Length - 1;
		}

		public void SetTabLabel (Gtk.Widget page, Gdk.Pixbuf icon, string label)
		{
			foreach (Tab tab in box.Children) {
				if (tab.Page == page) {
					tab.SetLabel (page, icon, label);
					UpdateEllipsize (Allocation);
					break;
				}
			}
		}
		
		public void UpdateStyle (DockItem item)
		{
			QueueResize ();
		}

		public int TabCount {
			get { return box.Children.Length; }
		}
		
		public int CurrentTab {
			get { return currentTab; }
			set {
				if (currentTab == value)
					return;
				if (currentTab != -1) {
					Tab t = (Tab) box.Children [currentTab];
					t.Page.Hide ();
					t.Active = false;
				}
				currentTab = value;
				if (currentTab != -1) {
					Tab t = (Tab) box.Children [currentTab];
					t.Active = true;
					t.Page.Show ();
				}
			}
		}
		
		new public Gtk.Widget CurrentPage {
			get {
				if (currentTab != -1) {
					Tab t = (Tab) box.Children [currentTab];
					return t.Page;
				} else
					return null;
			}
			set {
				if (value != null) {
					Gtk.Widget[] tabs = box.Children;
					for (int n = 0; n < tabs.Length; n++) {
						Tab tab = (Tab) tabs [n];
						if (tab.Page == value) {
							CurrentTab = n;
							return;
						}
					}
				}
				CurrentTab = -1;
			}
		}
		
		public void Clear ()
		{
			currentTab = -1;
			foreach (Tab w in box.Children)
				box.Remove (w);
		}
		
		void OnTabPress (object s, Gtk.ButtonPressEventArgs args)
		{
			CurrentTab = Array.IndexOf (box.Children, s);
			Tab t = (Tab) s;
			DockItem.SetFocus (t.Page);
			QueueDraw ();
			args.RetVal = true;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			UpdateEllipsize (allocation);
			base.OnSizeAllocated (allocation);
		}
		
		void UpdateEllipsize (Gdk.Rectangle allocation)
		{
			int tabsSize = 0;
			var children = box.Children;

			foreach (Tab tab in children)
				tabsSize += tab.LabelWidth;

			var totalWidth = allocation.Width;

			int[] sizes = new int[children.Length];
			double ratio = (double) allocation.Width / (double) tabsSize;

			if (ratio > 1 && visualStyle.ExpandedTabs.Value) {
				// The tabs have to fill all the available space. To get started, assume that all tabs with have the same size 
				var tsize = totalWidth / children.Length;
				// Maybe the assigned size is too small for some tabs. If it happens the extra space it requires has to be taken
				// from tabs which have surplus of space. To calculate it, first get the difference beteen the assigned space
				// and the required space.
				for (int n=0; n<children.Length; n++)
					sizes[n] = tsize - ((Tab)children[n]).LabelWidth;

				// If all is positive, nothing is left to do (all tabs have enough space). If there is any negative, it means
				// that space has to be reassigned. The negative space has to be turned into positive by reducing space from other tabs
				for (int n=0; n<sizes.Length; n++) {
					if (sizes[n] < 0) {
						ReduceSizes (sizes, -sizes[n]);
						sizes[n] = 0;
					}
				}
				// Now calculate the final space assignment of each tab
				for (int n=0; n<children.Length; n++) {
					sizes[n] += ((Tab)children[n]).LabelWidth;
					totalWidth -= sizes[n];
				}
			} else {
				if (ratio > 1)
					ratio = 1;
				for (int n=0; n<children.Length; n++) {
					var s = (int)((double)((Tab)children[n]).LabelWidth * ratio);
					sizes[n] = s;
					totalWidth -= s;
				}
			}

			// There may be some remaining space due to rounding. Spread it
			for (int n=0; n<children.Length && totalWidth > 0; n++) {
				sizes[n]++;
				totalWidth--;
			}
			// Assign the sizes
			for (int n=0; n<children.Length; n++)
				children[n].WidthRequest = sizes[n];
		}

		void ReduceSizes (int[] sizes, int amout)
		{
			// Homogeneously removes 'amount' pixels from the array of sizes, making sure
			// no size goes below 0.
			while (amout > 0) {
				int part;
				int candidates = sizes.Count (s => s > 0);
				if (candidates == 0)
					return;
				part = Math.Max (amout / candidates, 1);

				for (int n=0; n<sizes.Length && amout > 0; n++) {
					var s = sizes [n];
					if (s <= 0) continue;
					if (s > part) {
						s -= part;
						amout -= part;
					} else {
						amout -= s;
						s = 0;
					}
					sizes[n] = s;
				}
			}
		}

		internal class TabStripBox: HBox
		{
			public TabStrip TabStrip;

			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				if (TabStrip.VisualStyle.TabStyle == DockTabStyle.Normal) {
					var alloc = Allocation;
					Gdk.GC gc = new Gdk.GC (GdkWindow);
					gc.RgbFgColor = TabStrip.VisualStyle.InactivePadBackgroundColor.Value;
					evnt.Window.DrawRectangle (gc, true, alloc);
					gc.Dispose ();
		
					Gdk.GC bgc = new Gdk.GC (GdkWindow);
					var c = new HslColor (TabStrip.VisualStyle.PadBackgroundColor.Value);
					c.L *= 0.7;
					bgc.RgbFgColor = c;
					evnt.Window.DrawLine (bgc, alloc.X, alloc.Y + alloc.Height - 1, alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1);
					bgc.Dispose ();
				}	
				return base.OnExposeEvent (evnt);
			}
		}
		
	}
	
	class Tab: Gtk.EventBox
	{
		bool active;
		Gtk.Widget page;
		ExtendedLabel labelWidget;
		int labelWidth;
		DockVisualStyle visualStyle;
		Image tabIcon;
		DockFrame frame;
		string label;
		ImageButton btnDock;
		ImageButton btnClose;
		DockItem item;

		static Gdk.Pixbuf pixClose;
		static Gdk.Pixbuf pixAutoHide;
		static Gdk.Pixbuf pixDock;

		const int TopPadding = 5;
		const int BottomPadding = 7;
		const int TopPaddingActive = 5;
		const int BottomPaddingActive = 7;
		const int LeftPadding = 11;
		const int RightPadding = 9;

		static Tab ()
		{
			pixClose = Gdk.Pixbuf.LoadFromResource ("stock-close-12.png");
			pixAutoHide = Gdk.Pixbuf.LoadFromResource ("stock-auto-hide.png");
			pixDock = Gdk.Pixbuf.LoadFromResource ("stock-dock.png");
		}
		
		public Tab (DockItem item, DockFrame frame)
		{
			this.item = item;
			this.frame = frame;
			this.VisibleWindow = false;
			UpdateVisualStyle ();
			NoShowAll = true;
		}

		public DockVisualStyle VisualStyle {
			get { return visualStyle; }
			set {
				visualStyle = value;
				UpdateVisualStyle ();
				QueueDraw ();
			}
		}

		void UpdateVisualStyle ()
		{
			if (labelWidget != null && label != null) {
				if (visualStyle.UppercaseTitles.Value)
					labelWidget.Text = label.ToUpper ();
				else
					labelWidget.Text = label;
				labelWidget.UseMarkup = true;
				if (visualStyle.ExpandedTabs.Value)
					labelWidget.Xalign = 0.5f;

				if (!(Parent is TabStrip.TabStripBox))
					labelWidget.Xalign = 0;
			}

			if (tabIcon != null)
				tabIcon.Visible = visualStyle.ShowPadTitleIcon.Value;
			if (IsRealized) {
				if (labelWidget != null)
					labelWidget.ModifyFg (StateType.Normal, visualStyle.PadTitleLabelColor.Value);
			}
			var r = WidthRequest;
			WidthRequest = -1;
			labelWidth = SizeRequest ().Width + 1;
			WidthRequest = r;

			if (visualStyle != null)
				HeightRequest = visualStyle.PadTitleHeight != null ? visualStyle.PadTitleHeight.Value : -1;
		}

		public void SetLabel (Gtk.Widget page, Gdk.Pixbuf icon, string label)
		{
			this.label = label;
			this.page = page;
			if (Child != null) {
				Gtk.Widget oc = Child;
				Remove (oc);
				oc.Destroy ();
			}
			
			Gtk.HBox box = new HBox ();
			box.Spacing = 2;
			
			if (icon != null) {
				tabIcon = new Gtk.Image (icon);
				tabIcon.Show ();
				box.PackStart (tabIcon, false, false, 0);
			} else
				tabIcon = null;

			if (!string.IsNullOrEmpty (label)) {
				labelWidget = new ExtendedLabel (label);
				labelWidget.DropShadowVisible = true;
				labelWidget.UseMarkup = true;
				box.PackStart (labelWidget, true, true, 0);
			} else {
				labelWidget = null;
			}

			btnDock = new ImageButton ();
			btnDock.Image = pixAutoHide;
			btnDock.TooltipText = GettextCatalog.GetString ("Auto Hide");
			btnDock.CanFocus = false;
//			btnDock.WidthRequest = btnDock.HeightRequest = 17;
			btnDock.Clicked += OnClickDock;
			btnDock.WidthRequest = btnDock.SizeRequest ().Width;

			btnClose = new ImageButton ();
			btnClose.Image = pixClose;
			btnClose.TooltipText = GettextCatalog.GetString ("Close");
			btnClose.CanFocus = false;
//			btnClose.WidthRequest = btnClose.HeightRequest = 17;
			btnClose.WidthRequest = btnDock.SizeRequest ().Width;
			btnClose.Clicked += delegate {
				item.Visible = false;
			};

			Gtk.Alignment al = new Alignment (0, 0, 1, 1);
			HBox btnBox = new HBox (false, 3);
			btnBox.PackStart (btnDock, false, false, 0);
			btnBox.PackStart (btnClose, false, false, 0);
			al.Add (btnBox);
			al.LeftPadding = 3;
			al.TopPadding = 1;
			box.PackEnd (al, false, false, 0);

			Add (box);
			
			// Get the required size before setting the ellipsize property, since ellipsized labels
			// have a width request of 0
			box.ShowAll ();
			Show ();

			UpdateBehavior ();
			UpdateVisualStyle ();
		}
		
		void OnClickDock (object s, EventArgs a)
		{
			if (item.Status == DockItemStatus.AutoHide || item.Status == DockItemStatus.Floating)
				item.Status = DockItemStatus.Dockable;
			else
				item.Status = DockItemStatus.AutoHide;
		}

		public int LabelWidth {
			get { return labelWidth; }
		}
		
		public bool Active {
			get {
				return active;
			}
			set {
				if (active != value) {
					active = value;
					this.QueueResize ();
					QueueDraw ();
					UpdateBehavior ();
				}
			}
		}

		public Widget Page {
			get {
				return page;
			}
		}
		
		public void UpdateBehavior ()
		{
			if (btnClose == null)
				return;

			btnClose.Visible = (item.Behavior & DockItemBehavior.CantClose) == 0;
			btnDock.Visible = (item.Behavior & DockItemBehavior.CantAutoHide) == 0;
			
			if (active) {
				if (btnClose.Image == null)
					btnClose.Image = pixClose;
				if (item.Status == DockItemStatus.AutoHide || item.Status == DockItemStatus.Floating) {
					btnDock.Image = pixDock;
					btnDock.TooltipText = GettextCatalog.GetString ("Dock");
				} else {
					btnDock.Image = pixAutoHide;
					btnDock.TooltipText = GettextCatalog.GetString ("Auto Hide");
				}
			} else {
				btnDock.Image = null;
				btnClose.Image = null;
			}
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			UpdateVisualStyle ();
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			if (Child != null) {
				req = Child.SizeRequest ();
				req.Width += LeftPadding + RightPadding;
				if (active)
					req.Height += TopPaddingActive + BottomPaddingActive;
				else
					req.Height += TopPadding + BottomPadding;
			}
		}
					
		protected override void OnSizeAllocated (Gdk.Rectangle rect)
		{
			base.OnSizeAllocated (rect);

			int leftPadding = LeftPadding;
			int rightPadding = RightPadding;
			if (rect.Width < labelWidth) {
				int red = (labelWidth - rect.Width) / 2;
				leftPadding -= red;
				rightPadding -= red;
				if (leftPadding < 2) leftPadding = 2;
				if (rightPadding < 2) rightPadding = 2;
			}
			
			rect.X += leftPadding;
			rect.Width -= leftPadding + rightPadding;

			if (Child != null) {
				if (active) {
					rect.Y += TopPaddingActive;
					rect.Height = Child.SizeRequest ().Height;
				}
				else {
					rect.Y += TopPadding;
					rect.Height = Child.SizeRequest ().Height;
				}
				Child.SizeAllocate (rect);
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (VisualStyle.TabStyle == DockTabStyle.Normal)
				DrawAsBrowser (evnt);
			else
				DrawNormal (evnt);
			return base.OnExposeEvent (evnt);
		}

		void DrawAsBrowser (Gdk.EventExpose evnt)
		{
			var alloc = Allocation;

			Gdk.GC bgc = new Gdk.GC (GdkWindow);
			var c = new HslColor (VisualStyle.PadBackgroundColor.Value);
			c.L *= 0.7;
			bgc.RgbFgColor = c;
			bool first = true;
			bool last = true;
			TabStrip tabStrip = null;
			if (Parent is TabStrip.TabStripBox) {
				var tsb = (TabStrip.TabStripBox) Parent;
				var cts = tsb.Children;
				first = cts[0] == this;
				last = cts[cts.Length - 1] == this;
				tabStrip = tsb.TabStrip;
			}

			if (Active || (first && last)) {
				Gdk.GC gc = new Gdk.GC (GdkWindow);
				gc.RgbFgColor = VisualStyle.PadBackgroundColor.Value;
				evnt.Window.DrawRectangle (gc, true, alloc);
				if (!first)
					evnt.Window.DrawLine (bgc, alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height - 1);
				if (!(last && first) && !(tabStrip != null && tabStrip.VisualStyle.ExpandedTabs.Value && last))
					evnt.Window.DrawLine (bgc, alloc.X + alloc.Width - 1, alloc.Y, alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1);
				gc.Dispose ();

			} else {
				Gdk.GC gc = new Gdk.GC (GdkWindow);
				gc.RgbFgColor = tabStrip != null ? tabStrip.VisualStyle.InactivePadBackgroundColor.Value : frame.DefaultVisualStyle.InactivePadBackgroundColor.Value;
				evnt.Window.DrawRectangle (gc, true, alloc);
				gc.Dispose ();
				evnt.Window.DrawLine (bgc, alloc.X, alloc.Y + alloc.Height - 1, alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1);
			}
			bgc.Dispose ();
		}

		void DrawNormal (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				var x = Allocation.X;
				var y = Allocation.Y;

				ctx.Rectangle (x, y + 1, Allocation.Width, Allocation.Height - 1);
				using (var g = new Cairo.LinearGradient (x, y + 1, x, y + Allocation.Height - 1)) {
					g.AddColorStop (0, Styles.DockTabBarGradientStart);
					g.AddColorStop (1, Styles.DockTabBarGradientEnd);
					ctx.Pattern = g;
					ctx.Fill ();
				}

				ctx.MoveTo (x + 0.5, y + 0.5);
				ctx.LineTo (x + Allocation.Width - 0.5d, y + 0.5);
				ctx.Color = Styles.DockTabBarGradientTop;
				ctx.Stroke ();

				if (active) {

					ctx.Rectangle (x, y + 1, Allocation.Width, Allocation.Height - 1);
					using (var g = new Cairo.LinearGradient (x, y + 1, x, y + Allocation.Height - 1)) {
						g.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.01));
						g.AddColorStop (0.5, new Cairo.Color (0, 0, 0, 0.08));
						g.AddColorStop (1, new Cairo.Color (0, 0, 0, 0.01));
						ctx.Pattern = g;
						ctx.Fill ();
					}

/*					double offset = Allocation.Height * 0.25;
					var rect = new Cairo.Rectangle (x - Allocation.Height + offset, y, Allocation.Height, Allocation.Height);
					var cg = new Cairo.RadialGradient (rect.X + rect.Width / 2, rect.Y + rect.Height / 2, 0, rect.X, rect.Y + rect.Height / 2, rect.Height / 2);
					cg.AddColorStop (0, Styles.DockTabBarShadowGradientStart);
					cg.AddColorStop (1, Styles.DockTabBarShadowGradientEnd);
					ctx.Pattern = cg;
					ctx.Rectangle (rect);
					ctx.Fill ();

					rect = new Cairo.Rectangle (x + Allocation.Width - offset, y, Allocation.Height, Allocation.Height);
					cg = new Cairo.RadialGradient (rect.X + rect.Width / 2, rect.Y + rect.Height / 2, 0, rect.X, rect.Y + rect.Height / 2, rect.Height / 2);
					cg.AddColorStop (0, Styles.DockTabBarShadowGradientStart);
					cg.AddColorStop (1, Styles.DockTabBarShadowGradientEnd);
					ctx.Pattern = cg;
					ctx.Rectangle (rect);
					ctx.Fill ();*/
				}
			}
		}
	}
}


