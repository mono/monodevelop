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

		public int BarHeight {
			get { return tabStrip.BarHeight; }
		}

		internal void InitSize (Gtk.Window window)
		{
			tabStrip.InitSize (window);
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
		List<Gtk.Widget> children = new List<Widget> ();
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

		const int TabWidth = 125;
		const int TopBarPadding = 3;
		const int BottomBarPadding = 3;
		const int LeftRightPadding = 10;
		const int TopPadding = 7;
		const int BottomPadding = 7;
		const int SeparatorWidth = 2;
		const double TabBorderRadius = 3;
		const int BackgroundShadowHeight = 5;
		const int LeftBarPadding = 58;
		const int VerticalTextSize = 11;

		const int ActiveTabVerticalOffset = 2;
		const int TextOffset = -1;

		const uint CloseAnimationFrameRate = 20;
		const int CloseAnimationStepSize = 25;

		// Vertically aligns the close image(s) with the tab label.
		const int CloseImageTopOffset = 4;

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
			GtkWorkarounds.FixContainerLeak (this);

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

			children.Add (PreviousButton);
			children.Add (NextButton);
			children.Add (DropDownButton);
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
			foreach (var c in children)
				callback (c);
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

		int totalHeight;

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Height = totalHeight;
			requisition.Width = 0;
		}

		internal void InitSize (Gtk.Window window)
		{
			Pango.Layout la = CreateSizedLayout ();
			la.SetText ("H");
			int w, h;
			la.GetPixelSize (out w, out h);
			
			totalHeight = h + TopPadding + BottomPadding;
			la.Dispose ();
		}

		public int BarHeight {
			get {
				return totalHeight - BottomBarPadding + 1;
			}
		}

		int lastDragX = 0;

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			string newTooltip = null;

			if (!draggingTab) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);

				// If the user clicks and drags on the 'x' which closes the current
				// tab we can end up with a null tab here
				if (t == null)
					return base.OnMotionNotifyEvent (evnt);;

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
				if (evnt.Type == Gdk.EventType.TwoButtonPress) {
					notebook.OnActivateTab (t);
					buttonPressedOnTab = false;
					return true;
				}
				if (evnt.Button == 2) {
					notebook.OnCloseTab (t);
					return true;
				}
				// Don't select the tab if we are clicking the close button
				if (IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y))
					return true;
				buttonPressedOnTab = true;
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
				var h = Allocation.Height;
				ctx.Rectangle (0, 0, Allocation.Width, h); // +1 since background and shadow overlap 1 pixel
				Cairo.LinearGradient gr = new LinearGradient (0, 0, 0, h);
				gr.AddColorStop (0, Styles.TabBarGradientStartColor);
				gr.AddColorStop (1, Styles.TabBarGradientMidColor);
				ctx.Pattern = gr;
				ctx.Fill ();

				ctx.MoveTo (Allocation.X, 0.5);
				ctx.LineTo (Allocation.Right, 0.5);
				ctx.LineWidth = 1;
				ctx.Color = CairoExtensions.ParseColor ("e0e0e0");
				ctx.Stroke ();
			}

			foreach (var c in children)
				PropagateExpose (c, evnt);

			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				
				if (notebook.Tabs.Count > 0) {
					ctx.Rectangle (0, Allocation.Height - BottomBarPadding, Allocation.Width, BottomBarPadding);
					ctx.Color = Styles.BreadcrumbBackgroundColor;
					ctx.Fill ();
				}

				int lastTabIndex = CalcLastTab (firstTabIndex);
				int x = tabStartX;
				int y = 0;
				for (int n = firstTabIndex; n <= lastTabIndex && x < tabEndX; n++) {
					if (closingTab != null && n == closingTabIndex) {
						DrawTab (ctx, closingTab, ref x, y, false, false, false);
					}

					int sx = x;
					var tab = (DockNotebookTab)notebook.Tabs [n];

					if (tab == notebook.CurrentTab)
						DrawTab (ctx, tab, ref x, y, tab == highlightedTab, true, draggingTab);
					else
						DrawTab (ctx, tab, ref x, y, tab == highlightedTab, false, false);

					tab.Allocation = new Gdk.Rectangle (sx, Allocation.Y, x - sx, Allocation.Height);
				}

				if (draggingTab) {
					// We draw the active again to make sure it is drawn over all the other tabs
					DrawTab (ctx, (DockNotebookTab)notebook.CurrentTab, ref x, y, true, true, true);
				}

				ctx.ResetClip ();
			}
			return base.OnExposeEvent (evnt);
		}

		void CalcTabSize (IDockNotebookTab tab, ref int x, bool active, bool highlight)
		{
			var la = CreateTabLayout (tab);

			if (active) {
				var closePix = highlight && overCloseButton ? closeSelOverImage : closeSelImage;
				x += LeftRightPadding * 2 + TabWidth + closePix.Width;
			}
			else {
				var image = highlight && overCloseButton ? closeOverImage : closeImage;
				x += LeftRightPadding * 2 + TabWidth + image.Width;
			}

			la.Dispose ();
		}

		void DrawTab (Cairo.Context ctx, DockNotebookTab tab, ref int x, int y, bool highlight, bool active, bool dragging)
		{
			bool isClosing = tab == closingTab;
			var la = CreateTabLayout (tab);

			var closePix = highlight && overCloseButton ? closeSelOverImage : closeSelImage;
			int w = TabWidth - (LeftRightPadding * 2 + closePix.Width);
			Console.WriteLine (w);

			la.Ellipsize = Pango.EllipsizeMode.End;
			la.Width = Pango.Units.FromPixels (w + 3);

			int startX = x;

			if (dragging) {
				x = dragX;
				int tabsize = LeftRightPadding + w + closePix.Width + LeftRightPadding;
				if (x < tabStartX)
					x = tabStartX;
				else if (x + tabsize > tabEndX)
					x = tabEndX - tabsize;

			}
			int startXDrag = x;


			ctx.LineWidth = 1;
			DrawTabBorder (ctx, TabWidth, x, 0, active);
			ctx.ClosePath ();
			double tops = 5d / (Allocation.Bottom - TopBarPadding); // The initial gradient is 4 pixels height
			Cairo.LinearGradient gr = new LinearGradient (x, TopBarPadding, x, Allocation.Bottom);
			if (active) {
				gr.AddColorStop (0, Styles.BreadcrumbGradientStartColor);
				gr.AddColorStop (tops, Styles.BreadcrumbBackgroundColor);
				gr.AddColorStop (1, Styles.BreadcrumbBackgroundColor);
			} else {
				gr.AddColorStop (0, CairoExtensions.ParseColor ("e9e9e9"));
				gr.AddColorStop (1, CairoExtensions.ParseColor ("d1d1d1"));
			}
			ctx.Pattern = gr;
			ctx.Fill ();

			if (isClosing)
				w = (int)((1d - closingTabAnimationStep) *(double)w);

			ctx.Color = Styles.BreadcrumbBorderColor;
			DrawTabBorder (ctx, TabWidth, x, 0, active);
			ctx.Stroke ();

			x += LeftRightPadding;

			ctx.MoveTo (x, y + TopPadding + ActiveTabVerticalOffset + TextOffset);
			if (active)
				ctx.Color = highlight ? Styles.TabBarHoverActiveTextColor : Styles.TabBarActiveTextColor;
			else
				ctx.Color = highlight ? Styles.TabBarHoverInactiveTextColor : Styles.TabBarInactiveTextColor;
			PangoCairoHelper.ShowLayout (ctx, la);

			x += w;

			var ch = Allocation.Height - TopBarPadding - BottomBarPadding + CloseImageTopOffset + ActiveTabVerticalOffset;
			var crect = new Gdk.Rectangle (x + 3, y + TopBarPadding + (ch - closePix.Height) / 2, closePix.Width, closePix.Height);
			CairoHelper.SetSourcePixbuf (ctx, closePix, crect.X, crect.Y);
			ctx.Paint ();
			crect.Inflate (2, 2);
			tab.CloseButtonAllocation = crect;

			x += closePix.Width + LeftRightPadding;

			if (dragging)
				x = startX + (x - startXDrag);

			la.Dispose ();
		}

		void DrawTabBorder (Cairo.Context ctx, int contentWidth, int px, int margin, bool active = true)
		{
			double x = 0.5 + (double)px;
			double y = (double) Allocation.Height + 0.5 - BottomBarPadding + margin;
			double height = Allocation.Height - TopBarPadding - BottomBarPadding;

			double rightx = x + contentWidth;
			if (active) {
				ctx.MoveTo (0, y);
				ctx.LineTo (x, y);
				ctx.LineTo (x, y - height);
				ctx.LineTo (rightx, y - height);
				ctx.LineTo (rightx, y);
				ctx.LineTo (Allocation.Width, y);
			} else {
				height -= ActiveTabVerticalOffset + 1;
				y--;
				ctx.MoveTo (x, y);
				ctx.LineTo (x, y - height);
				ctx.LineTo (rightx, y - height);
				ctx.LineTo (rightx, y);
			}
		}

		Pango.Layout CreateSizedLayout ()
		{
			Pango.Layout la = new Pango.Layout (PangoContext);
			la.FontDescription = Pango.FontDescription.FromString ("normal");
			la.FontDescription.AbsoluteSize = Pango.Units.FromPixels (VerticalTextSize);

			return la;
		}

		Pango.Layout CreateTabLayout (IDockNotebookTab tab)
		{
			Pango.Layout la = CreateSizedLayout ();
			if (!string.IsNullOrEmpty (tab.Markup))
				la.SetMarkup (tab.Markup);
			else if (!string.IsNullOrEmpty (tab.Text))
				la.SetText (tab.Text);
			return la;
		}
	}
}