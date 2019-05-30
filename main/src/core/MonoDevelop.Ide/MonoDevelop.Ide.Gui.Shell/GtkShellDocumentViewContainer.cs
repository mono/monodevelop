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
		bool hasSplit;

		IGtkShellDocumentViewContainer currentContainer;
		GtkShellDocumentViewContainerTabs tabsContainer;
		GtkShellDocumentViewContainerSplit splitContainer;

		public event EventHandler ActiveViewChanged;
		public event EventHandler CurrentModeChanged;

		public GtkShellDocumentViewContainer ()
		{
			rootTabsBox = new VBox ();
			rootTabsBox.Accessible.SetShouldIgnore (true);

			bottomBarBox = new HBox (false, 0);
			bottomBarBox.Show ();
			rootTabsBox.PackEnd (bottomBarBox, false, false, 0);

			tabstrip = new Tabstrip ();
			bottomBarBox.PackStart (tabstrip, true, true, 0);

			rootTabsBox.Show ();
			Add (rootTabsBox);
		}

		public void SetSupportedModes (DocumentViewContainerMode supportedModes)
		{
			this.supportedModes = supportedModes;
			UpdateTabstrip ();
		}

		void UpdateTabstrip ()
		{
			var hadSplit = hasSplit;

			var hasMultipleViews = currentContainer != null && currentContainer.GetAllViews ().Skip (1).Any ();
			tabstrip.Visible = hasMultipleViews && (supportedModes & DocumentViewContainerMode.Tabs) != 0;

			hasSplit = (this.supportedModes & DocumentViewContainerMode.VerticalSplit) != 0 || (this.supportedModes & DocumentViewContainerMode.HorizontalSplit) != 0;
			if (hasSplit && !hadSplit) {
				var currentActive = tabstrip.ActiveTab;
				var tab = new Tab (tabstrip, GettextCatalog.GetString ("Split"));
				tabstrip.AddTab (tab);
				tabstrip.ActiveTab = currentActive;
				tab.Activated += TabActivated;
			} else if (!hasSplit && hadSplit)
				tabstrip.RemoveTab (tabstrip.TabCount - 1);

			// If this container is showing tabs and it is inside another container, give the parent the
			// chance to show the tabstrip in its own tab area, to avoid tab stacking
			ParentContainer?.UpdateAttachedTabstrips ();

			// This might be a container that has children with tabs. Maybe now it is possible to embed the
			// child tabstrips into this container's tab area. Let's try!
			UpdateAttachedTabstrips ();
		}

		public DocumentViewContainerMode CurrentMode {
			get { return currentMode; }
			set { SetCurrentMode (value, null); }
		}

		public void SetCurrentMode (DocumentViewContainerMode mode, GtkShellDocumentViewItem newActive)
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
					rootTabsBox.PackStart (tabsContainer.Widget, true, true, 0);
				}
				currentContainer = tabsContainer;
			} else {
				if (splitContainer == null) {
					splitContainer = new GtkShellDocumentViewContainerSplit (mode);
					rootTabsBox.PackStart (splitContainer.Widget, true, true, 0);
				}
				currentContainer = splitContainer;
				if (hasSplit)
					tabstrip.ActiveTab = tabstrip.TabCount - 1;
			}

			if (allViews != null)
				currentContainer.AddViews (allViews);

			// Restore current split sizes
			if (splitSizes != null && currentContainer is GtkShellDocumentViewContainerSplit newSplit)
				newSplit.SetRelativeSplitSizes (splitSizes);

			currentContainer.ActiveView = newActive ?? activeView;
			currentContainer.ActiveViewChanged += Container_ActiveViewChanged;
			currentContainer.Widget.Show ();

			if (newActive != activeView)
				ActiveViewChanged?.Invoke (this, EventArgs.Empty);

			CurrentModeChanged?.Invoke (this, EventArgs.Empty);
		}

		List<GtkShellDocumentViewContainer> attachedChildTabstrips = new List<GtkShellDocumentViewContainer> ();

		void UpdateAttachedTabstrips ()
		{
			// If we are showing tabs and there is any child which also has tabs, this method will embed the child tabstrip into
			// into this container's tab area, to avoid tab stacking. If any of the conditions that make the tabstrip embeddable
			// has changed and it can't be embedded anymore, it will remove it from the tab area and return it back to
			// the child.

			// We do child tab embedding only for the root container, and only if we are already showing tabs

			if (!tabstrip.Visible || ParentContainer != null) {
				// The container doesn't support embedding child tabs, remove all that are currently embedded, if any
				foreach (var child in attachedChildTabstrips)
					DetachTabstrip (child);
				attachedChildTabstrips.Clear ();
				return;
			}

			// Get a list of children with visible tabstrips. Notice that if any of the child tabstrips is hidden, then this
			// method will be called and the tabstrip will be removed

			var childrenWithTabs = GetAllViews ().OfType<GtkShellDocumentViewContainer> ().Where (c => c.tabstrip.Visible).ToList ();
			for (int n=0; n<childrenWithTabs.Count; n++) {
				var child = childrenWithTabs [n];
				var alreadyAttached = attachedChildTabstrips.Contains (child);
				if (!alreadyAttached) {
					// The child's tabstrip is embeddable, but it is not yet embedded
					child.OnAttachingTabstripToParent ();
					bottomBarBox.PackStart (child.tabstrip, false, false, 0);
					attachedChildTabstrips.Add (child);

					// Hide the tab that corresponds to this child, since we are now showing the children's tabstrip
					// directly in the parent tab area, so to switch to the child is just a matter of clicking in
					// one of its tabs
					var localTab = tabstrip.Tabs.FirstOrDefault (t => t.Tag == child);
					if (localTab != null)
						localTab.Visible = false;
				}
				var boxChild = (Box.BoxChild)bottomBarBox [child.tabstrip];
				boxChild.Position = n;
			}

			// Now remove embedded tabstrips which are not embeddable anymore

			for (int n=0; n < attachedChildTabstrips.Count; n++) {
				var c = attachedChildTabstrips [n];
				if (!childrenWithTabs.Contains (c)) {
					DetachTabstrip (c);
					attachedChildTabstrips.RemoveAt (n--);
				}
			}
		}

		void DetachTabstrip (GtkShellDocumentViewContainer item)
		{
			bottomBarBox.Remove (item.tabstrip);
			item.OnDetachedTabstripFromParent ();

			// Make the tab corresponding to the item visible again, since we are removing its tabstrip from our tab area
			var localTab = tabstrip.Tabs.FirstOrDefault (t => t.Tag == item);
			if (localTab != null)
				localTab.Visible = true;
		}

		void OnAttachingTabstripToParent ()
		{
			// This is called when the tabstrip of this container is embedded in the parent's tab area
			// We need to remove it from its own tab area and add a separator
			bottomBarBox.Remove (tabstrip);
			tabstrip.AddTab (new Tab (tabstrip, "|"));
		}

		void OnDetachedTabstripFromParent ()
		{
			// The tabstrip is not embeddable anymore, so it is returning to the child.
			// Add it back to its own tab area and remove the separator
			bottomBarBox.PackStart (tabstrip, true, true, 0);
			tabstrip.RemoveTab (tabstrip.Tabs.Count - 1);
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
			widget.ParentContainer = this;
			widget.Show ();
			currentContainer.InsertView (position, widget);
			tabstrip.InsertTab (position, CreateTab ((GtkShellDocumentViewItem)shellView));
			UpdateTabstrip ();
		}

		Tab CreateTab (GtkShellDocumentViewItem view)
		{
			var tab = GtkShellDocumentViewContainerTabs.CreateTab (tabstrip, view);
			tab.Activated += TabActivated;
			return tab;
		}

		void TabActivated (object s, EventArgs args)
		{
			var tab = (Tab)s;
			if (hasSplit && tab.Tag == null) {
				// If Tag is null it means it's clicking on the "Split" tab
				CurrentMode = DocumentViewContainerMode.VerticalSplit;
			} else {
				SetCurrentMode (DocumentViewContainerMode.Tabs, (GtkShellDocumentViewItem)tab.Tag);
				ShowChildView ((GtkShellDocumentViewItem)tab.Tag);
			}
			// Make this container is visible on its parent
			ParentContainer?.ShowChildView (this);
		}

		void ShowChildView (GtkShellDocumentViewItem view)
		{
			if (currentContainer.ActiveView != view) {
				currentContainer.ActiveView = view;

				// If this container has embedded tabstrips, reset the active tab in all of them except
				// the one that was clicked. Otherwise several active tabs would be visible, one for
				// each tabstrip

				if (attachedChildTabstrips.Contains (view))
					tabstrip.ActiveTab = -1;

				foreach (var c in attachedChildTabstrips)
					if (c != view)
						c.tabstrip.ActiveTab = -1;
			}
		}

		public void ReplaceView (int position, IShellDocumentViewItem shellView)
		{
			var oldView = currentContainer.GetChild (position);
			var newView = (GtkShellDocumentViewItem)shellView;
			newView.ParentContainer = this;
			newView.Show ();
			currentContainer.ReplaceView (position, newView);
			tabstrip.ReplaceTab (position, CreateTab (newView));
			UpdateTabstrip ();
			oldView.ParentContainer = null;
		}

		public void RemoveView (int tabPos)
		{
			var oldView = currentContainer.GetChild (tabPos);
			currentContainer.RemoveView (tabPos);
			tabstrip.RemoveTab (tabPos);
			UpdateTabstrip ();
			oldView.ParentContainer = null;
		}

		public void ReorderView (int currentIndex, int newIndex)
		{
			currentContainer.ReorderView (currentIndex, newIndex);
			tabstrip.ReorderTabs (currentIndex, newIndex);
		}

		public void RemoveAllViews ()
		{
			var oldViews = currentContainer.GetAllViews ().ToList ();
			currentContainer.RemoveAllViews ();
			while (tabstrip.TabCount > 1)
				tabstrip.RemoveTab (0);
			UpdateTabstrip ();
			foreach (var c in oldViews)
				c.ParentContainer = null;
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
				if (currentMode == DocumentViewContainerMode.Tabs)
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
		GtkShellDocumentViewItem GetChild (int index);
		void SetViewTitle (GtkShellDocumentViewItem view, string label, Xwt.Drawing.Image icon, string accessibilityDescription);
	}
}
