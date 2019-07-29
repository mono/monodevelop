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
using System.Linq;
using System.IO;
using MonoDevelop.Ide;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Shell;

namespace MonoDevelop.Components.DockNotebook
{
	delegate void TabsReorderedHandler (DockNotebookTab oldPlacement, DockNotebookTab newPlacement);

	class DockNotebook : Gtk.VBox, IShellNotebook
	{
		List<DockNotebookTab> pages = new List<DockNotebookTab> ();
		List<DockNotebookTab> previewPages = new List<DockNotebookTab> ();
		List<DockNotebookTab> pagesHistory = new List<DockNotebookTab> ();
		TabStrip tabStrip;
		Gtk.EventBox contentBox;
		ReadOnlyCollection<DockNotebookTab> pagesCol;
		ReadOnlyCollection<DockNotebookTab> previewPagesCol;
		const int MAX_LASTACTIVEWINDOWS = 10;

		DockNotebookTab currentTab;

		static DockNotebook activeNotebook;
		static List<DockNotebook> allNotebooks = new List<DockNotebook> ();

		public static event EventHandler ActiveNotebookChanged;
		public static event EventHandler NotebookChanged;

		enum TargetList {
			UriList = 100
		}

		static Gtk.TargetEntry[] targetEntryTypes = new Gtk.TargetEntry[] {
			new Gtk.TargetEntry ("text/uri-list", 0, (uint)TargetList.UriList)
		};

		public DockNotebook ()
		{
			pagesCol = new ReadOnlyCollection<DockNotebookTab> (pages);
			previewPagesCol = new ReadOnlyCollection<DockNotebookTab> (previewPages);
			AddEvents ((Int32)(EventMask.AllEventsMask));

			tabStrip = new TabStrip (this);

			PackStart (tabStrip, false, false, 0);

			contentBox = new EventBox ();
			contentBox.Accessible.SetShouldIgnore (true);
			PackStart (contentBox, true, true, 0);

			ShowAll ();

			contentBox.NoShowAll = true;

			tabStrip.DropDownButton.Sensitive = false;

			tabStrip.DropDownButton.ContextMenuRequested = delegate {
				ContextMenu menu = new ContextMenu ();
				foreach (var tab in pages) {
					var item = new ContextMenuItem (tab.Markup ?? tab.Text);
					var locTab = tab;
					item.Clicked += (object sender, ContextMenuItemClickedEventArgs e) => {
						CurrentTab = locTab;
					};
					menu.Items.Add (item);
				}
				return menu;
			};

			Gtk.Drag.DestSet (this, Gtk.DestDefaults.Motion | Gtk.DestDefaults.Highlight | Gtk.DestDefaults.Drop, targetEntryTypes, Gdk.DragAction.Copy);
			DragDataReceived += new Gtk.DragDataReceivedHandler (OnDragDataReceived);

			DragMotion += delegate {
				// Bring this window to the front. Otherwise, the drop may end being done in another window that overlaps this one
				if (!Platform.IsWindows) {
					var window = ((Gtk.Window)Toplevel);
					if (window is DockWindow)
						window.Present ();
				}
			};

			allNotebooks.Add (this);
		}

		public static DockNotebook ActiveNotebook {
			get { return activeNotebook; }
			set {
				if (activeNotebook != value) {
					if (activeNotebook != null)
						activeNotebook.tabStrip.IsActiveNotebook = false;
					activeNotebook = value;
					if (activeNotebook != null)
						activeNotebook.tabStrip.IsActiveNotebook = true;
					if (ActiveNotebookChanged != null)
						ActiveNotebookChanged (null, EventArgs.Empty);
				}
			}
		}

		public static IEnumerable<DockNotebook> AllNotebooks {
			get { return allNotebooks; }
		}

		Cursor fleurCursor = new Cursor (CursorType.Fleur);

		public event TabsReorderedHandler TabsReordered;
		public event EventHandler<TabEventArgs> TabClosed;
		public event EventHandler<TabEventArgs> TabActivated;

		public event EventHandler<TabEventArgs> PageAdded;
		public event EventHandler<TabEventArgs> PageRemoved;
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

		public IEnumerable<DockNotebookTab> Tabs => pagesCol.Concat (previewPagesCol);

		public ReadOnlyCollection<DockNotebookTab> NormalTabs {
			get { return pagesCol; }
		}

		public ReadOnlyCollection<DockNotebookTab> PreviewTabs {
			get { return previewPagesCol; }
		}

		public DockNotebookTab CurrentTab {
			get { return currentTab; }
			set {
				if (currentTab != value) {
					currentTab = value;
					if (contentBox.Child != null)
						contentBox.Remove (contentBox.Child);

					if (currentTab != null) {
						if (currentTab.Content != null) {
							contentBox.Add (currentTab.Content);
							// Focus the last child, as some editors like the JSON one have a dropdown at the top.
							contentBox.ChildFocus (DirectionType.Up);
						}
						pagesHistory.Remove (currentTab);
						pagesHistory.Insert (0, currentTab);
						if (pagesHistory.Count > MAX_LASTACTIVEWINDOWS)
							pagesHistory.RemoveAt (pagesHistory.Count - 1);
					}

					tabStrip.Update ();

					if (SwitchPage != null)
						SwitchPage (this, EventArgs.Empty);
				}
			}
		}

		void SelectLastActiveTab (DockNotebookTab lastClosed)
		{
			if (pages.Count == 0 && previewPages.Count == 0) {
				CurrentTab = null;
				return;
			}

			var collection = GetCollection (lastClosed.IsPreview);

			if (pagesHistory.Count == 0) {
				if (collection.Count > 0) {
					CurrentTab = collection.Last ();
					return;
				}
				var otherTabs = GetCollection (!lastClosed.IsPreview);
				if (otherTabs.Count > 0) {
					CurrentTab = otherTabs.Last ();
					return;
				} 

				CurrentTab = null;
				return;
			}

			while (pagesHistory.Count > 0 && pagesHistory [0].Content == null)
				pagesHistory.RemoveAt (0);

			if (pagesHistory.Count > 0)
				CurrentTab = pagesHistory [0];
			else {
				if (lastClosed.Index + 1 < collection.Count)
					CurrentTab = collection [lastClosed.Index + 1];
				else
					CurrentTab = collection [lastClosed.Index - 1];
			}
		}

		public int NormalTabCount {
			get { return pages.Count; }
		}

		public int TabCount {
			get { return NormalTabCount + PreviewTabCount; }
		}

		public int PreviewTabCount {
			get { return previewPages.Count; }
		}

		public int BarHeight {
			get { return tabStrip.BarHeight; }
		}

		internal void InitSize ()
		{
			tabStrip.InitSize ();
		}

		async void OnDragDataReceived (object o, Gtk.DragDataReceivedArgs args)
		{
			if (args.Info != (uint) TargetList.UriList)
				return;
			string fullData = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data);

			var loadWorkspaceItems = !IsInsideTabStrip (args.X, args.Y);
			var files = new List<Ide.Gui.FileOpenInformation> ();

			foreach (string individualFile in fullData.Split ('\n')) {
				string file = individualFile.Trim ();
				if (file.StartsWith ("file://")) {
					var filePath = new FilePath (file);
					if (filePath.IsDirectory) {
						if (!loadWorkspaceItems) // skip directories when not loading solutions
							continue;
						filePath = Directory.EnumerateFiles (filePath).FirstOrDefault (p => Services.ProjectService.IsWorkspaceItemFile (p));
					}
					if (!filePath.IsNullOrEmpty) // skip empty paths
						files.Add (new Ide.Gui.FileOpenInformation (filePath, null, 0, 0, Ide.Gui.OpenDocumentOptions.DefaultInternal) { DockNotebook = this });
				}
			}

			if (files.Count > 0) {
				if (loadWorkspaceItems) {
					try {
						IdeApp.OpenFiles (files);
					} catch (Exception e) {
						LoggingService.LogError ($"Failed to open dropped files", e);
					}
				} else { // open workspace items as files
					foreach (var file in files) {
						try {
							await IdeApp.Workbench.OpenDocument (file).ConfigureAwait (false);
						} catch (Exception e) {
							LoggingService.LogError ($"unable to open file {file}", e);
						}
					}
				}
			}
		}

		bool IsInsideTabStrip (int pointerX, int pointerY)
		{
			if (tabStrip?.IsRealized != true)
				return false;
			int tabX, tabY;
			TranslateCoordinates (tabStrip, pointerX, pointerY, out tabX, out tabY);
			return tabStrip.Allocation.Contains (new Point (tabX, tabY));
		}

		public DockNotebookContainer Container {
			get {
				var container = (DockNotebookContainer)Parent;
				return container.MotherContainer () ?? container;
			}
		}

		/// <summary>
		/// Returns the next notebook in the same window
		/// </summary>
		public DockNotebook GetNextNotebook ()
		{
			return Container.GetNextNotebook (this);
		}

		/// <summary>
		/// Returns the previous notebook in the same window
		/// </summary>
		public DockNotebook GetPreviousNotebook ()
		{
			return Container.GetPreviousNotebook (this);
		}

		public Action<DockNotebook, DockNotebookTab, Gdk.EventButton> DoPopupMenu { get; set; }

		public DockNotebookTab AddTab (Gtk.Widget content = null, bool isPreview = false)
		{
			var t = InsertTab (-1, isPreview);
			if (content != null)
				t.Content = content;
			return t;
		}

		public DockNotebookTab InsertTab (int index, bool isPreview = false)
		{
			var tabPages = isPreview ? previewPages : pages;

			var tab = new DockNotebookTab (this, tabStrip);
			tab.IsPreview = isPreview;
			if (index == -1) {
				tabPages.Add (tab);
				tab.Index = tabPages.Count - 1;
			} else {
				tabPages.Insert (index, tab);
				tab.Index = index;
				UpdateIndexes (tabPages, index + 1);
			}

			pagesHistory.Add (tab);

			if (tabPages.Count == 1)
				CurrentTab = tab;

			tabStrip.StartOpenAnimation ((DockNotebookTab)tab);
			tabStrip.Update ();
			tabStrip.DropDownButton.Sensitive = tabPages.Count > 0;

			PageAdded?.Invoke (this, new TabEventArgs { Tab = tab, });

			NotebookChanged?.Invoke (this, EventArgs.Empty);

			return tab;
		}

		void UpdateIndexes (List<DockNotebookTab> tabs, int startIndex)
		{
			for (int n = startIndex; n < tabs.Count; n++)
				tabs [n].Index = n;
		}

		public DockNotebookTab GetTab (int n)
		{
			return GetBaseTab (pages, n);
		}

		public DockNotebookTab GetPreviewTab (int n)
		{
			return GetBaseTab (previewPages, n);
		}

		DockNotebookTab GetBaseTab (List<DockNotebookTab> col, int n)
		{
			if (n < 0 || n >= col.Count)
				return null;
			else
				return col [n];
		}

		public void RemoveTab (DockNotebookTab tab, bool animate)
		{
			var list = GetCollection (tab.IsPreview);

			if (animate)
				tabStrip.StartCloseAnimation ((DockNotebookTab)tab);
			pagesHistory.Remove (tab);

			list.Remove (tab);

			SelectLastActiveTab (tab);

			UpdateIndexes (list, tab.Index);

			tabStrip.Update ();
			tabStrip.DropDownButton.Sensitive = list.Count > 0;

			PageRemoved?.Invoke (this, new TabEventArgs { Tab = tab });

			NotebookChanged?.Invoke (this, EventArgs.Empty);
		}

		internal void ChangeTabToPreview (DockNotebookTab tab)
		{
			if (tab.IsPreview) {
				return;
			}
			pages.Remove (tab);
			if (!previewPages.Contains (tab)) {
				previewPages.Add (tab);
			}

			tab.IsPreview = true;

			UpdateIndexes (pages, tab.Index);
			UpdateIndexes (previewPages, 0);
		}

		internal void ReorderTab (DockNotebookTab tab, DockNotebookTab targetTab)
		{
			var container = GetCollection (tab.IsPreview);
			if (tab == targetTab)
				return;
			int targetPos = targetTab.Index;
			if (tab.Index > targetTab.Index) {
				container.RemoveAt (tab.Index);
				container.Insert (targetPos, tab);
			} else {
				container.Insert (targetPos + 1, tab);
				container.RemoveAt (tab.Index);
			}
			if (TabsReordered != null) {
				TabsReordered (tab, targetTab);
			}
			UpdateIndexes (container, Math.Min (tab.Index, targetPos));
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

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			ActiveNotebook = this;
			return base.OnButtonPressEvent (evnt);
		}

		protected override void OnDestroyed ()
		{
			allNotebooks.Remove (this);

			if (ActiveNotebook == this)
				ActiveNotebook = null;
			if (fleurCursor != null) {
				fleurCursor.Dispose ();
				fleurCursor = null;
			}
			base.OnDestroyed ();
		}

		internal bool ContainsTab (DockNotebookTab tab) => GetReadOnlyCollectionForTab (tab).Contains (tab);

		List<DockNotebookTab> GetCollection (bool isPreview) => isPreview ? previewPages : pages;

		public ReadOnlyCollection<DockNotebookTab> GetReadOnlyCollectionForTab (DockNotebookTab tab) => tab.IsPreview ? PreviewTabs : NormalTabs;
	}

	class DockNotebookChangedArgs : EventArgs
	{
		public DockNotebook Notebook { get; private set; }
		public DockNotebookChangedArgs (DockNotebook notebook)
		{
			Notebook = notebook;
		}
	}
}
