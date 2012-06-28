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

namespace MonoDevelop.Components
{
	class SquaredNotebook: VBox
	{
		SquaredTabStrip tabStrip;
		HBox tabBox;

		public SquaredNotebook ()
		{
			tabStrip = new SquaredTabStrip ();
			PackStart (tabStrip, false, false, 0);

			tabBox = new HBox ();
			PackStart (tabBox, true, true, 0);

			ShowAll ();
		}

		public void AddTab (Gtk.Widget page, string label)
		{
			AddTab (page, label, null);
		}
		
		public void AddTab (Gtk.Widget page, string label, Gdk.Pixbuf icon)
		{
			page.Visible = tabBox.Children.Length == 0;
			tabStrip.AddTab (page, icon, label);
			tabBox.PackStart (page, true, true, 0);
		}

		class SquaredTabStrip: Gtk.EventBox
		{
			int currentTab = -1;
			bool ellipsized = true;
			HBox box = new HBox ();
			Label bottomFiller = new Label ();

			public SquaredTabStrip ()
			{
				VBox vbox = new VBox ();
				box = new TabStripBox () { TabStrip = this };
				vbox.PackStart (box, false, false, 0);
			//	vbox.PackStart (bottomFiller, false, false, 0);
				Add (vbox);
				ShowAll ();
				bottomFiller.Hide ();
				BottomPadding = 3;
			}
			
			public int BottomPadding {
				get { return bottomFiller.HeightRequest; }
				set {
					bottomFiller.HeightRequest = value;
					bottomFiller.Visible = value > 0;
				}
			}

			public SquaredTab AddTab (Gtk.Widget page, Gdk.Pixbuf icon, string label)
			{
				SquaredTab tab = new SquaredTab ();
				tab.SetLabel (page, icon, label);
				AddTab (tab);
				return tab;
			}

			public void AddTab (SquaredTab tab)
			{
				if (tab.Parent != null)
					((Gtk.Container)tab.Parent).Remove (tab);

				box.PackStart (tab, true, true, 0);
				if (currentTab == -1)
					CurrentTab = box.Children.Length - 1;
				else {
					tab.Active = false;
					tab.Page.Hide ();
				}
				
				tab.ButtonPressEvent += OnTabPress;
			}
			
			public void SetTabLabel (Gtk.Widget page, Gdk.Pixbuf icon, string label)
			{
				foreach (SquaredTab tab in box.Children) {
					if (tab.Page == page) {
						tab.SetLabel (page, icon, label);
						UpdateEllipsize (Allocation);
						break;
					}
				}
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
						SquaredTab t = (SquaredTab) box.Children [currentTab];
						t.Page.Hide ();
						t.Active = false;
					}
					currentTab = value;
					if (currentTab != -1) {
						SquaredTab t = (SquaredTab) box.Children [currentTab];
						t.Active = true;
						t.Page.Show ();
					}
				}
			}
			
			new public Gtk.Widget CurrentPage {
				get {
					if (currentTab != -1) {
						SquaredTab t = (SquaredTab) box.Children [currentTab];
						return t.Page;
					} else
						return null;
				}
				set {
					if (value != null) {
						Gtk.Widget[] tabs = box.Children;
						for (int n = 0; n < tabs.Length; n++) {
							SquaredTab tab = (SquaredTab) tabs [n];
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
				ellipsized = true;
				currentTab = -1;
				foreach (Widget w in box.Children)
					box.Remove (w);
			}
			
			void OnTabPress (object s, Gtk.ButtonPressEventArgs args)
			{
				CurrentTab = Array.IndexOf (box.Children, s);
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
				int tsize = 0;
				foreach (SquaredTab tab in box.Children)
					tsize += tab.LabelWidth;

				bool ellipsize = tsize > allocation.Width;
				if (ellipsize != ellipsized) {
					foreach (SquaredTab tab in box.Children) {
						tab.SetEllipsize (ellipsize);
						Gtk.Box.BoxChild bc = (Gtk.Box.BoxChild) box [tab];
						bc.Expand = bc.Fill = ellipsize;
					}
					ellipsized = ellipsize;
				}
			}

			internal class TabStripBox: HBox
			{
				public SquaredTabStrip TabStrip;

				protected override bool OnExposeEvent (Gdk.EventExpose evnt)
				{
					var alloc = Allocation;
					var baseColor = Style.Background (StateType.Normal);
					var c = new HslColor (baseColor);
					c.L *= 0.9;
					Gdk.GC gc = new Gdk.GC (GdkWindow);
					gc.RgbFgColor = c;
					evnt.Window.DrawRectangle (gc, true, alloc);
					gc.Dispose ();
		
					Gdk.GC bgc = new Gdk.GC (GdkWindow);
					c = new HslColor (baseColor);
					c.L *= 0.7;
					bgc.RgbFgColor = c;
					evnt.Window.DrawLine (bgc, alloc.X, alloc.Y + alloc.Height - 1, alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1);
					bgc.Dispose ();
					return base.OnExposeEvent (evnt);
				}
			}
		}
		
		class SquaredTab: Gtk.EventBox
		{
			bool active;
			Gtk.Widget page;
			Gtk.Label labelWidget;
			int labelWidth;
			Image tabIcon;

			const int TopPadding = 7;
			const int BottomPadding = 7;
			const int TopPaddingActive = 7;
			const int BottomPaddingActive = 7;
			const int HorzPadding = 11;
			
			public SquaredTab ()
			{
				this.VisibleWindow = false;
				UpdateVisualStyle ();
			}

			void UpdateVisualStyle ()
			{
			//	if (tabIcon != null)
			//		tabIcon.Visible = visualStyle != DockStyle.Browser;
			}

			public void SetLabel (Gtk.Widget page, Gdk.Pixbuf icon, string label)
			{
				Pango.EllipsizeMode oldMode = Pango.EllipsizeMode.End;
				
				this.page = page;
				if (Child != null) {
					if (labelWidget != null)
						oldMode = labelWidget.Ellipsize;
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
					labelWidget = new Gtk.Label (label);
					labelWidget.UseMarkup = true;
					labelWidget.Xalign = 0;
					box.PackStart (labelWidget, true, true, 0);
				} else {
					labelWidget = null;
				}
				
				Add (box);
				
				// Get the required size before setting the ellipsize property, since ellipsized labels
				// have a width request of 0
				ShowAll ();
				labelWidth = SizeRequest ().Width;
				
				if (labelWidget != null)
					labelWidget.Ellipsize = oldMode;
				UpdateVisualStyle ();
			}
			
			public void SetEllipsize (bool elipsize)
			{
				if (labelWidget != null) {
					if (elipsize)
						labelWidget.Ellipsize = Pango.EllipsizeMode.End;
					else
						labelWidget.Ellipsize = Pango.EllipsizeMode.None;
				}
			}
			
			public int LabelWidth {
				get { return labelWidth; }
			}
			
			public bool Active {
				get {
					return active;
				}
				set {
					active = value;
					this.QueueResize ();
					QueueDraw ();
				}
			}

			public Widget Page {
				get {
					return page;
				}
			}
			
			protected override void OnSizeRequested (ref Gtk.Requisition req)
			{
				req = Child.SizeRequest ();
				req.Width += HorzPadding * 2;
				if (active)
					req.Height += TopPaddingActive + BottomPaddingActive;
				else
					req.Height += TopPadding + BottomPadding;
			}
						
			protected override void OnSizeAllocated (Gdk.Rectangle rect)
			{
				base.OnSizeAllocated (rect);
				
				rect.X += HorzPadding;
				rect.Width -= HorzPadding * 2;
				
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

			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				DrawAsBrowser (evnt);
				return base.OnExposeEvent (evnt);
			}

			void DrawAsBrowser (Gdk.EventExpose evnt)
			{
				var alloc = Allocation;

				Gdk.GC bgc = new Gdk.GC (GdkWindow);
				var baseColor = Style.Background (StateType.Normal);
				var c = new HslColor (baseColor);
				c.L *= 0.7;
				bgc.RgbFgColor = c;

				var cts = ((SquaredTabStrip.TabStripBox)Parent).Children;
				var first = cts[0] == this;
				var last = cts[cts.Length - 1] == this;

				if (Active || (first && last)) {
					Gdk.GC gc = new Gdk.GC (GdkWindow);
					gc.RgbFgColor = baseColor;
					evnt.Window.DrawRectangle (gc, true, alloc);
					if (!first)
						evnt.Window.DrawLine (bgc, alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height - 1);
					if (!last || !first)
						evnt.Window.DrawLine (bgc, alloc.X + alloc.Width - 1, alloc.Y, alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1);
					gc.Dispose ();

				} else {
					c = new HslColor (baseColor);
					c.L *= 0.9;
					Gdk.GC gc = new Gdk.GC (GdkWindow);
					gc.RgbFgColor = c;
					evnt.Window.DrawRectangle (gc, true, alloc);
					gc.Dispose ();
					evnt.Window.DrawLine (bgc, alloc.X, alloc.Y + alloc.Height - 1, alloc.X + alloc.Width - 1, alloc.Y + alloc.Height - 1);
				}
				bgc.Dispose ();
			}
		}
	}
}
