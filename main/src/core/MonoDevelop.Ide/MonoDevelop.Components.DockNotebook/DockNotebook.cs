// DragNotebook.cs
//
// Author:
//   Todd Berman  <tberman@off.net>
//
// Copyright (c) 2004 Todd Berman
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

using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.DockNotebook
{
	delegate void TabsReorderedHandler (Widget widget, int oldPlacement, int newPlacement);

	class DockNotebook : Gtk.VBox
	{
		List<IDockNotebookTab> pages = new List<IDockNotebookTab> ();
		List<IDockNotebookTab> pagesHistory = new List<IDockNotebookTab> ();
		TabStrip tabStrip;
		Gtk.EventBox contentBox;
		ReadOnlyCollection<IDockNotebookTab> pagesCol;

		IDockNotebookTab currentTab;

		public DockNotebook ()
		{
			pagesCol = new ReadOnlyCollection<IDockNotebookTab> (pages);
			AddEvents ((Int32)(EventMask.AllEventsMask));

			tabStrip = new TabStrip (this);

			PackStart (tabStrip, false, false, 0);

			contentBox = new EventBox ();
			PackStart (contentBox, true, true, 0);

			ShowAll ();

			tabStrip.DropDownButton.Sensitive = false;

			tabStrip.DropDownButton.MenuCreator = delegate {
				Gtk.Menu menu = new Menu ();
				foreach (var tab in pages) {
					var mi = new Gtk.ImageMenuItem ("");
					menu.Insert (mi, -1);
					var label = (Gtk.AccelLabel) mi.Child;
					if (tab.Markup != null)
						label.Markup = tab.Markup;
					else
						label.Text = tab.Text;
					var locTab = tab;
					mi.Activated += delegate {
						CurrentTab = locTab;
					};
				}
				menu.ShowAll ();
				return menu;
			};
		}
		
		Cursor fleurCursor = new Cursor (CursorType.Fleur);

		public event TabsReorderedHandler TabsReordered;
		public event EventHandler<TabEventArgs> TabClosed;
		public event EventHandler<TabEventArgs> TabActivated;

		public event EventHandler PageAdded;
		public event EventHandler PageRemoved;
		public event EventHandler SwitchPage;

		public event EventHandler PreviousButtonClicked {
			add { tabStrip.PreviousButton.Clicked += value; }
			remove { tabStrip.PreviousButton.Clicked -= value; }
		}

		public event EventHandler NextButtonClicked {
			add { tabStrip.NextButton.Clicked += value; }
			remove { tabStrip.NextButton.Clicked -= value; }
		}

		public bool PreviousButtonEnabled {
			get { return tabStrip.PreviousButton.Sensitive; }
			set { tabStrip.PreviousButton.Sensitive = value; }
		}

		public bool NextButtonEnabled {
			get { return tabStrip.NextButton.Sensitive; }
			set { tabStrip.NextButton.Sensitive = value; }
		}

		public bool NavigationButtonsVisible {
			get { return tabStrip.NavigationButtonsVisible; }
			set { tabStrip.NavigationButtonsVisible = value; }
		}

		public ReadOnlyCollection<IDockNotebookTab> Tabs {
			get { return pagesCol; }
		}

		public IDockNotebookTab CurrentTab {
			get { return currentTab; }
			set {
				if (currentTab != value) {
					currentTab = value;
					if (contentBox.Child != null)
						contentBox.Remove (contentBox.Child);

					if (currentTab != null) {
						if (currentTab.Content != null) {
							contentBox.Add (currentTab.Content);
							contentBox.ChildFocus (DirectionType.Down);
						}
						pagesHistory.Remove (currentTab);
						pagesHistory.Insert (0, currentTab);
					}

					tabStrip.Update ();

					if (SwitchPage != null)
						SwitchPage (this, EventArgs.Empty);
				}
			}
		}

		public int CurrentTabIndex {
			get { return currentTab != null ? currentTab.Index : -1; }
			set { 
				if (value > pages.Count - 1)
					CurrentTab = null;
				else
					CurrentTab = pages [value]; 
			}
		}

		public int TabCount {
			get { return pages.Count; }
		}

		public int BarHeight {
			get { return tabStrip.BarHeight; }
		}

		internal void InitSize ()
		{
			tabStrip.InitSize ();
		}

		public Action<DockNotebook, int,Gdk.EventButton> DoPopupMenu { get; set; }

		public IDockNotebookTab InsertTab (int index)
		{
			var tab = new DockNotebookTab (this, tabStrip);
			if (index == -1) {
				pages.Add (tab);
				tab.Index = pages.Count - 1;
			} else {
				pages.Insert (index, tab);
				tab.Index = index;
				UpdateIndexes (index + 1);
			}

			pagesHistory.Add (tab);

			if (pages.Count == 1)
				CurrentTab = tab;

			tabStrip.StartOpenAnimation ((DockNotebookTab)tab);
			tabStrip.Update ();
			tabStrip.DropDownButton.Sensitive = pages.Count > 0;

			if (PageAdded != null)
				PageAdded (this, EventArgs.Empty);

			return tab;
		}

		void UpdateIndexes (int startIndex)
		{
			for (int n=startIndex; n < pages.Count; n++)
				((DockNotebookTab)pages [n]).Index = n;
		}

		public IDockNotebookTab GetTab (int n)
		{
			if (n < 0 || n >= pages.Count)
				return null;
			else
				return pages [n];
		}

		public void RemoveTab (int page, bool animate)
		{
			var tab = pages [page];
			if (animate)
				tabStrip.StartCloseAnimation ((DockNotebookTab)tab);
			if (page == CurrentTabIndex) {
				if (page < pages.Count - 1)
					CurrentTabIndex = page + 1;
				else if (page > 0)
					CurrentTabIndex = page - 1;
				else
					CurrentTab = null;
			}
			pages.RemoveAt (page);
			UpdateIndexes (page);
			pagesHistory.Remove (tab);
			tabStrip.Update ();
			tabStrip.DropDownButton.Sensitive = pages.Count > 0;

			if (PageRemoved != null)
				PageRemoved (this, EventArgs.Empty);
		}

		internal void ReorderTab (DockNotebookTab tab, DockNotebookTab targetTab)
		{
			if (tab == targetTab)
				return;
			int targetPos = targetTab.Index;
			if (tab.Index > targetTab.Index) {
				pages.RemoveAt (tab.Index);
				pages.Insert (targetPos, tab);
			} else {
				pages.Insert (targetPos + 1, tab);
				pages.RemoveAt (tab.Index);
			}
			IdeApp.Workbench.ReorderDocuments (tab.Index, targetPos);
			UpdateIndexes (Math.Min (tab.Index, targetPos));
			tabStrip.Update ();
		}

		internal void OnCloseTab (DockNotebookTab tab)
		{
			if (TabClosed != null)
				TabClosed (this, new TabEventArgs () { Tab = tab });
		}

		internal void OnActivateTab (DockNotebookTab tab)
		{
			if (TabActivated != null)
				TabActivated (this, new TabEventArgs () { Tab = tab });
		}
		
		internal void ShowContent (DockNotebookTab tab)
		{
			if (tab == currentTab)
				contentBox.Child = tab.Content;
		}

		protected override void OnDestroyed ()
		{
			if (fleurCursor != null) {
				fleurCursor.Dispose ();
				fleurCursor = null;
			}
			base.OnDestroyed ();
		}
	}
}
