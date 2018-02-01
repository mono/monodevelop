//
// ContextMenuExtensionsMac.cs
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
#if MAC
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide;
#endif

namespace MonoDevelop.Components
{
	#if MAC
	static class ContextMenuExtensionsMac
	{
		public static void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ContextMenu menu, Action closeHandler)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			var nsMenu = FromMenu (menu, closeHandler, null);
			ShowContextMenu (parent, evt, nsMenu);
		}

		public static void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, ContextMenu menu)
		{
			ShowContextMenu (parent, evt, menu, null);
		}

		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, ContextMenu menu, Action closeHandler, bool selectFirstItem = false)
		{
			var nsMenu = FromMenu (menu, closeHandler, null);
			ShowContextMenu (parent, x, y, nsMenu, selectFirstItem);
		}

		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, ContextMenu menu)
		{
			ShowContextMenu (parent, x, y, menu, null);
		}


		public static void ShowContextMenu (Gtk.Widget parent, int x, int y, NSMenu menu, bool selectFirstItem = false)
		{
			if (parent == null)
				throw new ArgumentNullException ("parent");
			if (menu == null)
				throw new ArgumentNullException ("menu");

			parent.GrabFocus ();

			Gtk.Application.Invoke ((o, args) => {
				// Explicitly release the grab because the menu is shown on the mouse position, and the widget doesn't get the mouse release event
				Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
				var nsview = MonoDevelop.Components.Mac.GtkMacInterop.GetNSView (parent);
				var toplevel = parent.Toplevel as Gtk.Window;

				var nswindow = MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow (toplevel);

				int titleBarOffset;
				if (toplevel.TypeHint == Gdk.WindowTypeHint.Toolbar && toplevel.Type == Gtk.WindowType.Toplevel && toplevel.Decorated == false) {
					// Undecorated toplevel toolbars are used for auto-hide pad windows. Don't add a titlebar offset for them.
					titleBarOffset = 0;
				} else if (MonoDevelop.Ide.DesktopService.GetIsFullscreen (toplevel)) {
					titleBarOffset = 0;
				} else {
					titleBarOffset = MonoDevelop.Components.Mac.GtkMacInterop.GetTitleBarHeight () + 12;
				}

				parent.TranslateCoordinates (parent.Toplevel, x, y, out x, out y);

				if (selectFirstItem) {
					var pt = new CoreGraphics.CGPoint (x, y);
					menu.PopUpMenu (menu.ItemAt (0), pt, nsview);
				} else {
					var pt = new CoreGraphics.CGPoint (x, nswindow.Frame.Height - y - titleBarOffset);
					var tmp_event = NSEvent.MouseEvent (NSEventType.LeftMouseDown,
					                                pt,
					                                0, 0,
					                                nswindow.WindowNumber,
					                                null, 0, 0, 0);
					NSMenu.PopUpContextMenu (menu, tmp_event, nsview);
				}
			});
		}

		public static void ShowContextMenu (Gtk.Widget parent, Gdk.EventButton evt, NSMenu menu)
		{
			int x = 0, y = 0;

			if (evt != null) {
				x = (int)evt.X;
				y = (int)evt.Y;
			}

			ShowContextMenu (parent, x, y, menu);
		}

		static void AddMenuItem (NSLocationAwareMenu menu, ContextMenuItem item)
		{
			if (item.IsSeparator) {
				menu.AddItem (NSMenuItem.SeparatorItem);
				return;
			}

			var menuItem = new NSContextMenuItem (item.Label.Replace ("_",""), item);

			menuItem.Hidden = !item.Visible;
			menuItem.Enabled = item.Sensitive;
			menuItem.Image = item.Image.ToNSImage ();

			if (item is RadioButtonContextMenuItem) {
				var radioItem = (RadioButtonContextMenuItem)item;
				menuItem.State = radioItem.Checked ? NSCellStateValue.On : NSCellStateValue.Off;
			} else if (item is CheckBoxContextMenuItem) {
				var checkItem = (CheckBoxContextMenuItem)item;
				menuItem.State = checkItem.Checked ? NSCellStateValue.On : NSCellStateValue.Off;
			} 

			if (item.SubMenu != null && item.SubMenu.Items.Count > 0) {
				var subMenu = FromMenu (item.SubMenu, null, menu);
				menuItem.Submenu = subMenu;
			}

			menu.AddItem (menuItem);
		}

		class NSContextMenuItem : NSMenuItem
		{
			readonly WeakReference<ContextMenuItem> contextMenu;

			public NSContextMenuItem (string label, ContextMenuItem item) : base (label)
			{
				contextMenu = new WeakReference<ContextMenuItem> (item);
				this.Activated += OnActivated;
			}

			static void OnActivated (object sender, EventArgs args)
			{
				var obj = (NSContextMenuItem)sender;

				if (obj.contextMenu.TryGetTarget (out var contextMenuItem))
					contextMenuItem.Click ();
			}
		}

		static NSLocationAwareMenu FromMenu (ContextMenu menu, Action closeHandler, NSLocationAwareMenu parent)
		{
			var result = new NSLocationAwareMenu (menu, closeHandler, parent) { AutoEnablesItems = false };

			foreach (var menuItem in menu.Items) {
				AddMenuItem (result, menuItem);
			}

			return result;
		}

		static readonly Xwt.Toolkit macToolkit = Xwt.Toolkit.Load (Xwt.ToolkitType.XamMac);

		public static NSImage ToNSImage (this Xwt.Drawing.Image image)
		{
			return (NSImage)macToolkit.GetNativeImage (image);
		}

		class NSLocationAwareMenu : NSMenu
		{
			public CGPoint Location { get; private set; }
			readonly WeakReference<NSLocationAwareMenu> Parent;

			public NSLocationAwareMenu (ContextMenu menu, Action closeHandler, NSLocationAwareMenu parent)
			{
				WeakDelegate = new ContextMenuDelegate (menu) { CloseHandler = closeHandler };
				Parent = parent != null ? new WeakReference<NSLocationAwareMenu> (parent) : null;
			}

			public override bool PopUpMenu (NSMenuItem item, CGPoint location, NSView view)
			{
				Location = location;
				var parentMenu = item?.ParentItem?.Menu as NSLocationAwareMenu;
				if (parentMenu != null) {
					Location = new CGPoint (
						Location.X + parentMenu.Location.X,
						Location.Y + parentMenu.Location.Y);
				}
				return base.PopUpMenu (item, location, view);
			}

			class ContextMenuDelegate : NSObject
			{
				ContextMenu menu;
				ContextMenuItem oldItem;
				public ContextMenuDelegate (ContextMenu menu)
				{
					this.menu = menu;
				}

				public Action CloseHandler { get; set; }

				[Export ("menuDidClose:")]
				void MenuDidClose (NSMenu menu)
				{
					if (menu.Supermenu != null)
						return;
					if (CloseHandler != null) {
						CloseHandler ();
					}
					this.menu.FireClosedEvent ();
				}

				[Export ("menu:willHighlightItem:")]
				void MenuWillHighlightItem (NSMenu menu, NSMenuItem willHighlightItem)
				{
					if (oldItem != null) {
						oldItem.FireDeselectedEvent ();
						oldItem = null;
					}
					if (willHighlightItem == null)
						return;
					int index = (int)menu.IndexOf (willHighlightItem);
					if (index < 0)
						return;
					oldItem = this.menu.Items [index];
					nfloat x = 0, y = 0;
					var locationAwareMenu = menu as NSLocationAwareMenu;
					if (locationAwareMenu != null) {
						while (locationAwareMenu.Parent != null && locationAwareMenu.Parent.TryGetTarget (out var other))
							locationAwareMenu = other;
						x = locationAwareMenu.Location.X;
						y = locationAwareMenu.Location.Y;
						menu = locationAwareMenu;
					}
					oldItem.FireSelectedEvent (new Xwt.Rectangle (x, y, menu.Size.Width, menu.Size.Height));
				}
			}
		}
	}
	#endif
}