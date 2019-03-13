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
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Shell
{
	class GtkShellDocumentViewContainer : GtkShellDocumentViewItem, IShellDocumentViewContainer
	{
		DocumentViewContainerMode supportedModes;
		double [] splitSizes;

		Tabstrip tabstrip;
		VBox rootTabsBox;
		HBox bottomBarBox;
		DocumentViewContainerMode currentMode;

		IGtkShellDocumentViewContainer currentContainer;
		GtkShellDocumentViewContainerTabs tabsContainer;
		GtkShellDocumentViewContainerSplit splitContainer;

		public event EventHandler ActiveViewChanged;

		public DocumentViewContainerMode CurrentMode => currentMode;

		public GtkShellDocumentViewContainer ()
		{
			rootTabsBox = new VBox ();
			rootTabsBox.Accessible.SetShouldIgnore (true);

			bottomBarBox = new HBox (false, 0);
			bottomBarBox.Show ();
			rootTabsBox.PackEnd (bottomBarBox, false, false, 0);

			tabstrip = new Tabstrip ();
			tabstrip.Show ();
			bottomBarBox.PackStart (tabstrip, true, true, 0);

			rootTabsBox.Show ();
			Add (rootTabsBox);
		}

		public void SetSupportedModes (DocumentViewContainerMode supportedModes)
		{
			var hadSplit = (this.supportedModes & DocumentViewContainerMode.VerticalSplit) != 0 || (this.supportedModes & DocumentViewContainerMode.HorizontalSplit) != 0;

			this.supportedModes = supportedModes;
			tabstrip.Visible = (supportedModes & DocumentViewContainerMode.Tabs) != 0;

			var hasSplit = (this.supportedModes & DocumentViewContainerMode.VerticalSplit) != 0 || (this.supportedModes & DocumentViewContainerMode.HorizontalSplit) != 0;
			if (hasSplit && !hadSplit) {
				var currentActive = tabstrip.ActiveTab;
				var tab = new Tab (tabstrip, GettextCatalog.GetString ("Split"));
				tabstrip.AddTab (tab);
				tabstrip.ActiveTab = currentActive;
				tab.Activated += TabActivated;
			} else if (!hasSplit && hadSplit)
				tabstrip.RemoveTab (tabstrip.TabCount - 1);
		}

		public void SetCurrentMode (DocumentViewContainerMode mode)
		{
			if (this.currentMode == mode)
				return;

			// Save current split sizes
			if (currentContainer is GtkShellDocumentViewContainerSplit split)
				splitSizes = split.GetRelativeSplitSizes ();

			this.currentMode = mode;

			GtkShellDocumentViewItem activeView = null;
			List<GtkShellDocumentViewItem> allViews = null;

			if (currentContainer != null) {
				activeView = currentContainer.ActiveView;
				currentContainer.ActiveViewChanged -= Container_ActiveViewChanged;
				allViews = currentContainer.GetAllViews ().ToList ();
				currentContainer.Widget.Hide ();
				currentContainer.RemoveAllViews ();
			}

			if (mode == DocumentViewContainerMode.Tabs) {
				if (tabsContainer == null) {
					tabsContainer = new GtkShellDocumentViewContainerTabs ();
					rootTabsBox.PackStart (tabsContainer.Widget, true, true, 1);
				}
				currentContainer = tabsContainer;
			} else {
				if (splitContainer == null) {
					splitContainer = new GtkShellDocumentViewContainerSplit (mode);
					rootTabsBox.PackStart (splitContainer.Widget, true, true, 1);
				}
				currentContainer = splitContainer;
			}

			if (allViews != null)
				currentContainer.AddViews (allViews);

			// Restore current split sizes
			if (splitSizes != null && currentContainer is GtkShellDocumentViewContainerSplit newSplit)
				newSplit.SetRelativeSplitSizes (splitSizes);

			currentContainer.ActiveView = activeView;
			currentContainer.ActiveViewChanged += Container_ActiveViewChanged;
			currentContainer.Widget.Show ();
		}

		void Container_ActiveViewChanged (object sender, EventArgs e)
		{
			ActiveViewChanged?.Invoke (this, EventArgs.Empty);
		}

		protected override async Task OnLoad (CancellationToken cancellationToken)
		{
			if (currentMode == DocumentViewContainerMode.Tabs) {
				var item = (GtkShellDocumentViewItem)currentContainer.ActiveView;
				if (item != null && !item.Loaded)
					await item.Load (cancellationToken);
			} else {
				var allTasks = new List<Task> ();
				foreach (var c in currentContainer.GetAllViews ())
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

		public void InsertView (int position, IShellDocumentViewItem shellView)
		{
			var widget = (GtkShellDocumentViewItem)shellView;
			widget.Show ();
			currentContainer.InsertView (position, widget);
			tabstrip.InsertTab (position, CreateTab ((GtkShellDocumentViewItem)shellView));
		}

		Tab CreateTab (GtkShellDocumentViewItem view)
		{
			var tab = GtkShellDocumentViewContainerTabs.CreateTab (tabstrip, view);
			tab.Activated += TabActivated;
			return tab;
		}

		void TabActivated (object s, EventArgs args)
		{
			if (tabstrip.ActiveTab == tabstrip.TabCount - 1) {
				SetCurrentMode (DocumentViewContainerMode.VerticalSplit);
			} else {
				var tab = (Tab)s;
				SetCurrentMode (DocumentViewContainerMode.Tabs);
				currentContainer.ActiveView = (GtkShellDocumentViewItem)tab.Tag;
			}
		}

		public void ReplaceView (int position, IShellDocumentViewItem shellView)
		{
			var newView = (GtkShellDocumentViewItem)shellView;
			newView.Show ();
			currentContainer.ReplaceView (position, newView);
			tabstrip.RemoveTab (position);
			tabstrip.InsertTab (position, CreateTab (newView));
		}

		public void RemoveView (int tabPos)
		{
			currentContainer.RemoveView (tabPos);
			tabstrip.RemoveTab (tabPos);
		}

		public void ReorderView (int currentIndex, int newIndex)
		{
			currentContainer.ReorderView (currentIndex, newIndex);
			tabstrip.ReorderTabs (currentIndex, newIndex);
		}

		public void RemoveAllViews ()
		{
			currentContainer.RemoveAllViews ();
			while (tabstrip.TabCount > 1)
				tabstrip.RemoveTab (0);
		}

		IEnumerable<GtkShellDocumentViewItem> GetAllViews ()
		{
			return currentContainer.GetAllViews ();
		}

		public IShellDocumentViewItem ActiveView {
			get => currentContainer.ActiveView;
			set {
				currentContainer.ActiveView = (GtkShellDocumentViewItem)value;
				var activeTab = tabstrip.Tabs.FindIndex (t => t.Tag == value);
				tabstrip.ActiveTab = activeTab;
			}
		}

		public double [] GetRelativeSplitSizes ()
		{
			if (splitSizes != null)
				return splitSizes;
			if (currentContainer is GtkShellDocumentViewContainerSplit split)
				return split.GetRelativeSplitSizes ();
			return null;
		}

		public void SetRelativeSplitSizes (double [] sizes)
		{
			if (currentContainer is GtkShellDocumentViewContainerSplit split)
				split.SetRelativeSplitSizes (sizes);
			else
				splitSizes = sizes;
		}

		public void SetViewTitle (GtkShellDocumentViewItem view, string label, Xwt.Drawing.Image icon, string accessibilityDescription)
		{
			currentContainer.SetViewTitle (view, label, icon, accessibilityDescription);
			for (int n = 0; n < tabstrip.TabCount; n++) {
				var tab = tabstrip.Tabs [n];
				if (tab.Tag == view) {
					GtkShellDocumentViewContainerTabs.UpdateTab (tab, label, icon, accessibilityDescription);
					break;
				}
			}
		}
	}

	interface IGtkShellDocumentViewContainer
	{
		Gtk.Widget Widget { get; }
		void AddViews (IEnumerable<GtkShellDocumentViewItem> views);
		void InsertView (int position, GtkShellDocumentViewItem view);
		void ReplaceView (int position, GtkShellDocumentViewItem view);
		void RemoveView (int tabPos);
		void ReorderView (int currentIndex, int newIndex);
		void RemoveAllViews ();
		GtkShellDocumentViewItem ActiveView { get; set; }
		event EventHandler ActiveViewChanged;
		IEnumerable<GtkShellDocumentViewItem> GetAllViews ();
		void SetViewTitle (GtkShellDocumentViewItem view, string label, Xwt.Drawing.Image icon, string accessibilityDescription);
	}
}
