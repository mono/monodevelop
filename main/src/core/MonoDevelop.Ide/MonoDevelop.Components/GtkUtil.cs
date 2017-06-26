// GtkUtil.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Linq;
using System.Collections.Generic;
using Gtk;
using System.Runtime.InteropServices;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Components
{
	public static class GtkUtil
	{
		static Dictionary<TreeView, TreeViewTooltipsData> treeData = new Dictionary<TreeView, TreeViewTooltipsData> ();

		static Xwt.Toolkit gtkToolkit;

		internal static Xwt.Toolkit GtkToolkit {
			get {
				if (gtkToolkit == null)
					gtkToolkit = Xwt.Toolkit.LoadedToolkits.FirstOrDefault (t => t.Type == Xwt.ToolkitType.Gtk);
				return gtkToolkit;
			}
		}

		public static Cairo.Color ToCairoColor (this Gdk.Color color)
		{
			return new Cairo.Color ((double)color.Red / ushort.MaxValue,
			                        (double)color.Green / ushort.MaxValue,
			                        (double)color.Blue / ushort.MaxValue);
		}
		
		public static Xwt.Drawing.Color ToXwtColor (this Gdk.Color color)
		{
			return new Xwt.Drawing.Color ((double)color.Red / ushort.MaxValue,
				(double)color.Green / ushort.MaxValue,
				(double)color.Blue / ushort.MaxValue);
		}

		public static string GetHex (this Gdk.Color color)
		{
			return String.Format("#{0:x2}{1:x2}{2:x2}",
			                     (byte)(((double)color.Red / ushort.MaxValue) * 255),
			                     (byte)(((double)color.Green / ushort.MaxValue) * 255),
			                     (byte)(((double)color.Blue / ushort.MaxValue) * 255));
		}

		public static Gdk.Color ToGdkColor (this Cairo.Color color)
		{
			return new Gdk.Color ((byte)(color.R * 255d), (byte)(color.G * 255d), (byte)(color.B * 255d));
		}
		
		public static Gdk.Color ToGdkColor (this Xwt.Drawing.Color color)
		{
			return new Gdk.Color ((byte)(color.Red * 255d), (byte)(color.Green * 255d), (byte)(color.Blue * 255d));
		}

		public static Cairo.Color ToCairoColor (this Xwt.Drawing.Color color)
		{
			return new Cairo.Color (color.Red, color.Green, color.Blue, color.Alpha);
		}

		public static Xwt.Drawing.Color ToXwtColor (this Cairo.Color color)
		{
			return new Xwt.Drawing.Color (color.R, color.G, color.B, color.A);
		}

		/// <summary>
		/// Makes a color lighter or darker
		/// </summary>
		/// <param name='lightAmount'>
		/// Amount of lightness to add. If the value is positive, the color will be lighter,
		/// if negative it will be darker. Value must be between 0 and 1.
		/// </param>
		public static Gdk.Color AddLight (this Gdk.Color color, double lightAmount)
		{
			var c = color.ToXwtColor ();
			c.Light += lightAmount;
			return c.ToGdkColor ();
		}

		/// <summary>
		/// Makes a color lighter or darker
		/// </summary>
		/// <param name='lightAmount'>
		/// Amount of lightness to add. If the value is positive, the color will be lighter,
		/// if negative it will be darker. Value must be between 0 and 1.
		/// </param>
		public static HslColor AddLight (this HslColor color, double lightAmount)
		{
			color.L += lightAmount;
			return color;
		}

		public static Cairo.Color AddLight (this Cairo.Color color, double lightAmount)
		{
			var c = color.ToXwtColor ();
			c.Light += lightAmount;
			return c.ToCairoColor ();
		}

		/// <summary>
		/// Makes a color lighter or darker
		/// </summary>
		/// <param name='lightAmount'>
		/// Amount of lightness to add. If the value is positive, the color will be lighter,
		/// if negative it will be darker. Value must be between 0 and 1.
		/// </param>
		public static Xwt.Drawing.Color AddLight (this Xwt.Drawing.Color color, double lightAmount)
		{
			color.Light += lightAmount;
			return color;
		}

		public static Xwt.Drawing.Context CreateXwtContext (this Gtk.Widget w)
		{
			var c = Gdk.CairoHelper.Create (w.GdkWindow);
			return GtkToolkit.WrapContext (w, c);
		}

		public static Gtk.Widget ToGtkWidget (this Xwt.Widget widget)
		{
			return (Gtk.Widget) GtkToolkit.GetNativeWidget (widget);
		}

		public static void DrawImage (this Cairo.Context s, Gtk.Widget widget, Xwt.Drawing.Image image, double x, double y)
		{
			GtkToolkit.RenderImage (widget, s, image, x, y);
		}

		public static Xwt.Drawing.Image ToXwtImage (this Gdk.Pixbuf pix)
		{
			return GtkToolkit.WrapImage (pix);
		}

		public static Gdk.Pixbuf ToPixbuf (this Xwt.Drawing.Image image)
		{
			return (Gdk.Pixbuf)GtkToolkit.GetNativeImage (image);
		}

		public static Gdk.Pixbuf ToPixbuf (this Xwt.Drawing.Image image, Gtk.IconSize size)
		{
			return (Gdk.Pixbuf)GtkToolkit.GetNativeImage (image.WithSize (size));
		}

		public static Xwt.Drawing.Image WithSize (this Xwt.Drawing.Image image, Gtk.IconSize size)
		{
			int w, h;
			size.GetSize (out w, out h);
			return image.WithSize (w, h);
		}

		public static Xwt.Size GetSize (this IconSize size)
		{
			var displayScale = Platform.IsWindows ? GtkWorkarounds.GetScaleFactor () : 1.0;
			int w, h;
			size.GetSize (out w, out h);
			return new Xwt.Size ((double)w / displayScale, (double)h / displayScale);
		}

		public static void GetSize (this IconSize size, out int width, out int height)
		{
			if (!Icon.SizeLookup (size, out width, out height))
				return;
			if (size == IconSize.Menu)
				width = height = 16;
		}

		public static Gdk.Rectangle ToGdkRectangle (this Xwt.Rectangle rect)
		{
			return new Gdk.Rectangle ((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
		}

		public static Xwt.Rectangle ToXwtRectangle (this Gdk.Rectangle rect)
		{
			return new Xwt.Rectangle (rect.X, rect.Y, rect.Width, rect.Height);
		}

		public static Gdk.Point ToGdkPoint (this Xwt.Point point)
		{
			return new Gdk.Point ((int)point.X, (int)point.Y);
		}

		public static Xwt.Point ToXwtPoint (this Gdk.Point point)
		{
			return new Xwt.Point (point.X, point.Y);
		}

		#if MAC
		static Gdk.Point ConvertToGdkCoordinates (this AppKit.NSScreen screen, Gdk.Point screenPoint)
		{
			if (screen == null)
				return Gdk.Point.Zero;
			var monitor = AppKit.NSScreen.Screens.IndexOf (screen);
			var macgeometry = screen.Frame;
			Gdk.Rectangle geometry = Gdk.Screen.Default.GetMonitorGeometry (monitor);

			// HACK: Cocoa screen frames are always relative to the main monitor 0, but we need absolute
			// coordinates in the Gdk system (origin is top-left corner of the left screen).

			// x position is relative to main monitor 0,
			// calculate X pos relative to current monitor first
			screenPoint.X = (int)Math.Abs (macgeometry.X - screenPoint.X);
			screenPoint.X += geometry.X;
			return screenPoint;
		}

		public static Gdk.Rectangle GetSceenBounds (this AppKit.NSView widget)
		{
			var frame = widget.Frame;
			var point = ConvertToGdkCoordinates (widget.Window?.Screen, new Gdk.Point ((int)frame.Location.X, (int)frame.Location.Y));
			frame.X = point.X;
			frame.Y = point.Y;
			return new Gdk.Rectangle ((int)frame.X, (int)frame.Y, (int)frame.Width, (int)frame.Height);
		}
		#endif

		public static Gdk.Rectangle GetSceenBounds (this Xwt.Widget widget)
		{
			var wbounds = widget.ScreenBounds.ToGdkRectangle ();
			#if MAC
			// Xwt.Widget.ScreenBounds is toolkit specific and Cocoa uses a different screen coordinate system.
			var view = widget.Surface.NativeWidget as AppKit.NSView;
			if (view != null) {
				var point = ConvertToGdkCoordinates (view.Window?.Screen, wbounds.Location);
				wbounds.X = point.X;
				wbounds.Y = point.Y;

				//var monitor = AppKit.NSScreen.Screens.IndexOf (view.Window.Screen);
				//var macgeometry = view.Window.Screen.Frame;
				//Gdk.Rectangle geometry = Gdk.Screen.Default.GetMonitorGeometry (monitor);

				//// HACK: Cocoa screen frames are always relative to the main monitor 0, but we need absolute
				//// coordinates in the Gdk system (origin is top-left corner of the left screen).

				//// x position is relative to main monitor 0,
				//// calculate X pos relative to current monitor first
				//wbounds.X = (int) Math.Abs (macgeometry.X - wbounds.X);
				//wbounds.X += geometry.X;
			}
			#endif
			return wbounds;
		}

		public static Gdk.Point ToScreenCoordinates (this Xwt.Widget widget, Xwt.Point point)
		{
			var spoint = widget.ConvertToScreenCoordinates (point).ToGdkPoint ();
			#if MAC
			// Xwt.Widget.ScreenBounds is toolkit specific and Cocoa uses a different screen coordinate system.
			var view = widget.Surface.NativeWidget as AppKit.NSView;
			if (view != null) {
				spoint = ConvertToGdkCoordinates (view.Window?.Screen, spoint);
			}
			#endif
			return spoint;
		}

		public static Gdk.Rectangle ToScreenCoordinates (Xwt.Widget widget, Xwt.Rectangle rect)
		{
			return new Gdk.Rectangle (ToScreenCoordinates (widget, rect.Location), new Gdk.Size ((int)rect.Width, (int)rect.Height));
		}

		public static Gdk.Point GetScreenCoordinates (this Gtk.Widget w, Gdk.Point p)
		{
			if (w.ParentWindow == null)
				return Gdk.Point.Zero;
			int x, y;
			w.ParentWindow.GetOrigin (out x, out y);
			var a = w.Allocation;
			x += a.X;
			y += a.Y;
			return new Gdk.Point (x + p.X, y + p.Y);
		}

		public static bool IsClickedNodeSelected (this Gtk.TreeView tree, int x, int y)
		{
			Gtk.TreePath path;
			if (tree.GetPathAtPos (x, y, out path))
				return tree.Selection.PathIsSelected (path);

			return false;
		}

		public static void EnableAutoTooltips (this Gtk.TreeView tree)
		{
			TreeViewTooltipsData data = new TreeViewTooltipsData ();
			treeData [tree] = data;
			tree.MotionNotifyEvent += HandleMotionNotifyEvent;
			tree.LeaveNotifyEvent += HandleLeaveNotifyEvent;
			tree.ButtonPressEvent += HandleButtonPressEvent;
			tree.ScrollEvent += HandleTreeScrollEvent;
			tree.Hidden += HandleTreeHidden;
			tree.Unrealized += HandleTreeHidden;
			tree.Destroyed += HandleTreeDestroyed;
		}
		
		static void ResetTooltip (Gtk.TreeView tree)
		{
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue (tree, out data))
				return;
			if (data.ShowTimer != 0)
				GLib.Source.Remove (data.ShowTimer);
			if (data.LeaveTimer != 0)
				GLib.Source.Remove (data.LeaveTimer);
			if (data.Tooltip != null)
				data.Tooltip.Destroy ();
		}

		static void HandleTreeHidden (object sender, EventArgs e)
		{
			ResetTooltip ((Gtk.TreeView) sender);
		}

		static void HandleTreeDestroyed (object sender, EventArgs e)
		{
			var tree = (Gtk.TreeView)sender;

			ResetTooltip (tree);
			treeData.Remove (tree);
		}

		[GLib.ConnectBeforeAttribute]
		static void HandleTreeScrollEvent (object o, ScrollEventArgs args)
		{
			HideTooltip ((Gtk.TreeView)o);
		}

		[GLib.ConnectBeforeAttribute]
		static void HandleLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
		{
			TreeView tree = (TreeView) o;
			ScheduleHideTooltip (tree);
		}

		internal static void ScheduleHideTooltip (TreeView tree)
		{
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue (tree, out data))
				return;
			data.LeaveTimer = GLib.Timeout.Add (50, delegate {
				data.LeaveTimer = 0;
				HideTooltip (tree);
				return false;
			});
		}

		internal static void UnscheduleHideTooltip (TreeView tree)
		{
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue (tree, out data))
				return;
			if (data.LeaveTimer != 0) {
				GLib.Source.Remove (data.LeaveTimer);
				data.LeaveTimer = 0;
			}
		}

		internal static void HideTooltip (TreeView tree)
		{
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue (tree, out data))
				return;
			if (data.ShowTimer != 0) {
				GLib.Source.Remove (data.ShowTimer);
				data.ShowTimer = 0;
				return;
			}
			if (data.LeaveTimer != 0) {
				GLib.Source.Remove (data.LeaveTimer);
				data.LeaveTimer = 0;
			}
			if (data.Tooltip != null) {
				data.Tooltip.Destroy ();
				data.Tooltip = null;
			}
		}

		[GLib.ConnectBeforeAttribute]
		static void HandleMotionNotifyEvent(object o, MotionNotifyEventArgs args)
		{
			TreeView tree = (TreeView) o;
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue (tree, out data))
				return;

			HideTooltip (tree);

			int cx, cy;
			TreePath path;
			TreeViewColumn col;
			if (!tree.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path, out col, out cx, out cy))
				return;
			data.Path = path;
			data.ShowTimer = GLib.Timeout.Add (300, delegate {
				data.ShowTimer = 0;
				int ox, oy;
				tree.BinWindow.GetOrigin (out ox, out oy);
				Gdk.Rectangle rect = tree.GetCellArea (path, col);
				data.Tooltip = new CellTooltipWindow (tree, col, path);
				if (rect.X + data.Tooltip.SizeRequest ().Width > tree.Allocation.Width) {
					data.Tooltip.Move (ox + rect.X - 1, oy + rect.Y);
					data.Tooltip.ShowAll ();
				} else {
					data.Tooltip.Destroy ();
					data.Tooltip = null;
				}
				return false;
			});
		}
		
		[GLib.ConnectBeforeAttribute]
		static void HandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs ev)
		{
			UnscheduleTooltipShow (o);
		}
		
		static void UnscheduleTooltipShow (object tree)
		{
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue ((Gtk.TreeView)tree, out data))
				return;
			if (data.ShowTimer != 0) {
				GLib.Source.Remove (data.ShowTimer);
				data.ShowTimer = 0;
			}
		}

		public static bool GetCellForegroundSet (this Gtk.CellRendererText cell)
		{
			GLib.Value property = cell.GetProperty ("foreground-set");
			bool result = (bool)property;
			property.Dispose ();
			return result;
		}

		public static void SetCellForegroundSet (this Gtk.CellRendererText cell, bool value)
		{
			GLib.Value val = new GLib.Value (value);
			cell.SetProperty ("foreground-set", val);
			val.Dispose ();
		}

		public static Gdk.Rectangle ToScreenCoordinates (Gtk.Widget widget, Gdk.Window w, Gdk.Rectangle rect)
		{
			return new Gdk.Rectangle (ToScreenCoordinates (widget, w, rect.X, rect.Y), rect.Size);
		}

		public static Gdk.Point ToScreenCoordinates (Gtk.Widget widget, Gdk.Window w, int x, int y)
		{
			int ox, oy;
			w.GetOrigin (out ox, out oy);
			//Bug 31032 - this is workaround bug in GTK on Windows OS which has widget.Allocation.X/Y
			//relative to widget.GdkWindow.Toplevel instead to widget.GdkWindow which is GdkWindow decicated
			//to TreeView so widget.Allocation.X/Y should always be 0,0(which is true on Mac)
			//hence skipping adding Allocation.X/Y since they should always be 0,0 anyway
			if (!(widget is TreeView)) {
				ox += widget.Allocation.X;
				oy += widget.Allocation.Y;
			}
			return new Gdk.Point (ox + x, oy + y);
		}

		public static Gdk.Rectangle ToWindowCoordinates (Gtk.Widget widget, Gdk.Window w, Gdk.Rectangle rect)
		{
			return new Gdk.Rectangle (ToWindowCoordinates (widget, w, rect.X, rect.Y), rect.Size);
		}
		
		public static Gdk.Point ToWindowCoordinates (Gtk.Widget widget, Gdk.Window w, int x, int y)
		{
			int ox, oy;
			w.GetOrigin (out ox, out oy);
			ox += widget.Allocation.X;
			oy += widget.Allocation.Y;
			return new Gdk.Point (x - ox, y - oy);
		}

		public static T ReplaceWithWidget<T> (this Gtk.Widget oldWidget, T newWidget, bool transferChildren = false) where T:Gtk.Widget
		{
			Gtk.Container parent = (Gtk.Container) oldWidget.Parent;
			if (parent == null)
				throw new InvalidOperationException ();

			if (parent is Box) {
				var box = (Box) parent;
				var bc = (Gtk.Box.BoxChild) parent [oldWidget];
				box.Add (newWidget);
				var nc = (Gtk.Box.BoxChild) parent [newWidget];
				nc.Expand = bc.Expand;
				nc.Fill = bc.Fill;
				nc.PackType = bc.PackType;
				nc.Padding = bc.Padding;
				nc.Position = bc.Position;
				box.Remove (oldWidget);
			}
			else if (parent is Table) {
				var table = (Table) parent;
				var bc = (Gtk.Table.TableChild) parent [oldWidget];
				table.Add (newWidget);
				var nc = (Gtk.Table.TableChild) parent [newWidget];
				nc.BottomAttach = bc.BottomAttach;
				nc.LeftAttach = bc.LeftAttach;
				nc.RightAttach = bc.RightAttach;
				nc.TopAttach = bc.TopAttach;
				nc.XOptions = bc.XOptions;
				nc.XPadding = bc.XPadding;
				nc.YOptions = bc.YOptions;
				nc.YPadding = bc.YPadding;
				table.Remove (oldWidget);
			}
			else if (parent is Paned) {
				var paned = (Paned) parent;
				var bc = (Gtk.Paned.PanedChild) parent [oldWidget];
				var resize = bc.Resize;
				var shrink = bc.Shrink;
				if (oldWidget == paned.Child1) {
					paned.Remove (oldWidget);
					paned.Add1 (newWidget);
				} else {
					paned.Remove (oldWidget);
					paned.Add2 (newWidget);
				}
				var nc = (Gtk.Paned.PanedChild) parent [newWidget];
				nc.Resize = resize;
				nc.Shrink = shrink;
			}
			else
				throw new NotSupportedException ();

			if (transferChildren) {
				if (newWidget is Paned && oldWidget is Paned) {
					var panedOld = (Paned) oldWidget;
					var panedNew = (Paned) (object) newWidget;
					if (panedOld.Child1 != null) {
						var c = panedOld.Child1;
						var bc = (Gtk.Paned.PanedChild) panedOld [c];
						var resize = bc.Resize;
						var shrink = bc.Shrink;
						panedOld.Remove (c);
						panedNew.Add1 (c);
						var nc = (Gtk.Paned.PanedChild) panedNew [c];
						nc.Resize = resize;
						nc.Shrink = shrink;
					}
					if (panedOld.Child2 != null) {
						var c = panedOld.Child2;
						var bc = (Gtk.Paned.PanedChild) panedOld [c];
						var resize = bc.Resize;
						var shrink = bc.Shrink;
						panedOld.Remove (c);
						panedNew.Add2 (c);
						var nc = (Gtk.Paned.PanedChild) panedNew [c];
						nc.Resize = resize;
						nc.Shrink = shrink;
					}
				}
				else
					throw new NotSupportedException ();
			}

			newWidget.Visible = oldWidget.Visible;
			return newWidget;
		}

		public static bool ScreenSupportsARGB ()
		{
			return Gdk.Screen.Default.IsComposited;
		}

		/// <summary>
		/// This method can be used to get a reliave Leave event for a widget, which
		/// is not fired if the pointer leaves the widget to enter a child widget.
		/// To ubsubscribe the event, dispose the object returned by the method.
		/// </summary>
		public static IDisposable SubscribeLeaveEvent (this Gtk.Widget w, System.Action leaveHandler)
		{
			return new LeaveEventData (w, leaveHandler);
		}

		public static Gdk.EventKey CreateKeyEvent (uint keyval, Gdk.ModifierType state, Gdk.EventType eventType, Gdk.Window win)
		{
			return CreateKeyEvent (keyval, -1, state, eventType, win, null);
		}

		public static Gdk.EventKey CreateKeyEventFromKeyCode (ushort keyCode, Gdk.ModifierType state, Gdk.EventType eventType, Gdk.Window win)
		{
			return CreateKeyEvent (0, keyCode, state, eventType, win, null);
		}

		public static Gdk.EventKey CreateKeyEventFromKeyCode (ushort keyCode, Gdk.ModifierType state, Gdk.EventType eventType, Gdk.Window win, uint time)
		{
			return CreateKeyEvent (0, keyCode, state, eventType, win, time);
		}

		static Gdk.EventKey CreateKeyEvent (uint keyval, int keyCode, Gdk.ModifierType state, Gdk.EventType eventType, Gdk.Window win, uint? time)
		{
			int effectiveGroup, level;
			Gdk.ModifierType cmods;
			if (keyval == 0)
				Gdk.Keymap.Default.TranslateKeyboardState ((uint)keyCode, state, 0, out keyval, out effectiveGroup, out level, out cmods);

			Gdk.KeymapKey[] keyms = Gdk.Keymap.Default.GetEntriesForKeyval (keyval);
			if (keyms.Length == 0)
				return null;

			var nativeEvent = new NativeEventKeyStruct {
				type = eventType,
				send_event = 1,
				window = win != null ? win.Handle : IntPtr.Zero,
				state = (uint)state,
				keyval = keyval,
				group = (byte)keyms [0].Group,
				hardware_keycode = keyCode == -1 ? (ushort)keyms [0].Keycode : (ushort)keyCode,
				length = 0,
				time = time ?? Gtk.Global.CurrentEventTime
			};

			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent); 
			return new EventKeyWrapper (ptr);
		}

		static IEnumerable<Gtk.Widget> FindAllChildWidgets (this Gtk.Container container)
		{
			var widgets = new Stack<Widget> (new[] { container });

			while (widgets.Any ()) {
				var widget = widgets.Pop ();
				yield return widget;

				if (widget is Gtk.Container) {
					var c = (Gtk.Container)widget;
					foreach (var w in c.Children) {
						widgets.Push (w);
					}
				}
			}
		}

		public static void UseNativeContextMenus (this Gtk.Window window)
		{
			#if MAC
			var entries = window.FindAllChildWidgets ().OfType<Gtk.Entry> ();
			foreach (var entry in entries) {
				entry.UseNativeContextMenus ();
			}
			#endif
		}

		public static void UseNativeContextMenus (this Gtk.Entry entry)
		{
			#if MAC
			entry.ButtonPressEvent += EntryButtonPressHandler;
			#endif
		}

		static void ShowNativeContextMenu (this Gtk.Entry entry, Gdk.EventButton evt)
		{
			var context_menu = new ContextMenu ();

			var cut = new ContextMenuItem { Label = GettextCatalog.GetString ("Cut"), Context = entry };
			cut.Clicked += CutClicked;
			context_menu.Items.Add (cut);

			var copy = new ContextMenuItem { Label = GettextCatalog.GetString ("Copy"), Context = entry };
			copy.Clicked += CopyClicked;
			context_menu.Items.Add (copy);

			var paste = new ContextMenuItem { Label = GettextCatalog.GetString ("Paste"), Context = entry };
			paste.Clicked += PasteClicked;
			context_menu.Items.Add (paste);

			context_menu.Items.Add (new SeparatorContextMenuItem ());

			var delete = new ContextMenuItem { Label = GettextCatalog.GetString ("Delete"), Context = entry };
			delete.Clicked += DeleteClicked;
			context_menu.Items.Add (delete);

			context_menu.Items.Add (new SeparatorContextMenuItem ());

			var select_all = new ContextMenuItem { Label = GettextCatalog.GetString ("Select All"), Context = entry };
			select_all.Clicked += SelectAllClicked;
			context_menu.Items.Add (select_all);

			/* Update the menu items' sensitivities */
			copy.Sensitive = select_all.Sensitive = (entry.Text.Length > 0);
			cut.Sensitive = delete.Sensitive = (entry.Text.Length > 0 && entry.IsEditable);
			paste.Sensitive = entry.IsEditable;

			context_menu.Show (entry, evt);
		}

		static void CutClicked (object o, ContextMenuItemClickedEventArgs e)
		{
			var entry = (Gtk.Entry)e.Context;

			if (entry.IsEditable) {
				int selection_start, selection_end;

				if (entry.GetSelectionBounds (out selection_start, out selection_end)) {
					var text = entry.GetChars (selection_start, selection_end);
					var clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

					clipboard.Text = text;
					entry.DeleteText (selection_start, selection_end);
				}
			} else {
				entry.ErrorBell ();
			}
		}

		static void CopyClicked (object o, ContextMenuItemClickedEventArgs e)
		{
			var entry = (Gtk.Entry)e.Context;
			int selection_start, selection_end;

			if (entry.GetSelectionBounds (out selection_start, out selection_end)) {
				var text = entry.GetChars (selection_start, selection_end);
				var clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

				clipboard.Text = text;
			}
		}

		static void PasteClicked (object o, ContextMenuItemClickedEventArgs e)
		{
			var entry = (Gtk.Entry)e.Context;

			if (entry.IsEditable) {
				var clipboard = Gtk.Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

				clipboard.RequestText ((cb, text) => {
					entry.InsertText (text);
				});
			} else {
				entry.ErrorBell ();
			}
		}

		static void DeleteClicked (object o, ContextMenuItemClickedEventArgs e)
		{
			var entry = (Gtk.Entry)e.Context;

			if (entry.IsEditable) {
				int selection_start, selection_end;

				if (entry.GetSelectionBounds (out selection_start, out selection_end)) {
					entry.DeleteText (selection_start, selection_end);
				}
			}
		}

		static void SelectAllClicked (object o, ContextMenuItemClickedEventArgs e)
		{
			var entry = (Gtk.Entry)e.Context;

			entry.SelectRegion (0, entry.Text.Length - 1);
		}

		[GLib.ConnectBefore]
		static void EntryButtonPressHandler (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				var entry = (Gtk.Entry)o;

				entry.ShowNativeContextMenu (args.Event);
				args.RetVal = true;
			}
		}

		/// <summary>
		/// Shows the context menu for a TreeView.
		/// </summary>
		/// <returns><c>true</c>, if context menu was shown, <c>false</c> otherwise.</returns>
		/// <param name="tree">Gtk TreeView for which the context menu is shown</param>
		/// <param name="evt">The current mouse event, or <c>null</c>.</param>
		/// <param name="entrySet">Entry set with the command definitions</param>
		/// <param name="initialCommandTarget">Initial command target.</param>
		public static bool ShowContextMenu (this Gtk.TreeView tree, Gdk.EventButton evt, Commands.CommandEntrySet entrySet,
			object initialCommandTarget = null)
		{
			if (evt == null) {
				var paths = tree.Selection.GetSelectedRows ();
				if (paths != null) {
					var area = tree.GetCellArea (paths [0], tree.Columns [0]);
					return Ide.IdeApp.CommandService.ShowContextMenu (tree, area.Left, area.Top, entrySet, initialCommandTarget);
				} else
					return Ide.IdeApp.CommandService.ShowContextMenu (tree, 0, 0, entrySet, initialCommandTarget);
			} else {
				int x = (int)evt.X, y = (int)evt.Y;
				if (Platform.IsMac && tree.BinWindow == evt.Window)
					tree.ConvertBinWindowToWidgetCoords (x, y, out x, out y);
				return Ide.IdeApp.CommandService.ShowContextMenu (tree, x, y, entrySet, initialCommandTarget);
			}
		}
	}

	class EventKeyWrapper: Gdk.EventKey
	{
		IntPtr ptr;

		public EventKeyWrapper (IntPtr ptr): base (ptr)
		{
			this.ptr = ptr;
		}

		~EventKeyWrapper ()
		{
			Marshal.FreeHGlobal (ptr);
		}
	}

	class LeaveEventData: IDisposable
	{
		public System.Action LeaveHandler;
		public Gtk.Widget RootWidget;
		public bool Inside;

		public LeaveEventData (Gtk.Widget w, System.Action leaveHandler)
		{
			RootWidget = w;
			LeaveHandler = leaveHandler;
			if (w.IsRealized) {
				RootWidget.Unrealized += HandleUnrealized;
				TrackLeaveEvent (w);
			}
			else
				w.Realized += HandleRealized;
		}

		void HandleRealized (object sender, EventArgs e)
		{
			RootWidget.Realized -= HandleRealized;
			RootWidget.Unrealized += HandleUnrealized;
			TrackLeaveEvent (RootWidget);
		}

		void HandleUnrealized (object sender, EventArgs e)
		{
			RootWidget.Unrealized -= HandleUnrealized;
			UntrackLeaveEvent (RootWidget);
			RootWidget.Realized += HandleRealized;
			if (Inside) {
				Inside = false;
				LeaveHandler ();
			}
		}

		public void Dispose ()
		{
			if (RootWidget.IsRealized) {
				UntrackLeaveEvent (RootWidget);
				RootWidget.Unrealized -= HandleUnrealized;
			} else {
				RootWidget.Realized -= HandleRealized;
			}
		}

		public void TrackLeaveEvent (Gtk.Widget w)
		{
			w.LeaveNotifyEvent += HandleLeaveNotifyEvent;
			w.EnterNotifyEvent += HandleEnterNotifyEvent;
			if (w is Gtk.Container) {
				((Gtk.Container)w).Added += HandleAdded;
				((Gtk.Container)w).Removed += HandleRemoved;
				foreach (var c in ((Gtk.Container)w).Children)
					TrackLeaveEvent (c);
			}
		}

		void UntrackLeaveEvent (Gtk.Widget w)
		{
			w.LeaveNotifyEvent -= HandleLeaveNotifyEvent;
			w.EnterNotifyEvent -= HandleEnterNotifyEvent;
			if (w is Gtk.Container) {
				((Gtk.Container)w).Added -= HandleAdded;
				((Gtk.Container)w).Removed -= HandleRemoved;
				foreach (var c in ((Gtk.Container)w).Children)
					UntrackLeaveEvent (c);
			}
		}

		void HandleRemoved (object o, RemovedArgs args)
		{
			UntrackLeaveEvent (args.Widget);
		}

		void HandleAdded (object o, AddedArgs args)
		{
			TrackLeaveEvent (args.Widget);
		}

		void HandleEnterNotifyEvent (object o, EnterNotifyEventArgs args)
		{
			Inside = true;
		}

		void HandleLeaveNotifyEvent (object o, LeaveNotifyEventArgs args)
		{
			Inside = false;

			// Delay the call to the leave handler since the pointer may be
			// entering a child widget, in which case the event doesn't have to be fired
			Gtk.Application.Invoke ((o2, a2) => {
				if (!Inside)
					LeaveHandler ();
			});
		}
	}

	class TreeViewTooltipsData
	{
		public uint ShowTimer;
		public uint LeaveTimer;
		public TreePath Path;
		public CellTooltipWindow Tooltip;
	}

	class CellTooltipWindow: TooltipWindow
	{
		TreeViewColumn col;
		TreeView tree;
		TreeIter iter;

		public CellTooltipWindow (TreeView tree, TreeViewColumn col, TreePath path)
		{
			this.tree = tree;
			this.col = col;
			
			NudgeHorizontal = true;
			
			Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
			
			Gdk.Rectangle rect = tree.GetCellArea (path, col);
			rect.Inflate (2, 2);
			
			if (!tree.Model.GetIter (out iter, path)) {
				Hide ();
				return;
			}

			col.CellSetCellData (tree.Model, iter, false, false);
			int x = 0;
			int th = 0;
			CellRenderer[] renderers = col.CellRenderers;
			foreach (CellRenderer cr in renderers) {
				int sp, wi, he, xo, yo;
				col.CellGetPosition (cr, out sp, out wi);
				Gdk.Rectangle crect = new Gdk.Rectangle (x, rect.Y, wi, rect.Height);
				cr.GetSize (tree, ref crect, out xo, out yo, out wi, out he);
				if (cr != renderers [renderers.Length - 1])
					x += crect.Width + col.Spacing + 1;
				else
					x += wi + 1;
				if (he > th) th = he;
			}
			SetSizeRequest (x, th + 2);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			base.OnExposeEvent (evnt);

			Gdk.Rectangle expose = Allocation;
			Gdk.Color save = Gdk.Color.Zero;
			bool hasFgColor = false;
			int x = 1;

			col.CellSetCellData (tree.Model, iter, false, false);

			foreach (CellRenderer cr in col.CellRenderers) {
				if (!cr.Visible)
					continue;

				if (cr is CellRendererText) {
					hasFgColor = ((CellRendererText)cr).GetCellForegroundSet ();
					save = ((CellRendererText)cr).ForegroundGdk;
					((CellRendererText)cr).ForegroundGdk = Style.Foreground (State);
				}

				int sp, wi, he, xo, yo;
				col.CellGetPosition (cr, out sp, out wi);
				Gdk.Rectangle bgrect = new Gdk.Rectangle (x, expose.Y, wi, expose.Height - 2);
				cr.GetSize (tree, ref bgrect, out xo, out yo, out wi, out he);
				int leftMargin = (int) ((bgrect.Width - wi) * cr.Xalign);
				int topMargin = (int) ((bgrect.Height - he) * cr.Yalign);
				Gdk.Rectangle cellrect = new Gdk.Rectangle (bgrect.X + leftMargin, bgrect.Y + topMargin + 1, wi, he);
				cr.Render (this.GdkWindow, this, bgrect, cellrect, expose, CellRendererState.Focused);
				x += bgrect.Width + col.Spacing + 1;

				if (cr is CellRendererText) {
					((CellRendererText)cr).ForegroundGdk = save;
					((CellRendererText)cr).SetCellForegroundSet (hasFgColor);
				}
			}

			return true;
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			bool res = base.OnButtonPressEvent (evnt);
			CreateEvent (evnt);
			return res;
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			bool res = base.OnButtonReleaseEvent (evnt);
			CreateEvent (evnt);
			return res;
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			bool res = base.OnScrollEvent (evnt);
			CreateEvent (evnt);
			return res;
		}
		
		void CreateEvent (Gdk.EventButton refEvent)
		{
			int rx, ry;
			tree.BinWindow.GetOrigin (out rx, out ry);
			NativeEventButtonStruct nativeEvent = new NativeEventButtonStruct (); 
			nativeEvent.type = refEvent.Type;
			nativeEvent.send_event = 1;
			nativeEvent.window = tree.BinWindow.Handle;
			nativeEvent.x = refEvent.XRoot - rx;
			nativeEvent.y = refEvent.YRoot - ry;
			nativeEvent.x_root = refEvent.XRoot;
			nativeEvent.y_root = refEvent.YRoot;
			nativeEvent.time = refEvent.Time;
			nativeEvent.state = (uint) refEvent.State;
			nativeEvent.button = refEvent.Button;
			nativeEvent.device = refEvent.Device.Handle;

			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent); 
			try {
				Gdk.EventButton evnt = new Gdk.EventButton (ptr); 
				Gdk.EventHelper.Put (evnt); 
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}
		
		void CreateEvent (Gdk.EventScroll refEvent)
		{
			int rx, ry;
			tree.BinWindow.GetOrigin (out rx, out ry);
			NativeEventScrollStruct nativeEvent = new NativeEventScrollStruct (); 
			nativeEvent.type = refEvent.Type;
			nativeEvent.send_event = 1;
			nativeEvent.window = tree.BinWindow.Handle;
			nativeEvent.x = refEvent.XRoot - rx;
			nativeEvent.y = refEvent.YRoot - ry;
			nativeEvent.x_root = refEvent.XRoot;
			nativeEvent.y_root = refEvent.YRoot;
			nativeEvent.time = refEvent.Time;
			nativeEvent.direction = refEvent.Direction;
			nativeEvent.state = (uint) refEvent.State;
			nativeEvent.device = refEvent.Device.Handle;

			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent); 
			try {
				Gdk.EventScroll evnt = new Gdk.EventScroll (ptr); 
				Gdk.EventHelper.Put (evnt); 
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			// While showing a window, if the cursor is in the area of the new window, sometimes we get several Enter/Leave events
			// in sequence, until the window is fully visible. To avoid hiding the window too early, we schedule here a tooltip
			// hide, which will be canceled if we get a new Enter event.
			GtkUtil.ScheduleHideTooltip (tree);
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			GtkUtil.UnscheduleHideTooltip (tree);
			return base.OnEnterNotifyEvent (evnt);
		}
	}

	// Analysis disable InconsistentNaming
	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventKeyStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public uint state; 
		public uint keyval; 
		public int length;
		public IntPtr str;
		public ushort hardware_keycode;
		public byte group;
		public uint is_modifier;
	} 

	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventButtonStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public double x; 
		public double y; 
		public IntPtr axes; 
		public uint state; 
		public uint button; 
		public IntPtr device; 
		public double x_root; 
		public double y_root; 
	} 
	
	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventScrollStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public double x; 
		public double y; 
		public uint state; 
		public Gdk.ScrollDirection direction;
		public IntPtr device; 
		public double x_root; 
		public double y_root;
		//FIXME: scroll deltas
	} 
}
