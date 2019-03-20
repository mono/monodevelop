//  SdiWorkspaceWindow.cs
//
// Author:
//   Mike Krüger
//   Lluis Sanchez Gual
//
//  This file was derived from a file from #Develop 2.0
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
//  Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// 
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
// 
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.DockNotebook;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.Ide.Gui.Shell
{
	internal class SdiWorkspaceWindow : EventBox, IWorkbenchWindow, ICommandDelegatorRouter
	{
		DefaultWorkbench workbench;
		DocumentController controller;
		GtkShellDocumentViewItem view;
		DockNotebookTab tab;
		DockNotebook tabControl;

		string myUntitledTitle;
		string titleHolder = "";

		bool showNotification;

		public event EventHandler CloseRequested;
		public event EventHandler<NotebookChangeEventArgs> NotebookChanged;
		public event EventHandler TitleChanged;

		public DockNotebook TabControl {
			get {
				return this.tabControl;
			}
		}

		public DocumentController DocumentController {
			get {
				return controller;
			}
		}

		public DockNotebookTab DockNotebookTab {
			get {
				return tab;
			}
		}

		public SdiWorkspaceWindow (DefaultWorkbench workbench, DocumentController controller, DockNotebook tabControl, DockNotebookTab tabLabel) : base ()
		{
			this.workbench = workbench;
			this.tabControl = tabControl;
			this.controller = controller;
			this.tab = tabLabel;

			// The previous WorkbenchWindow property assignement may end with a call to AttachViewContent,
			// which will add the content control to the subview notebook. In that case, we don't need to add it to box
			controller.DocumentTitleChanged += SetTitleEvent;
			controller.HasUnsavedChangesChanged += HandleDirtyChanged;

			SetTitleEvent ();
		}

		public void SetRootView (IShellDocumentViewItem view)
		{
			this.view = (GtkShellDocumentViewItem) view;
			if (Child != null)
				Remove (Child);
			if (view != null) {
				var widget = (Gtk.Widget)view;
				widget.Show ();
				Add (widget);
			}
		}

		public IShellDocumentViewContent CreateViewContent ()
		{
			return new GtkShellDocumentViewContent ();
		}

		public IShellDocumentViewContainer CreateViewContainer ()
		{
			return new GtkShellDocumentViewContainer ();
		}

		internal void SetDockNotebook (DockNotebook tabControl, DockNotebookTab tabLabel)
		{
			var oldNotebook = tabControl;
			this.tabControl = tabControl;
			this.tab = tabLabel;
			SetTitleEvent ();
			SetDockNotebookTabTitle ();

			if (oldNotebook != tabControl)
				NotebookChanged?.Invoke (this, new NotebookChangeEventArgs { OldNotebook = oldNotebook, NewNotebook = tabControl });
		}

		void HandleDirtyChanged (object sender, EventArgs e)
		{
			OnTitleChanged (null);
		}

		Document document;
		public Document Document {
			get {
				return document;
			}
			set {
				document = value;
			}
		}
		
		protected override bool OnWidgetEvent (Gdk.Event evt)
		{
			if (evt.Type == Gdk.EventType.ButtonRelease)
				DockNotebook.ActiveNotebook = (SdiDragNotebook)Parent.Parent;

			return base.OnWidgetEvent (evt);
		}

		public bool ShowNotification {
			get {
				return showNotification;
			}
			set {
				if (showNotification != value) {
					showNotification = value;
					OnTitleChanged (null);
				}
			}
		}

		public string Title {
			get {
				return titleHolder;
			}
			set {
				titleHolder = value;
				OnTitleChanged (null);
			}
		}

		public void OnDeactivated ()
		{
		}

		public void OnActivated ()
		{
		}

		public void SelectWindow ()
		{
			var window = tabControl.Toplevel as Gtk.Window;
			if (window != null) {
				if (window is DockWindow) {
					IdeServices.DesktopService.GrabDesktopFocus (window);
				}

				// Focusing the main window so hide all the autohide pads
				workbench.DockFrame.MinimizeAllAutohidden ();

#if MAC
				AppKit.NSWindow nswindow = MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow (window);
				if (nswindow != null) {
					nswindow.MakeFirstResponder (nswindow.ContentView);
					nswindow.MakeKeyAndOrderFront (nswindow);
				}
#endif
			}

			// The tab change must be done now to ensure that the content is created
			// before exiting this method.
			tabControl.CurrentTabIndex = tab.Index;

			// Focus the tab in the next iteration since presenting the window may take some time
			Application.Invoke (async (o, args) => {
				DockNotebook.ActiveNotebook = tabControl;
				await view.Load ();
				DeepGrabFocus (view);
			});
		}

		public bool ContentVisible => TabControl.CurrentTab.Content == this;

		public bool CanMoveToNextNotebook ()
		{
			return TabControl.GetNextNotebook () != null || (TabControl.Container.AllowRightInsert && TabControl.TabCount > 1);
		}

		public bool CanMoveToPreviousNotebook ()
		{
			return TabControl.GetPreviousNotebook () != null || (TabControl.Container.AllowLeftInsert && TabControl.TabCount > 1);
		}

		public void MoveToNextNotebook ()
		{
			var nextNotebook = TabControl.GetNextNotebook ();
			if (nextNotebook == null) {
				if (TabControl.Container.AllowRightInsert) {
					TabControl.RemoveTab (tab.Index, true);
					TabControl.Container.InsertRight (this);
					SelectWindow ();
				}
				return;
			}

			TabControl.RemoveTab (tab.Index, true);
			var newTab = nextNotebook.AddTab ();
			newTab.Content = this;
			SetDockNotebook (nextNotebook, newTab);
			SelectWindow ();
		}

		public void MoveToPreviousNotebook ()
		{
			var nextNotebook = TabControl.GetPreviousNotebook ();
			if (nextNotebook == null) {
				if (TabControl.Container.AllowLeftInsert) {
					TabControl.RemoveTab (tab.Index, true);
					TabControl.Container.InsertLeft (this);
					SelectWindow ();
				}
				return;
			}

			TabControl.RemoveTab (tab.Index, true);
			var newTab = nextNotebook.AddTab ();
			newTab.Content = this;
			SetDockNotebook (nextNotebook, newTab);
			SelectWindow ();
		}

		static void DeepGrabFocus (Gtk.Widget widget)
		{
			Widget first = null;

			foreach (var f in GetFocusableWidgets (widget)) {
				if (f.HasFocus)
					return;

				if (first == null)
					first = f;
			}
			if (first != null) {
				first.GrabFocus ();
			}
		}

		static IEnumerable<Gtk.Widget> GetFocusableWidgets (Gtk.Widget widget)
		{
			if (widget.CanFocus) {
				yield return widget;
			}

			if (widget is Container c) {
				foreach (var f in c.FocusChain.SelectMany (x => GetFocusableWidgets (x))) {
					yield return f;
				}

				if (c.Children is var children) {
					foreach (var f in children) {
						if (f is Container container) {
							foreach (var child in GetFocusableChildren (container)) {
								yield return child;
							}
						}
					}
				}
			}
		}

		static IEnumerable<Gtk.Widget> GetFocusableChildren (Gtk.Container container)
		{
			if (container.CanFocus) {
				yield return container;
			}

			foreach (var f in container.Children) {
				if (f is Container c) {
					foreach (var child in GetFocusableChildren (c)) {
						yield return child;
					}
				}
			}
		}

		public object ViewCommandHandler { get; set; }

		IShellNotebook IWorkbenchWindow.Notebook => tabControl;

		public void SetTitleEvent (object sender, EventArgs e)
		{
			SetTitleEvent ();
		}

		void SetTitleEvent ()
		{
			if (controller == null)
				return;

			string newTitle;
			if (controller.IsNewDocument && controller is FileDocumentController fileController) {
				if (myUntitledTitle == null)
					myUntitledTitle = workbench.GetUniqueTabName (fileController.FilePath);
				newTitle = myUntitledTitle;
			} else
				newTitle = controller.DocumentTitle;

			if (newTitle != Title)
				Title = newTitle;
		}

		public void RequestClose ()
		{
			CloseRequested?.Invoke (this, EventArgs.Empty);
		}

		public void Close ()
		{
			view.DetachFromView ();
			Destroy ();
		}

		protected override void OnDestroyed ()
		{
			controller.DocumentTitleChanged -= SetTitleEvent;
			controller.HasUnsavedChangesChanged -= HandleDirtyChanged;

			document = null;
			base.OnDestroyed ();
		}

		object ICommandDelegatorRouter.GetNextCommandTarget ()
		{
			return Parent;
		}
		
		object ICommandDelegatorRouter.GetDelegatedCommandTarget ()
		{
			// If command checks are flowing through this view, it means the view's notebook
			// is the active notebook.
			if (!(Toplevel is Gtk.Window))
				return null;

			if (((Gtk.Window)Toplevel).HasToplevelFocus)
				DockNotebook.ActiveNotebook = (SdiDragNotebook)Parent.Parent;

			return null;
		}

		void SetDockNotebookTabTitle ()
		{
			tab.Text = Title;
			tab.Notify = showNotification;
			tab.Dirty = controller.HasUnsavedChanges;
			if (!string.IsNullOrEmpty (controller.DocumentTitle)) {
				tab.Tooltip = controller.DocumentTitle;
			}
			tab.Icon = controller.DocumentIcon?.WithSize (IconSize.Menu);
		}
		
		void OnTitleChanged(EventArgs e)
		{
			SetDockNotebookTabTitle ();
			if (TitleChanged != null) {
				TitleChanged(this, e);
			}
		}
	}
}
