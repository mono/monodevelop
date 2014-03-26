//
// PlaceholderWindow.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using Gdk;
using Gtk;
using MonoDevelop.Components.Docking;
using Mono.TextEditor;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using System;
using System.Linq;

namespace MonoDevelop.Components.DockNotebook
{
	class PlaceholderWindow: Gtk.Window
	{
		Gdk.GC redgc;
		uint anim;
		int rx, ry, rw, rh;

		int controlKeyMask;

		static PlaceholderWindow ()
		{
			IdeApp.Workbench.ActiveDocumentChanged += delegate {
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc == null)
					return;
				var rootWindow = doc.Window.ActiveViewContent.Control.Toplevel as DockWindow;
				if (rootWindow == null)
					return;
				
				rootWindow.Title = DefaultWorkbench.GetTitle (doc.Window);
			};
		}

		IDockNotebookTab frame;
		
		public PlaceholderWindow (IDockNotebookTab frame): base (Gtk.WindowType.Toplevel)
		{
			this.frame = frame;
			SkipTaskbarHint = true;
			Decorated = false;
			TransientFor = IdeApp.Workbench.RootWindow;
			TypeHint = WindowTypeHint.Utility;
			KeepAbove = true;
			// Create the mask for the arrow

			Realize ();
			redgc = new Gdk.GC (GdkWindow);
			redgc.RgbFgColor = frame.Content.Style.Background (StateType.Selected);
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape)
				Destroy ();
			if (evnt.Key == Gdk.Key.Control_L)
				controlKeyMask |= 1;
			if (evnt.Key == Gdk.Key.Control_R)
				controlKeyMask |= 2;
			MovePosition (curX, curY);

			return base.OnKeyPressEvent (evnt);
		}


		protected override bool OnKeyReleaseEvent (EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Control_L)
				controlKeyMask &= ~1;
			if (evnt.Key == Gdk.Key.Control_R)
				controlKeyMask &= ~2;
			MovePosition (curX, curY);

			return base.OnKeyReleaseEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			Destroy ();
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			Destroy ();
			return base.OnLeaveNotifyEvent (evnt);
		}


		internal static List<DockNotebook> newNotebooks = new List<DockNotebook> ();

		static IEnumerable<DockNotebook> AllNotebooks ()
		{
			yield return ((DefaultWorkbench)IdeApp.Workbench.RootWindow).TabControl;
			foreach (var notebook in newNotebooks)
				yield return notebook;
		}

		DockNotebook hoverNotebook;

		bool CanPlaceInHoverNotebook ()
		{
			return controlKeyMask == 0 && hoverNotebook != null && (hoverNotebook.TabCount != 1 || hoverNotebook.TabCount == 1 && hoverNotebook.Tabs [0] != frame);
		}

		int curX, curY;
		public void MovePosition (int x, int y)
		{
			this.curX = x;
			this.curY = y;
			hoverNotebook = null;
			this.KeepAbove = true;
			
			// TODO: Handle z-ordering of floating windows.
			int ox = 0, oy = 0;
			foreach (var notebook in AllNotebooks ()) {
				if (notebook.GdkWindow == null)
					continue;
				int ox2, oy2;
				notebook.GdkWindow.GetOrigin (out ox2, out oy2); 
				var alloc = notebook.Allocation;
				if (ox2 <= x && x <= ox2 + alloc.Width && oy2 <= y && y <= oy2 + alloc.Height) {
					hoverNotebook = notebook;
					ox = ox2;
					oy = oy2;
				}
			}
			
			if (CanPlaceInHoverNotebook ()) {
				var alloc = hoverNotebook.Allocation;
				if (x <= ox + DockFrame.GroupDockSeparatorSize) {
					Relocate (
						ox, 
						oy, 
						alloc.Width / 3,  
						alloc.Height, 
						false
					); 
					placementDelegate = delegate(DockNotebook arg1, IDockNotebookTab tab, Rectangle allocation2, int x2, int y2) {
						var window = (SdiWorkspaceWindow)tab.Content;
						var container = (DockNotebookContainer)hoverNotebook.Parent;
						container.InsertLeft (window);
					};
					return;
				}

				if (x >= ox + alloc.Width - DockFrame.GroupDockSeparatorSize) {
					Relocate (
						ox + alloc.Width * 2 / 3, 
						oy, 
						alloc.Width / 3,  
						alloc.Height, 
						false
					); 
					placementDelegate = delegate(DockNotebook arg1, IDockNotebookTab tab, Rectangle allocation2, int x2, int y2) {
						var window = (SdiWorkspaceWindow)tab.Content;
						var container = (DockNotebookContainer)hoverNotebook.Parent;
						container.InsertRight (window);
					};
					return;
				}

				if (y <= oy + DockFrame.GroupDockSeparatorSize) {
					Relocate (
						ox, 
						oy, 
						alloc.Width,  
						alloc.Height / 3, 
						false
					); 
					placementDelegate = delegate(DockNotebook arg1, IDockNotebookTab tab, Rectangle allocation2, int x2, int y2) {
						var window = (SdiWorkspaceWindow)tab.Content;
						var container = (DockNotebookContainer)hoverNotebook.Parent;
						container.InsertTop (window);
					};
					return;
				}

				if (y >= oy + alloc.Height - DockFrame.GroupDockSeparatorSize) {
					Relocate (
						ox, 
						oy + alloc.Height * 2 / 3, 
						alloc.Width,  
						alloc.Height / 3, 
						false
					); 
					placementDelegate = delegate(DockNotebook arg1, IDockNotebookTab tab, Rectangle allocation2, int x2, int y2) {
						var window = (SdiWorkspaceWindow)tab.Content;
						var container = (DockNotebookContainer)hoverNotebook.Parent;
						container.InsertBottom (window);
					};
					return;
				}
				
				if (!hoverNotebook.Tabs.Contains (frame)) {
					Relocate (
						ox + alloc.Width / 3, 
						oy + alloc.Height / 3, 
						alloc.Width - alloc.Width * 2 / 3, 
						alloc.Height - alloc.Height * 2 / 3, 
						false
					); 
					placementDelegate = PlaceInHoverNotebook;
					return;
				}
			}

			Relocate (
				x - w / 2, 
				y - h / 2, 
				w, 
				h, 
				false
			); 
			placementDelegate = PlaceInFloatingFrame;
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			GdkWindow.Opacity = 0.6;
		}

		void CreateShape (int width, int height)
		{
			Color black, white;
			black = new Color (0, 0, 0);
			black.Pixel = 1;
			white = new Color (255, 255, 255);
			white.Pixel = 0;

			var pm = new Pixmap (GdkWindow, width, height, 1);
			var gc = new Gdk.GC (pm);
			gc.Background = white;
			gc.Foreground = white;
			pm.DrawRectangle (gc, true, 0, 0, width, height);

			gc.Foreground = black;
			pm.DrawRectangle (gc, false, 0, 0, width - 1, height - 1);
			pm.DrawRectangle (gc, false, 1, 1, width - 3, height - 3);

			ShapeCombineMask (pm, 0, 0);
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			CreateShape (allocation.Width, allocation.Height);
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			//base.OnExposeEvent (args);
			int w, h;
			GetSize (out w, out h);
			GdkWindow.DrawRectangle (redgc, false, 0, 0, w-1, h-1);
			GdkWindow.DrawRectangle (redgc, false, 1, 1, w-3, h-3);
			return true;
		}

		public void Relocate (int x, int y, int w, int h, bool animate)
		{
			var geometry = Screen.GetUsableMonitorGeometry (Screen.GetMonitorAtPoint (x, y));
			if (x < geometry.X)
				x = geometry.X;
			if (x + w > geometry.Right)
				x = geometry.Right - w;
			if (y < geometry.Y)
				y = geometry.Y;
			if (y > geometry.Bottom - h)
				y = geometry.Bottom - h;

			if (x != rx || y != ry || w != rw || h != rh) {
				Resize (w, h);
				Move (x, y);

				rx = x; ry = y; rw = w; rh = h;

				if (anim != 0) {
					GLib.Source.Remove (anim);
					anim = 0;
				}
				if (animate && w < 150 && h < 150) {
					const int sa = 7;
					Move (rx-sa, ry-sa);
					Resize (rw+sa*2, rh+sa*2);
					anim = GLib.Timeout.Add (10, RunAnimation);
				}
			}
		}

		bool RunAnimation ()
		{
			int cx, cy, ch, cw;
			GetSize (out cw, out ch);
			GetPosition	(out cx, out cy);

			if (cx != rx) {
				cx++; cy++;
				ch-=2; cw-=2;
				Move (cx, cy);
				Resize (cw, ch);
				return true;
			}
			anim = 0;
			return false;
		}

		public DockDelegate DockDelegate { get; private set; }
		public Rectangle DockRect { get; private set; }

		public void SetDockInfo (DockDelegate dockDelegate, Rectangle rect)
		{
			DockDelegate = dockDelegate;
			DockRect = rect;
		}
		
		class DockWindow : Gtk.Window
		{
			public DockWindow () : base (Gtk.WindowType.Toplevel)
			{
				IdeApp.CommandService.RegisterTopWindow (this);
				AddAccelGroup (IdeApp.CommandService.AccelGroup);
				this.DeleteEvent += delegate(object o, DeleteEventArgs args) {
					var documents = IdeApp.Workbench.Documents.Where(d => d.Window.ViewContent.Control.Toplevel == this).ToList ();
//					bool showDirtyDialog = false;
//					foreach (var content in documents) {
//						if (content.IsDirty) {
//							showDirtyDialog = true;
//							break;
//						}
//					}
//
//					if (showDirtyDialog) {
//						var dlg = new MonoDevelop.Ide.Gui.Dialogs.DirtyFilesDialog ();
//						dlg.Modal = true;
//						if (MessageService.ShowCustomDialog (dlg, this) != (int)Gtk.ResponseType.Ok) {
//							args.RetVal = true;
//							return;
//						}
//					}
					foreach (var d in documents) {
						if (!d.Close ())
							args.RetVal = true;
					}
				};
			}

			protected override bool OnConfigureEvent (EventConfigure evnt)
			{
				((DefaultWorkbench)IdeApp.Workbench.RootWindow).SetActiveWidget (Focus);
				return base.OnConfigureEvent (evnt);
			}

			protected override bool OnFocusInEvent (EventFocus evnt)
			{
				((DefaultWorkbench)IdeApp.Workbench.RootWindow).SetActiveWidget (Focus);
				return base.OnFocusInEvent (evnt);
			}

			protected override bool OnKeyPressEvent (EventKey evnt)
			{
				return ((DefaultWorkbench)IdeApp.Workbench.RootWindow).FilterWindowKeypress (evnt) || base.OnKeyPressEvent (evnt);
			}
			
			static void RemoveWindows (DockNotebookContainer container)
			{
				var notebook = container.TabControl;
				while (notebook.TabCount > 0) {
					((SdiWorkspaceWindow)notebook.Tabs[0].Content).CloseWindow (true);
				}
				newNotebooks.Remove (notebook); 
			}
			
			void RemoveWindows (Widget widget)
			{
				var dockNotebook = widget as DockNotebookContainer;
				if (dockNotebook != null) {
					RemoveWindows (dockNotebook);
					return;
				}
				
				var panedControl = widget as Paned;
				if (panedControl != null) {
					RemoveWindows (panedControl.Child1);
					RemoveWindows (panedControl.Child2);
					return;
				}
			}

			protected override void OnDestroyed ()
			{
				RemoveAccelGroup (IdeApp.CommandService.AccelGroup);
				RemoveWindows (Child);
				base.OnDestroyed ();
			}
		}

		static void PlaceInFloatingFrame (DockNotebook notebook, IDockNotebookTab tab, Rectangle allocation, int ox, int oy)
		{
			var newWindow = new DockWindow ();
			var newNotebook = new SdiDragNotebook ((DefaultWorkbench)IdeApp.Workbench.RootWindow);
			newNotebook.NavigationButtonsVisible = false;
			newNotebooks.Add (newNotebook);
			
			var box = new VBox ();
			box.PackStart (new DockNotebookContainer (newNotebook), true, true, 0);
			newWindow.Child = box;
			newNotebook.InitSize ();

			var window2 = (SdiWorkspaceWindow)tab.Content;
			var newTab2 = newNotebook.InsertTab (-1); 
			newTab2.Content = window2;
			newWindow.Title = DefaultWorkbench.GetTitle (window2);
			newWindow.ShowAll (); 

			window2.SetDockNotebook (newNotebook, newTab2); 
			newWindow.Move (ox + allocation.Width / 2 - w / 2, oy + allocation.Height / 2 - h / 2);
			newWindow.Resize (w, h);
		}

		const int w = 640;
		const int h = 480;

		void PlaceInHoverNotebook (DockNotebook notebook, IDockNotebookTab tab, Rectangle allocation, int ox, int oy)
		{
			var window = (SdiWorkspaceWindow)tab.Content;
			var newTab = hoverNotebook.InsertTab (-1); 
			newTab.Content = window;
			window.SetDockNotebook (hoverNotebook, newTab); 
			window.SelectWindow ();
		}

		Action<DockNotebook, IDockNotebookTab, Rectangle, int, int> placementDelegate;

		public void PlaceWindow (DockNotebook notebook)
		{
			int ox, oy;
			GdkWindow.GetOrigin (out ox, out oy); 

			var allocation = Allocation;
			Destroy ();

			var tab = notebook.CurrentTab;
			notebook.RemoveTab (tab.Index, false); 

			(placementDelegate ?? PlaceInFloatingFrame) (notebook, tab, allocation, ox, oy);
		}
	}
}
