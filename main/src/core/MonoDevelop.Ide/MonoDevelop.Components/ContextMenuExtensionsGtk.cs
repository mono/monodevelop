//
// ContextMenuExtensionsGtk.cs
//
// Author:
//       Greg Munn <greg.munn@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc
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
using Xwt.GtkBackend;
using MonoDevelop.Ide;

namespace MonoDevelop.Components
{
	static class ContextMenuExtensionsGtk
	{
		public static void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ContextMenu menu)
		{
			ShowContextMenu (parent, evt, menu, null);
		}

		public static void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ContextMenu menu, Action closeHandler)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			var gtkMenu = FromMenu (menu, closeHandler);
			gtkMenu.ShowAll ();
			ShowContextMenu (parent, evt, gtkMenu);
		}

		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, ContextMenu menu)
		{
			ShowContextMenu (parent, x, y, menu, null);
		}

		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, ContextMenu menu, Action closeHandler, bool selectFirstItem = false)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			var gtkMenu = FromMenu (menu, closeHandler);
			gtkMenu.ShowAll ();
			if (selectFirstItem && gtkMenu.Children.Length > 0) {
				gtkMenu.SelectItem (gtkMenu.Children [0]);
				new PoupHandlerWrapper (menu, gtkMenu);
			}
			ShowContextMenu (parent, x, y, gtkMenu);
		}

		// Gtk.Menu.Popup event seems not to be working correcty 
		// so doing a work around using the expose event.
		class PoupHandlerWrapper
		{
			ContextMenu menu;

			public PoupHandlerWrapper (ContextMenu menu, Gtk.Menu gtkMenu)
			{
				this.menu = menu;
				//gtkMenu.ExposeEvent += HandleExposeEvent;
			}

//			void HandleExposeEvent (object o, Gtk.ExposeEventArgs args)
//			{
//				var gtkMenu = (Gtk.Menu)o;
//				gtkMenu.ExposeEvent -= HandleExposeEvent;
//				int ox, oy;
//				gtkMenu.ParentWindow.GetOrigin (out ox, out oy);
//				int rx, ry;
//				IdeApp.Workbench.RootWindow.GdkWindow.GetOrigin (out rx, out ry);
//				menu.Items [0].FireSelectedEvent (new Xwt.Rectangle (ox - rx, oy - ry, gtkMenu.Allocation.Width, gtkMenu.Allocation.Height));
//			}
		}

		public static void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, Gtk.Menu menu)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			GtkWorkarounds.ShowContextMenu (menu, parent, evt);
		}

		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, Gtk.Menu menu)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			GtkWorkarounds.ShowContextMenu (menu, parent, x, y, parent.Allocation);
		}

		static Gtk.MenuItem CreateMenuItem (ContextMenuItem item)
		{
			if (!item.Visible)
				return null;
			
			if (item.IsSeparator) {
				return new Gtk.SeparatorMenuItem ();
			}

			Gtk.MenuItem menuItem;
			if (item is RadioButtonContextMenuItem) {
				var radioItem = (RadioButtonContextMenuItem)item;
				menuItem = new Gtk.CheckMenuItem (item.Label) { Active = radioItem.Checked, DrawAsRadio = true };
			} else if (item is CheckBoxContextMenuItem) {
				var checkItem = (CheckBoxContextMenuItem)item;
				menuItem = new Gtk.CheckMenuItem (item.Label) { Active = checkItem.Checked };
			} else {
				menuItem = new Gtk.ImageMenuItem (item.Label);
			} 
			menuItem.Selected += delegate (object sender, EventArgs e) {
				var si = sender as Gtk.MenuItem;
				if (si == null || si.GdkWindow == null)
					return;
				int x, y;
				si.GdkWindow.GetOrigin (out x, out y);
				int rx, ry;
				IdeApp.Workbench.RootWindow.GdkWindow.GetOrigin (out rx, out ry);

				item.FireSelectedEvent (new Xwt.Rectangle (x - rx, y - ry, si.Allocation.Width, si.Allocation.Height));
			};
			menuItem.Deselected += delegate {
				item.FireDeselectedEvent ();
			};
			if (item.SubMenu != null && item.SubMenu.Items.Count > 0) {
				menuItem.Submenu = FromMenu (item.SubMenu, null);
			}
			else {
				menuItem.Activated += (sender, e) => item.Click ();
			}

			menuItem.Sensitive = item.Sensitive;

			var label = (Gtk.Label) menuItem.Child;
			label.UseUnderline = item.UseMnemonic;
			if (item.UseMnemonic)
				label.TextWithMnemonic = item.Label;

			if (item.Image != null) {
				Gtk.ImageMenuItem imageItem = menuItem as Gtk.ImageMenuItem;
				if (imageItem != null) {
					var img = new ImageView (item.Image);
					img.ShowAll ();
					imageItem.Image = img;
					Xwt.GtkBackend.GtkWorkarounds.ForceImageOnMenuItem (imageItem);
				}
			}

			return menuItem;
		}

		static Gtk.Menu FromMenu (ContextMenu menu, Action closeHandler)
		{
			var result = new Gtk.Menu ();

			foreach (var menuItem in menu.Items) {
				var item = CreateMenuItem (menuItem);
				if (item != null)
					result.Append (item);
			}

			result.Hidden += delegate {
				if (closeHandler != null)
					closeHandler ();
				menu.FireClosedEvent ();
			};
			return result;
		}
	}
}
