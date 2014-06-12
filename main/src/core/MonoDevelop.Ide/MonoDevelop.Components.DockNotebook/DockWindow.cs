//
// DockWindow.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

		protected override void OnRealized ()
		{
			base.OnRealized ();
			// A small delay to make sure the window is fully rendered before showing it
			GdkWindow.Opacity = 0;
			GLib.Timeout.Add (120, delegate {
				GdkWindow.Opacity = 1;
				return false;
			});
		}
	}
	
}
