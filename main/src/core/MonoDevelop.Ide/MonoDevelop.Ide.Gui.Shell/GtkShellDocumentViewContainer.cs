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

using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Shell
{
	class GtkShellDocumentViewContainer : GtkShellDocumentViewItem, IShellDocumentViewContainer
	{
		DocumentViewContainerMode supportedModes;
		DocumentViewContainerMode mode;

		Notebook notebook;
		Tabstrip tabstrip;
		VBox rootTabsBox;
		HBox bottomBarBox;

		GtkMultiPaned paned;

		public event EventHandler ActiveViewChanged;

		void SetupContainer ()
		{
		}

		void SetNotebookMode (IEnumerable<GtkShellDocumentViewItem> items)
		{
			if (notebook != null) {
				return;
			}

			rootTabsBox = new VBox ();
			rootTabsBox.Accessible.SetShouldIgnore (true);

			bottomBarBox = new HBox (false, 0);
			bottomBarBox.Show ();
			rootTabsBox.PackEnd (bottomBarBox, false, false, 0);

			notebook = new Notebook ();
			notebook.TabPos = PositionType.Bottom;
			notebook.ShowTabs = false;
			notebook.ShowBorder = false;
			notebook.Show ();
			rootTabsBox.PackStart (notebook, true, true, 1);

			tabstrip = new Tabstrip ();
			tabstrip.Show ();
			bottomBarBox.PackStart (tabstrip, true, true, 0);

			rootTabsBox.Show ();
			Add (rootTabsBox);
		}

		void SetSplitMode (IEnumerable<GtkShellDocumentViewItem> items)
		{
			if (paned != null)
				return;
			paned = new GtkMultiPaned (mode);
			paned.AddRange (items);
			Add (paned.Paned);
		}

		public void SetSupportedModes (DocumentViewContainerMode supportedModes)
		{
			this.supportedModes = supportedModes;
		}

		protected override async Task OnLoad (CancellationToken cancellationToken)
		{
			if (mode == DocumentViewContainerMode.Tabs) {
				var item = (GtkShellDocumentViewItem)notebook.CurrentPageWidget;
				if (item != null && !item.Loaded)
					await item.Load (cancellationToken);
			} else {
				var allTasks = new List<Task> ();
				foreach (var c in paned.Children.OfType<GtkShellDocumentViewItem> ())
					allTasks.Add (c.Load (cancellationToken));
				await Task.WhenAll (allTasks);
			}
		}

		public override void DetachFromView ()
		{
			foreach (var child in GetAllViews ())
				child.DetachFromView ();
			base.DetachFromView ();
		}

		public IShellDocumentViewItem InsertView (int position, DocumentView view)
		{
			var workbenchView = CreateShellView (view);

			if (mode == DocumentViewContainerMode.Tabs) {
				var tab = new Tab (tabstrip, view.Title) { Tag = workbenchView };
				if (tab.Accessible != null)
					tab.Accessible.Help = view.AccessibilityDescription;
				tab.Activated += TabActivated;
				notebook.InsertPage (workbenchView, new Gtk.Label (), position);
				tabstrip.InsertTab (position, tab);
			} else {
				paned.InsertView (position, workbenchView);
			}
			return workbenchView;
		}

		void TabActivated (object s, EventArgs args)
		{
			var tab = (Tab)s;
			SelectView ((IShellDocumentViewItem) tab.Tag);
		}

		public IShellDocumentViewItem ReplaceView (int position, DocumentView view)
		{
			var replacedView = GetViewAt (position);
			IShellDocumentViewItem newView;

			if (mode == DocumentViewContainerMode.Tabs) {
				notebook.RemovePage (position);
				newView = InsertView (position, view);
			} else {
				var workbenchView = CreateShellView (view);
				paned.ReplaceView (position, workbenchView);
				newView = workbenchView;
			}
			replacedView.DetachFromView ();
			replacedView.Destroy ();
			return newView;
		}

		public void RemoveView (int tabPos)
		{
			var removed = GetViewAt (tabPos);

			if (mode == DocumentViewContainerMode.Tabs)
				notebook.RemovePage (tabPos);
			else
				paned.RemoveView (tabPos);

			removed.DetachFromView ();
			removed.Destroy ();
		}

		public void ReorderView (int currentIndex, int newIndex)
		{
			if (mode == DocumentViewContainerMode.Tabs) {
				var child = (GtkShellDocumentViewItem)notebook.Children [currentIndex];
				notebook.ReorderChild (child, newIndex);
			} else {
				paned.ReorderView (currentIndex, newIndex);
			}
		}

		public void RemoveAllViews ()
		{
			var allViews = GetAllViews ().ToList ();

			if (mode == DocumentViewContainerMode.Tabs) {
				while (notebook.NPages > 0)
					notebook.RemovePage (notebook.NPages - 1);
			} else {
				paned.RemoveAllViews ();
			}
			foreach (var child in allViews) {
				child.DetachFromView ();
				child.Destroy ();
			}
		}

		public void SelectView (IShellDocumentViewItem view)
		{
			if (mode == DocumentViewContainerMode.Tabs) {
				var child = (GtkShellDocumentViewItem)view;
				if (notebook != null) {
					var i = notebook.Children.IndexOf (child);
					if (i != -1) {
						notebook.CurrentPage = i;
						tabstrip.ActiveTab = i;
						if (Loaded && !child.Loaded)
							child.Load ();
					}
				}
			}
		}

		IEnumerable<GtkShellDocumentViewItem> GetAllViews ()
		{
			if (notebook != null)
				return notebook.Children.Cast<GtkShellDocumentViewItem> ();
			else
				return paned.Children.Cast<GtkShellDocumentViewItem> ();
		}

		public GtkShellDocumentViewItem GetViewAt (int position)
		{
			if (mode == DocumentViewContainerMode.Tabs) {
				return (GtkShellDocumentViewItem)notebook.GetNthPage (position);
			} else {
				return (GtkShellDocumentViewItem)paned.Children [position];
			}
		}

		public IShellDocumentViewItem ActiveView { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }
	}
}
