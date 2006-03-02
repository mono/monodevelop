//
// WidgetTreeCombo.cs
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
using Gtk;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.GtkCore
{
	public class WidgetTreeCombo: Button
	{
		Label label;
		Image image;
		Stetic.Wrapper.Widget rootWidget;
		Stetic.Project project;
		Gdk.Pixbuf emptyImage;
		
		public WidgetTreeCombo ()
		{
			label = new Label ();
			image = new Gtk.Image ();
			
			HBox bb = new HBox ();
			bb.PackStart (image, false, false, 3);
			bb.PackStart (label, true, true, 3);
			label.Xalign = 0;
			bb.PackStart (new VSeparator (), false, false, 1);
			bb.PackStart (new Arrow (ArrowType.Down, ShadowType.None), false, false, 1);
			Child = bb;
			emptyImage = IdeApp.Services.Resources.GetIcon ("md-gtkcore-widget", Gtk.IconSize.LargeToolbar);
			this.WidthRequest = 300;
		}
		
		public Stetic.Wrapper.Widget RootWidget {
			get { return rootWidget; }
			set {
				rootWidget = value;
				project = (Stetic.Project) rootWidget.Project;
				project.Selected += new Stetic.Project.SelectedHandler (OnSelectionChanged);
				Update ();
			}
		}
		
		void OnSelectionChanged (Gtk.Widget w, Stetic.ProjectNode node)
		{
			Update ();
		}
		
		void Update ()
		{
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (project.Selection);
			if (project.Selection != null && wrapper != null) {
				label.Text = project.Selection.Name;
				image.Pixbuf = wrapper.ClassDescriptor.Icon;
				image.Show ();
			} else {
				label.Text = "             ";
				image.Pixbuf = emptyImage;
			}
		}
		
		protected override void OnPressed ()
		{
			base.OnPressed ();

			Gtk.Menu menu = new Gtk.Menu ();
			FillCombo (menu, RootWidget.Wrapped, 0);
			menu.ShowAll ();
			menu.Popup (null, null, new Gtk.MenuPositionFunc (OnPosition), 0, Gtk.Global.CurrentEventTime);
		}
		
		void OnPosition (Gtk.Menu menu, out int x, out int y, out bool pushIn)
		{
			ParentWindow.GetOrigin (out x, out y);
			x += Allocation.X;
			y += Allocation.Y + Allocation.Height;
			pushIn = true;
		}
		
		void FillCombo (Menu menu, Gtk.Widget widget, int level)
		{
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (widget);
			if (wrapper == null) return;
			
			MenuItem item = new WidgetMenuItem (widget);
			item.Activated += new EventHandler (OnItemSelected);
			
			HBox box = new HBox ();
			Gtk.Image img = new Gtk.Image (wrapper.ClassDescriptor.Icon);
			img.Xalign = 1;
			img.WidthRequest = level*30;
			box.PackStart (img, false, false, 0);
			
			Label lab = new Label ();
			if (widget == project.Selection)
				lab.Markup = "<b>" + widget.Name + "</b>";
			else
				lab.Text = widget.Name;
				
			box.PackStart (lab, false, false, 3);
			item.Child = box;
			menu.Append (item);
			
			Gtk.Container cc = widget as Gtk.Container;
			if (cc != null) {
				foreach (Gtk.Widget child in cc.Children)
					FillCombo (menu, child, level + 1);
			}
		}
		
		void OnItemSelected (object sender, EventArgs args)
		{
			WidgetMenuItem item = (WidgetMenuItem) sender;
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (item.Widget);
			wrapper.Select ();
		}
	}
	
	class WidgetMenuItem: MenuItem
	{
		internal Gtk.Widget Widget;
		
		public WidgetMenuItem (Gtk.Widget widget)
		{
			this.Widget = widget;
		}
	}
}
