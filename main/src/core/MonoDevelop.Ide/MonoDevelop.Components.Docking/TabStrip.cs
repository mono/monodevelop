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
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.Docking
{
	class TabStrip : IDisposable
	{
		ITabStripControl control;

		public TabStrip (DockFrame frame)
		{
			control = new TabStripControl ();
			control.Initialize (this);
		}

		public void Dispose ()
		{
			control.Destroy ();
		}

		public ITabStripControl Control
		{
			get { return control; }
		}

		public int BottomPadding {
			get { return control.BottomPadding; }
			set { control.BottomPadding = value; }
		}

		public DockVisualStyle VisualStyle {
			get { return control.VisualStyle; }
			set { control.VisualStyle = value; }
		}

		public void AddTab (DockItemTitleTab tab)
		{
			tab.ParentTabStrip = this;
			control.AddTab (tab);
		}

		public void SetTabLabel (DockItemContainer container, Xwt.Drawing.Image icon, string label)
		{
			var tabControl = container.Control as IDockItemTitleTabControl;
			tabControl.SetLabel (container, icon, label);
			UpdateEllipsize (control.Size);
		}

		public void UpdateStyle (DockItem item)
		{
			control.UpdateStyle (item);
		}

		public int TabCount {
			get { return control.TabCount; }
		}

		public int CurrentTab {
			get { return control.CurrentTab; }
			set { control.CurrentTab = value; }
		}

		internal DockItemTitleTab CurrentTitleTab {
			get { return control.GetTitleTab (CurrentTab).ParentTab; }
		}

		new public Control CurrentPage {
			get {
				return CurrentTitleTab?.Page;
			}
			set {
				if (value != null) {
					for (int n = 0; n < control.TabCount; n++) {
						var tabControl = control.GetTitleTab (n);
						if (tabControl.ParentTab.Page == value) {
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
			control.CurrentTab = -1;
			control.ClearChildren ();
		}

		public void Show ()
		{
			control.Show ();
		}

		public void RemoveFromParent ()
		{
			control.RemoveFromParent ();
		}

		public Gdk.Rectangle Size {
			get {
				return control.Size;
			}
			set {
				control.Size = value;
			}
		}

		internal void QueueDraw ()
		{
			control.QueueDraw ();
		}

		internal void UpdateEllipsize (Gdk.Rectangle allocation)
		{
			int tabsSize = 0;
			int n = 0;

			if (control.TabCount == 0) {
				return;
			}

			control.ForeachTab (titleTab => {
				tabsSize += titleTab.LabelWidth;
			});
			var totalWidth = allocation.Width;

			int[] sizes = new int[control.TabCount];
			double ratio = (double) allocation.Width / (double) tabsSize;

			if (ratio > 1 && VisualStyle.ExpandedTabs.Value) {
				// The tabs have to fill all the available space. To get started, assume that all tabs with have the same size
				var tsize = totalWidth / control.TabCount;
				// Maybe the assigned size is too small for some tabs. If it happens the extra space it requires has to be taken
				// from tabs which have surplus of space. To calculate it, first get the difference beteen the assigned space
				// and the required space
				n = 0;
				control.ForeachTab (titleTab => {
					sizes[n++] = tsize - titleTab.LabelWidth;
				});

				// If all is positive, nothing is left to do (all tabs have enough space). If there is any negative, it means
				// that space has to be reassigned. The negative space has to be turned into positive by reducing space from other tabs
				for (n = 0; n < sizes.Length; n++) {
					if (sizes[n] < 0) {
						ReduceSizes (sizes, -sizes[n]);
						sizes[n] = 0;
					}
				}

				// Now calculate the final space assignment of each tab
				n = 0;
				control.ForeachTab (titleTab => {
					sizes[n] += titleTab.LabelWidth;
					totalWidth -= sizes[n++];
				});
			} else {
				if (ratio > 1)
					ratio = 1;

				n = 0;
				control.ForeachTab (titleTab => {
					var s = (int)((double)titleTab.LabelWidth * ratio);
					sizes[n++] = s;
					totalWidth -= s;
				});
			}

			// There may be some remaining space due to rounding. Spread it
			n = 0;
			control.ForeachTab (titleTab => {
				sizes[n++]++;
				totalWidth--;
			});

			// Assign the sizes
			control.SetTabSizes (sizes);
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

		internal int MinimumWidth {
			get {
				int minWidth = 0;
				control.ForeachTab (tab => {
					minWidth += tab.MinWidth;
				});

				return minWidth;
			}
		}
	}

	interface ITabStripControl
	{
		void Initialize (TabStrip parentTabStrip);
		int BottomPadding { get; set; }
		DockVisualStyle VisualStyle { get; set; }
		int TabCount { get; }
		int CurrentTab { get; set; }
		Gdk.Rectangle Size { get; set; } // The width, height
		Gdk.Rectangle Allocation { get; } // the origin and size

		void AddTab (DockItemTitleTab tab);
		void UpdateStyle (DockItem item);
		void ClearChildren ();
		void Show ();
		void RemoveFromParent ();
		void Destroy ();
		void QueueDraw ();
		IDockItemTitleTabControl GetTitleTab (int index);


		void ForeachTab (System.Action<IDockItemTitleTabControl> titleTab);
		void SetTabSizes (int[] sizes);
	}

	class TabStripControl : EventBox, ITabStripControl
	{
		TabStrip parentTabStrip;
		TabStripBox box;
		Label bottomFiller = new Label ();

		DockVisualStyle visualStyle;

		int currentTab = -1;

		public void Initialize (TabStrip parentTabStrip)
		{
			this.parentTabStrip = parentTabStrip;

			Accessible.SetRole (AtkCocoa.Roles.AXTabGroup);
			Accessible.SetCommonAttributes ("Docking.TabStrip",
			                                GettextCatalog.GetString ("Pad Tab Bar"),
			                                GettextCatalog.GetString ("The different pads in this dock position"));
			VBox vbox = new VBox ();
			vbox.Accessible.SetShouldIgnore (true);
			box = new TabStripBox () { TabStrip = parentTabStrip };
			box.Accessible.SetShouldIgnore (true);
			vbox.PackStart (box, false, false, 0);
			Add (vbox);
			ShowAll ();
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

		public void AddTab (DockItemTitleTab tab)
		{
			var tabWidget = tab.Control as Widget;
			if (tabWidget == null) {
				throw new ToolkitMismatchException ();
			}

			if (tabWidget.Parent != null)
				((Gtk.Container)tabWidget.Parent).Remove (tabWidget);

			box.PackStart (tabWidget, false, false, 0);

			if (currentTab == -1)
				CurrentTab = box.Children.Length - 1;
			else {
				tab.Active = false;
				tab.Page.GetNativeWidget<Widget> ().Hide ();
			}

			tab.Control.TabPressed += OnTabPress;
			tab.UpdateRole (true, parentTabStrip);
			UpdateAccessibilityTabs ();
		}

		void HandleRemoved (object o, RemovedArgs args)
		{
			var w = args.Widget as IDockItemTitleTabControl;

			if (w == null) {
				throw new ToolkitMismatchException ();
			}

			w.UpdateRole (false, parentTabStrip);

			w.TabPressed -= OnTabPress;
			if (currentTab >= box.Children.Length)
				currentTab = box.Children.Length - 1;

			UpdateAccessibilityTabs ();
		}

		void UpdateAccessibilityTabs ()
		{
			var tabs = new Atk.Object [box.Children.Length];
			int i = 0;

			foreach (var w in box.Children) {
				tabs [i] = w.Accessible;
				i++;
			}

			Accessible.SetTabs (tabs);
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
					var w = box.Children [currentTab];

					IDockItemTitleTabControl tabControl = w as IDockItemTitleTabControl;

					DockItemTitleTab tab = tabControl.ParentTab;
					tab.Page.GetNativeWidget<Widget> ().Hide ();
					tab.Active = false;
				}
				currentTab = value;
				if (currentTab != -1) {
					var w = box.Children [currentTab];

					IDockItemTitleTabControl tabControl = w as IDockItemTitleTabControl;
					if (tabControl == null) {
						throw new ToolkitMismatchException ();
					}

					DockItemTitleTab tab = tabControl.ParentTab;
					tab.Active = true;
					tab.Page.GetNativeWidget<Widget> ().Show ();
				}
			}
		}

		public IDockItemTitleTabControl GetTitleTab (int index)
		{
			return box.Children [index] as IDockItemTitleTabControl;
		}

		public Gdk.Rectangle Size {
			get {
				var req = SizeRequest ();
				return new Gdk.Rectangle (0, 0, req.Width, req.Height);
			}

			set {
				SizeAllocate (value);
			}
		}

		public void ClearChildren ()
		{
			foreach (var w in box.Children)
				box.Remove (w);
		}

		public void RemoveFromParent ()
		{
			if (Parent == null) {
				return;
			}

			((Container)Parent).Remove (this);
		}

		public void ForeachTab (Action<IDockItemTitleTabControl> closure)
		{
			foreach (var w in box.Children) {
				closure ((IDockItemTitleTabControl) w);
			}
		}

		public void SetTabSizes (int[] sizes)
		{
			int i = 0;
			foreach (var w in box.Children) {
				w.WidthRequest = sizes[i++];
			}
		}

		void OnTabPress (object s, EventArgs args)
		{
			CurrentTab = Array.IndexOf (box.Children, s);
			var t = s as IDockItemTitleTabControl;

			GtkUtil.SetFocus (t.Page);
			QueueDraw ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			parentTabStrip.UpdateEllipsize (allocation);
			base.OnSizeAllocated (allocation);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			requisition.Width = parentTabStrip.MinimumWidth;
		}

		internal class TabStripBox: HBox
		{
			public TabStrip TabStrip;
			static Xwt.Drawing.Image tabbarBackImage = Xwt.Drawing.Image.FromResource ("tabbar-back.9.png");

			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				if (TabStrip.VisualStyle.TabStyle == DockTabStyle.Normal) {
					using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
						ctx.DrawImage (this, tabbarBackImage.WithSize (Allocation.Width, Allocation.Height), 0, 0);
					}
				}	
				return base.OnExposeEvent (evnt);
			}
		}
	}
}


