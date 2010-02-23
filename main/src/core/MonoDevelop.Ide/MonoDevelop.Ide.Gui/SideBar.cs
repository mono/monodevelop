// 
// SideBar.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using Gtk;
using Mono.Addins;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Components.Docking;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui
{
	class SideBar: VBox
	{
		List<SideBarTab> tabs = new List<SideBarTab> ();
		Box mainBox;
		Box boxTabs;
		SideBarTab currentActive;
		DefaultWorkbench workbench;
		Gtk.Orientation orientation;
		
		static List<SideBar> bars = new List<SideBar> ();
		
		public static void UpdateStatus ()
		{
			foreach (SideBar bar in bars)
				bar.UpdateBarStatus ();
		}
		
		public SideBar (DefaultWorkbench workbench, Gtk.Orientation orientation)
		{
			bars.Add (this);
			this.orientation = orientation;
			if (orientation == Orientation.Vertical) {
				mainBox = this;
				boxTabs = new VBox (false, 0);
			} else {
				mainBox = new HBox ();
				PackStart (mainBox);
				boxTabs = new HBox (false, 0);
			}
			this.workbench = workbench;
			mainBox.PackStart (boxTabs, true, true, 0);
			AddinManager.ExtensionChanged += HandleAddinManagerExtensionChanged;
			ShowAll ();
			IdeApp.Workbench.LayoutChanged += HandleIdeAppWorkbenchLayoutChanged;
			UpdateTabs ();
		}
		
		protected override void OnDestroyed ()
		{
			bars.Remove (this);
			base.OnDestroyed ();
		}

		
		void UpdateBarStatus ()
		{
			foreach (PadCodon pad in workbench.PadContentCollection) {
				IPadWindow w = workbench.WorkbenchLayout.GetPadWindow (pad);
				if (w != null) {
					DockItem item = workbench.WorkbenchLayout.GetDockItem (pad);
					if (item != null) {
						foreach (SideBarTab tab in tabs) {
							string layout = workbench.Context.Id + "." + tab.Label;
							tab.Running = w.IsWorking && item.VisibleInLayout (layout);
						}
					}
				}
			}
		}

		void HandleAddinManagerExtensionChanged (object sender, ExtensionEventArgs args)
		{
			if (args.Path == "/MonoDevelop/Ide/SideBarTabs")
				UpdateTabs ();
		}

		void UpdateTabs ()
		{
/*			foreach (SideBarTabNode node in AddinManager.GetExtensionNodes ("MonoDevelop/Ide/SideBarTabs")) {
				SideBarTab tab = new SideBarTab (node.Label, node.Icon, orientation);
				tab.ShowAll ();
				boxTabs.PackStart (tab, false, false, 0);
				tab.ButtonPressEvent += delegate {
					if (currentActive != null)
						currentActive.Active = false;
					tab.Active = true;
					currentActive = tab;
					Widget w = (Widget) node.GetInstance (typeof(Widget));
					IdeApp.Workbench.ShowCustomLayout (w);
				};
			}*/
			UpdateLayoutTabs ();
		}

		void HandleIdeAppWorkbenchLayoutChanged (object sender, EventArgs e)
		{
			UpdateLayoutTabs ();
		}
		
		void UpdateLayoutTabs ()
		{
			foreach (SideBarTab tab in tabs) {
				boxTabs.Remove (tab);
				tab.Destroy ();
			}
			tabs.Clear ();
			
			for (int i = 0; i < IdeApp.Workbench.Layouts.Length; i++)
			{
				string layout = IdeApp.Workbench.Layouts[i];
				SideBarTab tab = new SideBarTab (layout, null, orientation);
				tab.ShowAll ();
				boxTabs.PackStart (tab, false, false, 0);
				tabs.Add (tab);
				tab.Active = IdeApp.Workbench.CurrentLayout == layout;
					
				tab.ButtonPressEvent += delegate(object o, ButtonPressEventArgs args) {
					if (currentActive != null)
						currentActive.Active = false;
					tab.Active = true;
					currentActive = tab;
					IdeApp.Workbench.CurrentLayout = layout;
				};
			}
			UpdateStatus ();
		}
	}
	
	class SideBarButton: Button
	{
	}
	
	class SideBarTab: EventBox
	{
		static bool isWindows = (System.IO.Path.DirectorySeparatorChar == '\\');
		Box box;
		
		Gtk.Orientation orientation;
		string label;
		string icon;
		bool isActive;
		bool hilight;
		bool running;
		Gtk.Label slabel;
		
		public Widget CustomLayout { get; set; }
		
		public bool Active {
			get {
				return isActive;
			}
			set {
				if (isActive == value)
					return;
				isActive = value;
				UpdateColor ();
/*				if (Parent != null) {
					HslColor c = new HslColor (normalColor);
					c.L += 0.2;
					if (c.L > 1) c.L = 1;
					if (isActive)
						ModifyBg (StateType.Normal, c);
					else
						ModifyBg (StateType.Normal, normalColor);
				}*/
			}
		}
		
		void UpdateColor ()
		{
			if (isActive)
				slabel.ModifyFg (StateType.Normal, normalColor);
			else {
				HslColor c = normalColor;
				c.L += 0.3;
				slabel.ModifyFg (StateType.Normal, c);
			}
		}
		
		public bool Running {
			get {
				return running;
			}
			set {
				if (running != value) {
					running = value;
					UpdateTab ();
				}
			}
		}
		
		public string Label {
			get {
				return label;
			}
		}
		
		public SideBarTab (string label, string icon, Gtk.Orientation orientation)
		{
			Events = Events | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
			this.orientation = orientation;
			
			if (string.IsNullOrEmpty (icon)) {
//				if (label == "Debug") icon = "md-execute-debug";
//				if (label == "Default") icon = "md-solution";
			}
			this.label = label;
			this.icon = icon;
			VisibleWindow = false;
			UpdateTab ();
		}
		
		const int ItemPadding = 5;
		const int BarPadding = 4;
		
		public void UpdateTab ()
		{
			if (box != null) {
				Remove (box);
				box.Destroy ();
				slabel = null;
			}
			
			Gtk.Alignment align = new Gtk.Alignment (0,0,0,0);
			
			if (orientation == Gtk.Orientation.Horizontal) {
				box = new HBox ();
				align.LeftPadding = ItemPadding;
				align.RightPadding = ItemPadding;
				align.TopPadding = BarPadding;
				align.BottomPadding = BarPadding;
			}
			else {
				box = new VBox ();
				align.LeftPadding = BarPadding;
				align.RightPadding = BarPadding;
				align.TopPadding = ItemPadding;
				align.BottomPadding = ItemPadding;
			}
			align.Add (box);
			
			if (!string.IsNullOrEmpty (icon))
				box.PackStart (new Gtk.Image (icon, IconSize.Menu), false, false, 0);
				
			if (!string.IsNullOrEmpty (label)) {
				string txt = label;
//				string txt = "<b>" + label + "</b>";
				if (running)
					txt = "<span foreground='blue'>" + txt + "</span>";
				slabel = new Gtk.Label (txt);
				UpdateColor ();
				slabel.UseMarkup = true;
				if (orientation == Gtk.Orientation.Vertical)
					slabel.Angle = 270;
				box.PackStart (slabel, true, true, 0);
			}
			box.Spacing = 2;
			align.ShowAll ();
			
			Add (align);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (isActive || hilight) {
				double x = Allocation.Left, y = Allocation.Top;
				int w = Allocation.Width, h = Allocation.Height;
				using (Cairo.Context ctx = Gdk.CairoHelper.Create (GdkWindow)) {
					HslColor c1 = new HslColor (Style.Background (Gtk.StateType.Normal));
					HslColor c2 = c1;
					if (isActive)
						c2.L *= 0.8;
					else
						c2.L *= 0.9;
					Cairo.Gradient pat;
					pat = new Cairo.LinearGradient (x, y, x+w-2, y);
					pat.AddColorStop (0, c2);
					pat.AddColorStop (1, c1);
					ctx.Pattern = pat;
					ctx.NewPath ();
					ctx.Rectangle (x, y, w, h);
					ctx.Fill ();
				}
			}
			
//			Gtk.Style.PaintBox (Style, GdkWindow, StateType.Normal, ShadowType.Out, evnt.Area, this, isWindows? "button" : "",
//			                    Allocation.Left, Allocation.Top, Allocation.Width, Allocation.Height);
			
			bool res = base.OnExposeEvent (evnt);
			return res;
		}
		
		Gdk.Color normalColor;
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			normalColor = Style.Foreground (Gtk.StateType.Normal);
			UpdateColor ();
		}

		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			hilight = true;
			QueueDraw ();
			return base.OnEnterNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			hilight = false;
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}
	}
}
