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

using System.Linq;
using Gdk;
using Gtk;
using System;
using Mono.TextEditor;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cairo;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui
{
	public delegate void TabsReorderedHandler (Widget widget, int oldPlacement, int newPlacement);

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
			ButtonPressEvent += new ButtonPressEventHandler (OnButtonPress);
			ButtonReleaseEvent += new ButtonReleaseEventHandler (OnButtonRelease);
			AddEvents ((Int32)(EventMask.AllEventsMask));

			tabStrip = new TabStrip (this);

			PackStart (tabStrip, false, false, 0);

			contentBox = new EventBox ();
			PackStart (contentBox, true, true, 0);

			ShowAll ();

			tabStrip.DropDownButton.MenuCreator = delegate {
				Gtk.Menu menu = new Menu ();
				foreach (var tab in pagesHistory) {
					var mi = new Gtk.ImageMenuItem (tab.Text);
					menu.Insert (mi, -1);
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
						if (currentTab.Content != null)
							contentBox.Add (currentTab.Content);
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
			set { CurrentTab = pages [value]; }
		}

		public int TabCount {
			get { return pages.Count; }
		}

		public Action<int,Gdk.EventButton> DoPopupMenu { get; set; }

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

			tabStrip.Update ();

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

			if (PageRemoved != null)
				PageRemoved (this, EventArgs.Empty);
		}

		internal void ReorderTab (DockNotebookTab tab, DockNotebookTab targetTabPosition)
		{
			if (tab == targetTabPosition)
				return;
			if (tab.Index > targetTabPosition.Index) {
				pages.RemoveAt (tab.Index);
				pages.Insert (targetTabPosition.Index, tab);
			} else {
				pages.Insert (targetTabPosition.Index + 1, tab);
				pages.RemoveAt (tab.Index);
			}
			UpdateIndexes (0);
			tabStrip.Update ();
		}

		internal void OnCloseTab (DockNotebookTab tab)
		{
			if (TabClosed != null)
				TabClosed (this, new TabEventArgs () { Tab = tab });
		}

		internal void ShowContent (DockNotebookTab tab)
		{
			if (tab == currentTab)
				contentBox.Child = tab.Content;
		}

		bool DragInProgress;

		public int FindTabAtPosition (double cursorX, double cursorY) {
/*			int    dragNotebookXRoot;
			int    dragNotebookYRoot;
			Widget page;
			int    pageNumber        = CurrentPage;
			Widget tab;
			int    tabMaxX;
			int    tabMaxY;
			int    tabMinX;
			int    tabMinY;
			int? direction = null;

			ParentWindow.GetOrigin (out dragNotebookXRoot, out dragNotebookYRoot);
			
			// We cannot rely on the allocations being zero for tabs which are
			// offscreen. If we write the logic to walk from page 0 til NPages,
			// we can end up choosing the wrong page because pages which are
			// offscreen will match the mouse coordinates. We can work around
			// this by walking either up or down from the active page and choosing
			// the first page which is within the mouse x/y coordinates.
			while ((page = GetTab (pageNumber)) != null && pageNumber >= 0 && pageNumber <= NPages) {

				if ((tab = GetTabLabel (page)) == null)
					return -1;

				tabMinX = dragNotebookXRoot + tab.Allocation.X;
				tabMaxX = tabMinX + tab.Allocation.Width;

				tabMinY = dragNotebookYRoot + tab.Allocation.Y;
				tabMaxY = tabMinY + tab.Allocation.Height;

				if ((tabMinX <= cursorX) && (cursorX <= tabMaxX) &&
					(tabMinY <= cursorY) && (cursorY <= tabMaxY))
					return pageNumber;

				if (!direction.HasValue)
						direction = cursorX > tabMaxX ? 1 : -1;

				pageNumber += direction.Value;
			}
*/
			return -1;
		}

		void MoveTab (int destinationPage)
		{
			if (destinationPage >= 0 && destinationPage != CurrentTabIndex) {
				int oldPage = CurrentTabIndex;
				ReorderChild (CurrentTab.Content, destinationPage);

				if (TabsReordered != null)
					TabsReordered (CurrentTab.Content, oldPage, destinationPage);
			}
		}

		[GLib.ConnectBefore]
		void OnButtonPress (object obj, ButtonPressEventArgs args) {

			if (DragInProgress || args.Event.TriggersContextMenu ())
				return;

			if (args.Event.Button == 1 && args.Event.Type == EventType.ButtonPress && FindTabAtPosition (args.Event.XRoot, args.Event.YRoot) >= 0)
				MotionNotifyEvent += new MotionNotifyEventHandler (OnMotionNotify);
		}
		
		public void LeaveDragMode (uint time)
		{
			if (DragInProgress) {
				Pointer.Ungrab (time);
				Grab.Remove (this);
			}
			MotionNotifyEvent -= new MotionNotifyEventHandler (OnMotionNotify);
			DragInProgress = false;
		}
		
		[GLib.ConnectBefore]
		void OnButtonRelease (object obj, ButtonReleaseEventArgs args) {
			LeaveDragMode (args.Event.Time);
		}


		[GLib.ConnectBefore]
		void OnMotionNotify (object obj, MotionNotifyEventArgs args) {

			if (!DragInProgress) {
				DragInProgress = true;
				Grab.Add (this);

				if (!Pointer.IsGrabbed)
					Pointer.Grab (ParentWindow, false, EventMask.Button1MotionMask | EventMask.ButtonReleaseMask, null, fleurCursor, args.Event.Time);	
			}

			MoveTab (FindTabAtPosition (args.Event.XRoot, args.Event.YRoot));
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

	interface IDockNotebookTab
	{
		int Index { get; }
		string Text { get; set; }
		string Markup { get; set; }
		Pixbuf Icon { get; set; }
		Widget Content { get; set; }
		string Tooltip { get; set; }
	}

	internal class DockNotebookTab: IDockNotebookTab
	{
		DockNotebook notebook;
		TabStrip strip;

		string text;
		string markup;
		Gdk.Pixbuf icon;
		Gtk.Widget content;

		internal Gdk.Rectangle Allocation;
		internal Gdk.Rectangle CloseButtonAllocation;

		public int Index { get; internal set; }

		public string Text {
			get {
				return this.text;
			}
			set {
				text = value;
				markup = null;
				strip.Update ();
			}
		}

		public string Markup {
			get {
				return this.markup;
			}
			set {
				markup = value;
				text = null;
				strip.Update ();
			}
		}

		public Pixbuf Icon {
			get {
				return this.icon;
			}
			set {
				icon = value;
				strip.Update ();
			}
		}

		public Widget Content {
			get {
				return this.content;
			}
			set {
				content = value;
				notebook.ShowContent (this);
			}
		}

		public string Tooltip { get; set; }

		internal DockNotebookTab (DockNotebook notebook, TabStrip strip)
		{
			this.notebook = notebook;
			this.strip = strip;
		}
	}

	class TabEventArgs: EventArgs
	{
		public DockNotebookTab Tab { get; set; }
	}

	class TabStrip: EventBox
	{
		DockNotebook notebook;
		IDockNotebookTab firstTab;
		int firstTabIndex;
		IDockNotebookTab highlightedTab;
		bool overCloseButton;
		bool buttonPressedOnTab;
		int tabStartX, tabEndX;

		bool draggingTab;
		int dragX;
		int dragOffset;

		DockNotebookTab closingTab;
		int closingTabIndex;
		int closingTabStep;
		int closingAnimationLength;
		double closingTabAnimationStep;
		uint closingAnimation;

		public Gtk.Button PreviousButton;
		public Gtk.Button NextButton;
		public MenuButton DropDownButton;

		static Gdk.Pixbuf closeSelImage;
		static Gdk.Pixbuf closeSelOverImage;
		static Gdk.Pixbuf closeImage;
		static Gdk.Pixbuf closeOverImage;

		const int TopBarPadding = 3;
		const int BottomBarPadding = 3;
		const int LeftRightPadding = 11;
		const int LeftRightPaddingSel = 7;
		const int TopPadding = 7;
		const int BottomPadding = 7;
		const int SeparatorWidth = 2;
		const int LabelButtonSeparatorWidth = 6;
		const double TabBorderRadius = 3;
		const int BackgroundShadowHeight = 5;
		const int LeftBarPadding = 58;

		const uint CloseAnimationFrameRate = 20;
		const int CloseAnimationStepSize = 25;

		static TabStrip ()
		{
			try {
				closeSelImage = Gdk.Pixbuf.LoadFromResource ("MonoDevelop.Close.Selected.png");
				closeSelOverImage = Gdk.Pixbuf.LoadFromResource ("MonoDevelop.Close.Selected.Over.png");
				closeImage = Gdk.Pixbuf.LoadFromResource ("MonoDevelop.Close.png");
				closeOverImage = Gdk.Pixbuf.LoadFromResource ("MonoDevelop.Close.Over.png");
			} catch (Exception e) {
				MonoDevelop.Core.LoggingService.LogError ("Can't create pixbuf from resource: MonoDevelop.Close.png", e);
			}
		}

		public TabStrip (DockNotebook notebook)
		{
			this.notebook = notebook;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			Events |= Gdk.EventMask.PointerMotionMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonPressMask;

			var arr = new Gtk.Image (Gdk.Pixbuf.LoadFromResource ("tabbar-prev.png"));
			arr.HeightRequest = arr.WidthRequest = 10;
			PreviousButton = new Button (arr);
			PreviousButton.Relief = ReliefStyle.None;
			PreviousButton.CanDefault = PreviousButton.CanFocus = false;

			arr = new Gtk.Image (Gdk.Pixbuf.LoadFromResource ("tabbar-next.png"));
			arr.HeightRequest = arr.WidthRequest = 10;
			NextButton = new Button (arr);
			NextButton.Relief = ReliefStyle.None;
			NextButton.CanDefault = NextButton.CanFocus = false;

			DropDownButton = new MenuButton ();
			DropDownButton.Relief = ReliefStyle.None;
			DropDownButton.CanDefault = DropDownButton.CanFocus = false;

			PreviousButton.ShowAll ();
			NextButton.ShowAll ();
			DropDownButton.ShowAll ();

			PreviousButton.Name = "MonoDevelop.DockNotebook.BarButton";
			NextButton.Name = "MonoDevelop.DockNotebook.BarButton";
			DropDownButton.Name = "MonoDevelop.DockNotebook.BarButton";

			PreviousButton.Parent = this;
			NextButton.Parent = this;
			DropDownButton.Parent = this;
		}

		public void StartCloseAnimation (DockNotebookTab closingTab)
		{
			if (closingAnimation != 0)
				GLib.Source.Remove (closingAnimation);

			int x = 0;
			CalcTabSize (closingTab, ref x, false, false);

			this.closingTab = closingTab;
			closingTabIndex = closingTab.Index;
			closingTabStep = 0;
			closingAnimationLength = x;
			closingTabAnimationStep = 0;
			closingAnimation = GLib.Timeout.Add (CloseAnimationFrameRate, AnimateClosingTab);
		}

		bool AnimateClosingTab ()
		{
			closingTabStep += CloseAnimationStepSize;
			closingTabAnimationStep = (double)closingTabStep / (double)closingAnimationLength;
			QueueDraw ();
			if (closingTabStep < closingAnimationLength)
				return true;
			else {
				closingAnimation = 0;
				closingTab = null;
				return false;
			}
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			base.ForAll (include_internals, callback);
			callback (NextButton);
			callback (PreviousButton);
			callback (DropDownButton);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			tabStartX = allocation.X + LeftBarPadding;
			tabEndX = allocation.Width - DropDownButton.SizeRequest ().Width;

			PreviousButton.SizeAllocate (new Gdk.Rectangle (
				allocation.X,
				allocation.Y,
				LeftBarPadding / 2,
				allocation.Height - BottomBarPadding
			)
			);
			NextButton.SizeAllocate (new Gdk.Rectangle (
				allocation.X + LeftBarPadding / 2,
				allocation.Y,
				LeftBarPadding / 2, allocation.Height - BottomBarPadding)
			);

			DropDownButton.SizeAllocate (new Gdk.Rectangle (
				tabEndX,
				allocation.Y,
				DropDownButton.SizeRequest ().Width,
				allocation.Height - BottomBarPadding));

			base.OnSizeAllocated (allocation);
			Update ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			Pango.Layout la = new Pango.Layout (PangoContext);
			la.SetText ("H");
			int w, h;
			la.GetPixelSize (out w, out h);

			h += TopPadding + BottomPadding;
			requisition.Height = h;
			requisition.Width = 0;
		}

		int lastDragX = 0;

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			string newTooltip = null;

			if (!draggingTab) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);
				if (t != highlightedTab) {
					highlightedTab = t;
					QueueDraw ();
				}
				var newOver = IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y);
				if (newOver != overCloseButton) {
					overCloseButton = newOver;
					QueueDraw ();
				}
				if (!overCloseButton && !draggingTab && buttonPressedOnTab) {
					draggingTab = true;
					int x = (int)evnt.X;
					dragOffset = x - t.Allocation.X;
					dragX = x - dragOffset;
					lastDragX = (int)evnt.X;
				} else if (t != null)
					newTooltip = t.Tooltip;
			} else {
				dragX = (int)evnt.X - dragOffset;
				QueueDraw ();

				var t = FindTab ((int)evnt.X, TopPadding + 3);
				if (t == null) {
					var last = (DockNotebookTab)notebook.Tabs.Last ();
					if (dragX > last.Allocation.Right)
						t = last;
					if (dragX < 0)
						t = (DockNotebookTab)notebook.Tabs.First ();
				}
				if (t != null && t != notebook.CurrentTab && (
					((int)evnt.X > lastDragX && t.Index > notebook.CurrentTab.Index) ||
					((int)evnt.X < lastDragX && t.Index < notebook.CurrentTab.Index)))
				{
					notebook.ReorderTab ((DockNotebookTab)notebook.CurrentTab, t);
				}
				lastDragX = (int)evnt.X;
			}

			if (newTooltip != null && TooltipText != null && TooltipText != newTooltip)
				TooltipText = null;
			else
				TooltipText = newTooltip;

			return base.OnMotionNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			highlightedTab = null;
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			var t = FindTab ((int)evnt.X, (int)evnt.Y);
			if (t != null) {
				if (evnt.IsContextMenuButton ()) {
					notebook.DoPopupMenu (t.Index, evnt);
					return true;
				}
				buttonPressedOnTab = true;
				// Don't select the tab if we are clicking the close button
				if (IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y))
					return true;
				notebook.CurrentTab = t;
				return true;
			} else
				buttonPressedOnTab = true;
			QueueDraw ();
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			buttonPressedOnTab = false;
			if (!draggingTab) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);
				if (t != null && IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
					notebook.OnCloseTab (t);
					return true;
				}
			}
			draggingTab = false;
			QueueDraw ();
			return base.OnButtonReleaseEvent (evnt);
		}

		DockNotebookTab FindTab (int x, int y)
		{
			for (int n = firstTabIndex; n < notebook.Tabs.Count; n++) {
				DockNotebookTab tab = (DockNotebookTab) notebook.Tabs[n];
				if (tab.Allocation.Contains (x, y))
					return tab;
			}
			return null;
		}

		bool IsOverCloseButton (DockNotebookTab tab, int x, int y)
		{
			return tab != null && tab.CloseButtonAllocation.Contains (x, y);
		}

		public void Update ()
		{
			if (firstTab != null && notebook.Tabs.Contains (firstTab))
				firstTabIndex = firstTab.Index;

			int tabsAllocWidth = tabEndX - tabStartX;

			int lastTabIndex = CalcLastTab (firstTabIndex);
			if (lastTabIndex == -1)
				lastTabIndex = firstTabIndex;

			if (notebook.CurrentTab != null) {
				if (notebook.CurrentTab.Index <= firstTabIndex)
					firstTabIndex = notebook.CurrentTab.Index;
				else if (notebook.CurrentTab.Index > lastTabIndex) {
					firstTabIndex = notebook.CurrentTab.Index - 1;
					do {
						int t = CalcLastTab (firstTabIndex);
						if (t == notebook.CurrentTab.Index || t == -1)
							break;
						if (t < notebook.CurrentTab.Index) {
							firstTabIndex++;
							break;
						}
						firstTabIndex--;
					} while (firstTabIndex >= 0);
				}
			}

			// If there is space left in the tab strip, try to fill it using tabs
			// in lower indexes
			int startIndex = firstTabIndex;
			while (startIndex >= 0 && CalcTabExtent (startIndex, out lastTabIndex) < tabsAllocWidth)
				firstTabIndex = startIndex--;

			firstTab = notebook.GetTab (firstTabIndex);

			QueueDraw ();
		}

		int CalcLastTab (int startIndex)
		{
			int lastIncludedTabIndex;
			CalcTabExtent (startIndex, out lastIncludedTabIndex);
			return lastIncludedTabIndex;
		}

		int CalcTabExtent (int startIndex, out int lastIncludedTabIndex)
		{
			// Gets the space taken for the tabs starting at firstTabIndex, and including
			// tabs which don't go beyond maxExtent

			int maxExtent = tabEndX - tabStartX;
			lastIncludedTabIndex = -1;
			int x = 0;
			int n;
			for (n = startIndex; n < notebook.Tabs.Count; n++) {
				if (n > startIndex && (n - 1 != notebook.CurrentTabIndex))
					x += SeparatorWidth;
				var tab = notebook.Tabs [n];
				CalcTabSize (tab, ref x, tab == notebook.CurrentTab, tab == highlightedTab);
				if (x < maxExtent)
					lastIncludedTabIndex = n;
				else
					break;
			}
			return x;
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {

				// Draw the background
				var h = Allocation.Height - BottomBarPadding - BackgroundShadowHeight + 1;
				ctx.Rectangle (0, 0, Allocation.Width, h); // +1 since background and shadow overlap 1 pixel
				Cairo.LinearGradient gr = new LinearGradient (0, 0, 0, h);
				gr.AddColorStop (0, Styles.TabBarGradientStartColor);
				gr.AddColorStop (1, Styles.TabBarGradientMidColor);
				ctx.Pattern = gr;
				ctx.Fill ();

				ctx.Rectangle (0, h - 1, Allocation.Width, BackgroundShadowHeight);
				gr = new LinearGradient (0, h, 0, h - 1 + BackgroundShadowHeight);
				gr.AddColorStop (0, Styles.TabBarGradientMidColor);
				gr.AddColorStop (1, Styles.TabBarGradientEndColor);
				ctx.Pattern = gr;
				ctx.Fill ();

				if (notebook.Tabs.Count > 0) {
					ctx.Rectangle (0, Allocation.Height - BottomBarPadding, Allocation.Width, BottomBarPadding);
					ctx.Color = Styles.BreadcrumbBackgroundColor;
					ctx.Fill ();
				}

				ctx.Rectangle (tabStartX, 0, tabEndX - tabStartX, Allocation.Height); 
				ctx.Clip ();
				int x = tabStartX;
				int y = 0;
				for (int n = firstTabIndex; n < notebook.Tabs.Count && x < tabEndX; n++) {
					if (closingTab != null && n == closingTabIndex) {
						DrawTab (ctx, closingTab, ref x, y, false);
					}

					int sx = x;
					var tab = (DockNotebookTab)notebook.Tabs [n];

					if (tab == notebook.CurrentTab)
						DrawActiveTab (ctx, tab, ref x, y, tab == highlightedTab);
					else {
						DrawTab (ctx, tab, ref x, y, tab == highlightedTab);
						if (n + 1 < notebook.Tabs.Count && notebook.Tabs[n+1] != notebook.CurrentTab)
							DrawSeparator (ctx, ref x);
					}

					tab.Allocation = new Gdk.Rectangle (sx, Allocation.Y, x - sx, Allocation.Height);
				}

				if (draggingTab) {
					// We draw the active again to make sure it is drawn over all the other tabs
					DrawActiveTab (ctx, (DockNotebookTab)notebook.CurrentTab, ref x, y, true);
				}

				ctx.ResetClip ();
			}
			return base.OnExposeEvent (evnt);
		}

		void CalcTabSize (IDockNotebookTab tab, ref int x, bool active, bool highlight)
		{
			var la = CreateTabLayout (tab);

			int w, h;
			la.GetPixelSize (out w, out h);

			if (active) {
				var closePix = highlight && overCloseButton ? closeSelOverImage : closeSelImage;
				x += LeftRightPaddingSel * 2 + w + LabelButtonSeparatorWidth + closePix.Width;
			}
			else {
				var image = highlight && overCloseButton ? closeOverImage : closeImage;
				x += LeftRightPadding * 2 + w + LabelButtonSeparatorWidth + image.Width;
			}

			la.Dispose ();
		}

		void DrawSeparator (Cairo.Context ctx, ref int x)
		{
			var topMargin = TopBarPadding + 4;
			ctx.Rectangle (x, topMargin, 1, Allocation.Height - BottomBarPadding - topMargin);
			Cairo.LinearGradient gr = new LinearGradient (x, topMargin, x, Allocation.Height - BottomBarPadding - topMargin);
			gr.AddColorStop (0, new Cairo.Color (0, 0, 0, 0));
			gr.AddColorStop (1, new Cairo.Color (0, 0, 0, 0.3));
			ctx.Pattern = gr;
			ctx.Fill ();
			x++;

			ctx.Rectangle (x, topMargin, 1, Allocation.Height - BottomBarPadding - topMargin);
			gr = new LinearGradient (x, topMargin, x, Allocation.Height - BottomBarPadding - topMargin);
			gr.AddColorStop (0, new Cairo.Color (1, 1, 1, 0));
			gr.AddColorStop (1, new Cairo.Color (1, 1, 1, 0.3));
			ctx.Pattern = gr;
			ctx.Fill ();
			x++;
		}

		void DrawTab (Cairo.Context ctx, DockNotebookTab tab, ref int x, int y, bool highlight)
		{
			bool isClosing = tab == closingTab;

			var la = CreateTabLayout (tab);
			int w, h;
			la.GetPixelSize (out w, out h);

			x += LeftRightPadding;

			if (isClosing) {
				w = (int)((1d - closingTabAnimationStep) * (double)w);
				ctx.Save ();
				ctx.Rectangle (x, y + TopPadding, w, Allocation.Height);
				ctx.Clip ();
			}

			ctx.MoveTo (x, y + TopPadding);
			ctx.Color = highlight ? Styles.TabBarHoverInactiveTextColor : Styles.TabBarInactiveTextColor;
			PangoCairoHelper.ShowLayout (ctx, la);

			if (isClosing)
				ctx.Restore ();

			x += w + LabelButtonSeparatorWidth;

			var image = highlight && overCloseButton ? closeOverImage : closeImage;
			var ch = Allocation.Height - TopBarPadding - BottomBarPadding;
			var crect = new Gdk.Rectangle (x, y + TopBarPadding + (ch - image.Height) / 2, image.Width, image.Height);
			CairoHelper.SetSourcePixbuf (ctx, image, crect.X, crect.Y);
			ctx.Paint ();
			crect.Inflate (2, 2);
			tab.CloseButtonAllocation = crect;

			x += image.Width + LeftRightPadding;

			la.Dispose ();
		}

		void DrawActiveTab (Cairo.Context ctx, DockNotebookTab tab, ref int x, int y, bool highlight)
		{
			var la = CreateTabLayout (tab);
			int w, h;
			la.GetPixelSize (out w, out h);

			var closePix = highlight && overCloseButton ? closeSelOverImage : closeSelImage;

			int startX = x;

			if (draggingTab) {
				x = dragX;
				int tabsize = LeftRightPaddingSel + w + LabelButtonSeparatorWidth + closePix.Width + LeftRightPadding;
				if (x < tabStartX)
					x = tabStartX;
				else if (x + tabsize > tabEndX)
					x = tabEndX - tabsize;

			}
			int startXDrag = x;

			int tabWidth = w + LeftRightPaddingSel * 2 + LabelButtonSeparatorWidth + closePix.Width;

			ctx.LineWidth = 1;
			DrawTabBorder (ctx, tabWidth - 2, x, 0);
			ctx.ClosePath ();
			double tops = 5d / (Allocation.Bottom - TopBarPadding); // The initial gradient is 4 pixels height
			Cairo.LinearGradient gr = new LinearGradient (x, TopBarPadding, x, Allocation.Bottom);
			gr.AddColorStop (0, Styles.BreadcrumbGradientStartColor);
			gr.AddColorStop (tops, Styles.BreadcrumbBackgroundColor);
			gr.AddColorStop (1, Styles.BreadcrumbBackgroundColor);
			ctx.Pattern = gr;
			ctx.Fill ();

			ctx.Color = Styles.BreadcrumbBorderColor;
			DrawTabBorder (ctx, tabWidth - 2, x, 0);
			ctx.Stroke ();

			ctx.Color = Styles.BreadcrumbInnerBorderColor;
			DrawTabBorder (ctx, tabWidth - 4, x + 1, 1);
			ctx.Stroke ();

			x += LeftRightPaddingSel;

			ctx.MoveTo (x, y + TopPadding);
			ctx.Color = highlight ? Styles.TabBarHoverActiveTextColor : Styles.TabBarActiveTextColor;
			PangoCairoHelper.ShowLayout (ctx, la);

			x += w + LabelButtonSeparatorWidth;

			var ch = Allocation.Height - TopBarPadding - BottomBarPadding;
			var crect = new Gdk.Rectangle (x, y + TopBarPadding + (ch - closePix.Height) / 2, closePix.Width, closePix.Height);
			CairoHelper.SetSourcePixbuf (ctx, closePix, crect.X, crect.Y);
			ctx.Paint ();
			crect.Inflate (2, 2);
			tab.CloseButtonAllocation = crect;

			x += closePix.Width + LeftRightPadding;

			if (draggingTab)
				x = startX + (x - startXDrag);

			la.Dispose ();
		}

		void DrawTabBorder (Cairo.Context ctx, int contentWidth, int px, int margin)
		{
			double x = 0.5 + (double)px;
			double y = (double) Allocation.Height + 0.5 - BottomBarPadding + margin;
			double height = Allocation.Height - TopBarPadding - BottomBarPadding;

			ctx.MoveTo (0.5, y);
			ctx.LineTo (x, y);
			ctx.LineTo (x, y - height + TabBorderRadius);
			ctx.Arc (x + TabBorderRadius, y - height + TabBorderRadius, TabBorderRadius, System.Math.PI, System.Math.PI * 1.5d);
			double rightx = x + contentWidth + 1;
			ctx.LineTo (rightx - TabBorderRadius, y - height);
			ctx.Arc (rightx - TabBorderRadius, y - height + TabBorderRadius, TabBorderRadius, System.Math.PI * 1.5d, System.Math.PI * 2d);
			ctx.LineTo (rightx, y);
			ctx.LineTo (Allocation.Width - 0.5, y);
		}

		Pango.Layout CreateTabLayout (IDockNotebookTab tab)
		{
			Pango.Layout la = new Pango.Layout (PangoContext);
			if (!string.IsNullOrEmpty (tab.Markup))
				la.SetMarkup (tab.Markup);
			else if (!string.IsNullOrEmpty (tab.Text))
				la.SetText (tab.Text);
			return la;
		}
	}
}