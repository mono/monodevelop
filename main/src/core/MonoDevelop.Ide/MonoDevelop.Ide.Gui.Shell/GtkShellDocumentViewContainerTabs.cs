//
// GtkShellDocumentViewContainerTabs.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.Generic;
using System.Linq;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.Gui.Shell
{
	class GtkShellDocumentViewContainerTabs: IGtkShellDocumentViewContainer
	{
		EventBox notebook;
		Tabstrip tabstrip;
		VBox rootTabsBox;
		HBox bottomBarBox;

		public GtkShellDocumentViewContainerTabs ()
		{
			rootTabsBox = new VBox ();
			rootTabsBox.Accessible.SetShouldIgnore (true);

			bottomBarBox = new HBox (false, 0);
			bottomBarBox.Show ();
			rootTabsBox.PackEnd (bottomBarBox, false, false, 0);

			notebook = new EventBox ();
			notebook.Show ();
			rootTabsBox.PackStart (notebook, true, true, 0);

			tabstrip = new Tabstrip ();
			//tabstrip.Show ();
			bottomBarBox.PackStart (tabstrip, true, true, 0);

			rootTabsBox.Show ();
		}

		public Widget Widget => rootTabsBox;

		public GtkShellDocumentViewItem ActiveView {
			get {
				if (tabstrip.Tabs.Count > 0 && tabstrip.ActiveTab != -1)
					return (GtkShellDocumentViewItem) tabstrip.Tabs [tabstrip.ActiveTab].Tag;
				return null;
			}
			set {
				var i = tabstrip.Tabs.FindIndex (t => t.Tag == value);
				if (i != -1) {
					tabstrip.ActiveTab = i;
					ShowActiveContent ();
				}
			}
		}

		public event EventHandler ActiveViewChanged;

		public void InsertView (int position, GtkShellDocumentViewItem view)
		{
			tabstrip.InsertTab (position, CreateTab (view));
		}

		Tab CreateTab (GtkShellDocumentViewItem view)
		{
			var tab = CreateTab (tabstrip, view);
			tab.Activated += TabActivated;
			return tab;
		}

		internal static Tab CreateTab (Tabstrip tabstrip, GtkShellDocumentViewItem view)
		{
			var tab = new Tab (tabstrip, view.Title) { Tag = view };
			if (tab.Accessible != null)
				tab.Accessible.Help = view.AccessibilityDescription;
			return tab;
		}

		internal static void UpdateTab (Tab tab, string label, Xwt.Drawing.Image icon, string accessibilityDescription)
		{
			tab.Label = label;
			if (tab.Accessible != null) {
				tab.Accessible.Help = accessibilityDescription;
				tab.Accessible.Label = label ?? "";
			}
		}

		void TabActivated (object s, EventArgs args)
		{
			ShowActiveContent ();
			ActiveViewChanged?.Invoke (this, EventArgs.Empty);
		}

		public void RemoveAllViews ()
		{
			while (tabstrip.Tabs.Count > 0)
				tabstrip.RemoveTab (0);
			ShowActiveContent ();
		}

		public void RemoveView (int tabPos)
		{
			tabstrip.RemoveTab (tabPos);
		}

		public void ReorderView (int currentIndex, int newIndex)
		{
			var child = (GtkShellDocumentViewItem)notebook.Children [currentIndex];
			tabstrip.ReorderTabs (currentIndex, newIndex);
			ShowActiveContent ();
		}

		void ShowActiveContent ()
		{
			if (tabstrip.ActiveTab != -1) {
				var newChild = (GtkShellDocumentViewItem)tabstrip.Tabs [tabstrip.ActiveTab].Tag;
				if (newChild != notebook.Child) {
					if (notebook.Child != null)
						notebook.Remove (notebook.Child);
					notebook.Add (newChild);
				}
			} else if (notebook.Child != null)
				notebook.Remove (notebook.Child);
		}

		public void ReplaceView (int position, GtkShellDocumentViewItem view)
		{
			tabstrip.ReplaceTab (position, CreateTab (view));
			ShowActiveContent ();
		}

		public IEnumerable<GtkShellDocumentViewItem> GetAllViews ()
		{
			return tabstrip.Tabs.Select (t => t.Tag).Cast<GtkShellDocumentViewItem> ();
		}

		public GtkShellDocumentViewItem GetChild (int index)
		{
			return (GtkShellDocumentViewItem)tabstrip.Tabs [index].Tag;
		}

		public void SetViewTitle (GtkShellDocumentViewItem view, string label, Xwt.Drawing.Image icon, string accessibilityDescription)
		{
			var tab = tabstrip.Tabs.FirstOrDefault (t => t.Tag == view);
			if (tab != null) {
				UpdateTab (tab, label, icon, accessibilityDescription);
			}
		}

		public void AddViews (IEnumerable<GtkShellDocumentViewItem> views)
		{
			foreach (var view in views)
				tabstrip.AddTab (CreateTab (view));
			ShowActiveContent ();
		}
	}
}
