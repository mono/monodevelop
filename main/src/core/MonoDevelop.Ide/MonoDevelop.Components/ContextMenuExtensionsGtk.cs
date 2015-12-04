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

		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, ContextMenu menu, Action closeHandler)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			var gtkMenu = FromMenu (menu, closeHandler);
			gtkMenu.ShowAll ();
			ShowContextMenu (parent, x, y, gtkMenu);
		}

		public static void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, Gtk.Menu menu)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			Mono.TextEditor.GtkWorkarounds.ShowContextMenu (menu, parent, evt);
		}

		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, Gtk.Menu menu)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			Mono.TextEditor.GtkWorkarounds.ShowContextMenu (menu, parent, x, y, parent.Allocation);
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
					GtkWorkarounds.ForceImageOnMenuItem (imageItem);
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

			if (closeHandler != null) {
				result.Hidden += (sender, e) => closeHandler ();
			}
			return result;
		}
	}
}