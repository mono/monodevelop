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
		Notebook notebook;
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

			notebook = new Notebook ();
			notebook.TabPos = PositionType.Bottom;
			notebook.ShowTabs = false;
			notebook.ShowBorder = false;
			notebook.Show ();
			rootTabsBox.PackStart (notebook, true, true, 1);

			tabstrip = new Tabstrip ();
			//tabstrip.Show ();
			bottomBarBox.PackStart (tabstrip, true, true, 0);

			rootTabsBox.Show ();
		}

		public Widget Widget => rootTabsBox;

		public GtkShellDocumentViewItem ActiveView {
			get {
				if (tabstrip.Tabs.Count > 0)
					return (GtkShellDocumentViewItem) tabstrip.Tabs [tabstrip.ActiveTab].Tag;
				return null;
			}
			set {
				var i = notebook.Children.IndexOf (value);
				if (i != -1) {
					notebook.CurrentPage = i;
					tabstrip.ActiveTab = i;
				}
			}
		}

		public event EventHandler ActiveViewChanged;

		public void InsertView (int position, GtkShellDocumentViewItem view)
		{
			notebook.InsertPage (view, new Gtk.Label (), position);
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
			notebook.CurrentPage = tabstrip.ActiveTab;
			ActiveViewChanged?.Invoke (this, EventArgs.Empty);
		}

		public void RemoveAllViews ()
		{
			while (notebook.NPages > 0) {
				notebook.RemovePage (notebook.NPages - 1);
				tabstrip.RemoveTab (0);
			}
		}

		public void RemoveView (int tabPos)
		{
			notebook.RemovePage (tabPos);
			tabstrip.RemoveTab (tabPos);
		}

		public void ReorderView (int currentIndex, int newIndex)
		{
			var child = (GtkShellDocumentViewItem)notebook.Children [currentIndex];
			notebook.ReorderChild (child, newIndex);
			tabstrip.ReorderTabs (currentIndex, newIndex);
		}

		public void ReplaceView (int position, GtkShellDocumentViewItem view)
		{
			notebook.RemovePage (position);
			notebook.InsertPage (view, new Gtk.Label (), position);
			tabstrip.ReplaceTab (position, CreateTab (view));
		}

		public IEnumerable<GtkShellDocumentViewItem> GetAllViews ()
		{
			return notebook.Children.Cast<GtkShellDocumentViewItem> ();
		}

		public void SetViewTitle (GtkShellDocumentViewItem view, string label, Xwt.Drawing.Image icon, string accessibilityDescription)
		{
			var i = notebook.Children.IndexOf (view);
			if (i != -1) {
				var tab = tabstrip.Tabs [i];
				UpdateTab (tab, label, icon, accessibilityDescription);
			}
		}

		public void AddViews (IEnumerable<GtkShellDocumentViewItem> views)
		{
			foreach (var view in views) {
				notebook.AppendPage (view, new Gtk.Label ());
				tabstrip.AddTab (CreateTab (view));
			}
		}
	}
}
