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
	class DockWindow : Gtk.Window
	{
		DockNotebookContainer container;

		public DockNotebookContainer Container {
			get {
				return container;
			}

			set {
				container = value;
				Child = value;
			}
		}

		bool IsChildOfMe (Document d)
		{
			Widget control = ((SdiWorkspaceWindow)d.Window).TabControl;
			while (control.Parent != null)
				control = control.Parent;
			return control == this;
		}

		public DockWindow () : base (Gtk.WindowType.Toplevel)
		{
			IdeApp.Workbench.FloatingEditors.Add (this);
			IdeApp.CommandService.RegisterTopWindow (this);
			AddAccelGroup (IdeApp.CommandService.AccelGroup);
			this.DeleteEvent += delegate(object o, DeleteEventArgs args) {
				var documents = IdeApp.Workbench.Documents.Where (IsChildOfMe).ToList ();
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
					if (!d.Close ()) {
						args.RetVal = true;
						break;
					}
				}
			};
		}

		public DockNotebookTab AddTab ()
		{
			if (Container == null) {
				// This dock window doesn't yet have any tabs inserted.
				var addToControl = new SdiDragNotebook ((DefaultWorkbench)IdeApp.Workbench.RootWindow);
				addToControl.NavigationButtonsVisible = false;
				var tab = addToControl.InsertTab (-1);
				Container = new DockNotebookContainer (addToControl);
				addToControl.InitSize ();
				return tab;
			} else {
				// Use the existing tab control.
				return Container.TabControl.InsertTab (-1);
			}
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

		protected override void OnDestroyed ()
		{
			IdeApp.Workbench.FloatingEditors.Remove (this);
			RemoveAccelGroup (IdeApp.CommandService.AccelGroup);
			base.OnDestroyed ();
		}
	}

	class PlaceholderWindow: Gtk.Window
	{
		Gdk.GC redgc;
		uint anim;
		int rx, ry, rw, rh;

		int controlKeyMask;
		DocumentTitleWindow titleWindow;

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

		DockNotebookTab frame;
		
		public PlaceholderWindow (DockNotebookTab tab): base (Gtk.WindowType.Toplevel)
		{
			this.frame = tab;
			SkipTaskbarHint = true;
			Decorated = false;
			TypeHint = WindowTypeHint.Utility;
			titleWindow = new DocumentTitleWindow (this, tab);
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

		DockNotebook hoverNotebook;

		bool CanPlaceInHoverNotebook ()
		{
			return controlKeyMask == 0 && hoverNotebook != null;
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			Gtk.Application.Invoke (delegate {
				titleWindow.Destroy ();
			});
		}

		int curX, curY;
		public void MovePosition (int x, int y)
		{
			this.curX = x;
			this.curY = y;
			hoverNotebook = null;

			titleWindow.ShowAll ();

			int winw, winh;
			titleWindow.GetSize (out winw, out winh);
			titleWindow.Move (x - winw/2, y - winh/2);
			titleWindow.GdkWindow.Raise ();

			// TODO: Handle z-ordering of floating windows.
			int ox = 0, oy = 0;
			foreach (var notebook in DockNotebook.AllNotebooks) {
				if (notebook.GdkWindow == null)
					continue;

				int ox2, oy2;
				notebook.ParentWindow.GetOrigin (out ox2, out oy2);
				var alloc = notebook.Allocation;
				ox2 += alloc.X;
				ox2 += alloc.Y;
				if (ox2 <= x && x <= ox2 + alloc.Width && oy2 <= y && y <= oy2 + alloc.Height) {
					hoverNotebook = notebook;
					TransientFor = (Gtk.Window) hoverNotebook.Toplevel;
					ox = ox2;
					oy = oy2;
				}
			}

			if (CanPlaceInHoverNotebook ()) {
				var container = (DockNotebookContainer)hoverNotebook.Parent;
				var alloc = hoverNotebook.Allocation;

				if (x <= ox + alloc.Width / 3) {
					if (container.AllowLeftInsert) {
						Relocate (
							ox,
							oy,
							alloc.Width / 2,
							alloc.Height,
							false
						);
						placementDelegate = delegate(DockNotebook arg1, DockNotebookTab tab, Rectangle allocation2, int x2, int y2) {
							var window = (SdiWorkspaceWindow)tab.Content;
							container.InsertLeft (window);
						};
						return;
					}
				}

				if (x >= ox + alloc.Width - alloc.Width / 3) {
					if (container.AllowRightInsert) {
						Relocate (
							ox + alloc.Width / 2,
							oy,
							alloc.Width / 2,
							alloc.Height,
							false
						);
						placementDelegate = delegate(DockNotebook arg1, DockNotebookTab tab, Rectangle allocation2, int x2, int y2) {
							var window = (SdiWorkspaceWindow)tab.Content;
							container.InsertRight (window);
						};
						return;
					}
				}

				Relocate (
					ox, 
					oy, 
					alloc.Width, 
					alloc.Height, 
					false
				); 
				if (!hoverNotebook.Tabs.Contains (frame))
					placementDelegate = PlaceInHoverNotebook;
				else
					placementDelegate = null;
				return;
			}

			Hide ();
/*			Relocate (
				x - w / 2, 
				y - h / 2, 
				w, 
				h, 
				false
			); */
			placementDelegate = PlaceInFloatingFrame;
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			GdkWindow.Opacity = 0.4;
		}
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			int w, h;
			GetSize (out w, out h);

			using (var ctx = CairoHelper.Create (evnt.Window)) {
				ctx.SetSourceColor (new Cairo.Color (0.17, 0.55, 0.79));
				ctx.Rectangle (Allocation.ToCairoRect ());
				ctx.Fill ();
			}
			return true;
		}

		public void Relocate (int x, int y, int w, int h, bool animate)
		{
			if (!Visible || x != rx || y != ry || w != rw || h != rh) {
				Hide ();
				if (w != rw || h != rh)
					Resize (w, h);
				Move (x, y);
				ShowAll ();

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

		static void PlaceInFloatingFrame (DockNotebook notebook, DockNotebookTab tab, Rectangle allocation, int ox, int oy)
		{
			var newWindow = new DockWindow ();
			var newTab = newWindow.AddTab ();
			var newNotebook = newTab.Notebook;

			var workspaceWindow = (SdiWorkspaceWindow)tab.Content;
			newTab.Content = workspaceWindow;
			newWindow.Title = DefaultWorkbench.GetTitle (workspaceWindow);

			workspaceWindow.SetDockNotebook (newNotebook, newTab);
			newWindow.Move (ox + allocation.Width / 2 - w / 2, oy + allocation.Height / 2 - h / 2);
			newWindow.Resize (w, h);
			newWindow.ShowAll ();
		}

		const int w = 640;
		const int h = 480;

		void PlaceInHoverNotebook (DockNotebook notebook, DockNotebookTab tab, Rectangle allocation, int ox, int oy)
		{
			var window = (SdiWorkspaceWindow)tab.Content;
			var newTab = hoverNotebook.InsertTab (-1); 
			newTab.Content = window;
			window.SetDockNotebook (hoverNotebook, newTab); 
			window.SelectWindow ();
		}

		Action<DockNotebook, DockNotebookTab, Rectangle, int, int> placementDelegate;

		public void PlaceWindow (DockNotebook notebook)
		{
			int ox, oy;
			GdkWindow.GetOrigin (out ox, out oy); 

			var allocation = Allocation;
			Destroy ();

			if (placementDelegate != null) {
				var tab = notebook.CurrentTab;
				notebook.RemoveTab (tab.Index, false); 

				placementDelegate (notebook, tab, allocation, ox, oy);

				IdeApp.Workbench.EnsureValidSplits ();
			}
		}
	}


	class DocumentTitleWindow: Gtk.Window
	{
		public DocumentTitleWindow (Gtk.Window parent, DockNotebookTab draggedItem): base (Gtk.WindowType.Popup)
		{
			SdiWorkspaceWindow w;

			SkipTaskbarHint = true;
			Decorated = false;
			//KeepAbove = true;
			
			//TransientFor = parent;
			TypeHint = WindowTypeHint.Utility;

			VBox mainBox = new VBox ();

			HBox box = new HBox (false, 3);
			if (draggedItem.Icon != null) {
				var img = new Xwt.ImageView (draggedItem.Icon);
				box.PackStart (img.ToGtkWidget (), false, false, 0);
			}
			Gtk.Label la = new Label ();
			la.Markup = draggedItem.Text;
			box.PackStart (la, false, false, 0);

			mainBox.PackStart (box, false, false, 0);

			CustomFrame f = new CustomFrame ();
			f.SetPadding (12, 12, 12, 12);
			f.SetMargins (1, 1, 1, 1);
			f.Add (mainBox);

			Add (f);
		}
	}
}
