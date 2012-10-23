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

		public float WidthModifier { get; set; }

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

		internal Gdk.Rectangle SavedAllocation { get; private set; }
		internal float SaveStrength { get; set; }

		internal void SaveAllocation ()
		{
			SavedAllocation = Allocation;
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
		IDockNotebookTab highlightedTab;
		bool overCloseButton;
		bool buttonPressedOnTab;
		int tabStartX, tabEndX;

		MouseTracker tracker;

		bool draggingTab;
		int dragX;
		int dragOffset;
		float dragXProgress;

		int renderOffset;
		int targetOffset;
		int animationTarget;


		Dictionary<int, DockNotebookTab> closingTabs;

		public Gtk.Button PreviousButton;
		public Gtk.Button NextButton;
		public MenuButton DropDownButton;

		static Gdk.Pixbuf closeSelImage;
		static Gdk.Pixbuf closeSelOverImage;

		const int TopBarPadding = 3;
		const int BottomBarPadding = 3;
		const int LeftRightPadding = 10;
		const int TopPadding = 8;
		const int BottomPadding = 8;
		const int LeftBarPadding = 58;
		const int VerticalTextSize = 11;
		const int TabSpacing = -1;
		const int Radius = 2;
		const int LeanWidth = 18;

		const int ActiveTabVerticalOffset = 2;
		const int TextOffset = -1;

		// Vertically aligns the close image(s) with the tab label.
		const int CloseImageTopOffset = 4;

		int TabWidth { get; set; }
		int LastTabWidthAdjustment { get; set; }

		int targetWidth;
		int TargetWidth {
			get { return targetWidth; }
			set {
				targetWidth = value;
				if (TabWidth != value) {
					this.Animate ("TabWidth",
					              easing: Easing.CubicOut,
					              start: TabWidth,
					              end: value,
					              callback: f => TabWidth = (int) f);
				}
			}
		}

		static TabStrip ()
		{
			try {
				closeSelImage = Gdk.Pixbuf.LoadFromResource ("MonoDevelop.Close.Selected.png");
				closeSelOverImage = Gdk.Pixbuf.LoadFromResource ("MonoDevelop.Close.Selected.Over.png");
			} catch (Exception e) {
				MonoDevelop.Core.LoggingService.LogError ("Can't create pixbuf from resource: MonoDevelop.Close.png", e);
			}
		}

		public TabStrip (DockNotebook notebook)
		{
			TabWidth = 125;
			TargetWidth = 125;
			tracker = new MouseTracker (this);
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

			tracker.HoveredChanged += (sender, e) => {
				if (!tracker.Hovered) {
					highlightedTab = null;
					UpdateTabWidth (tabEndX - tabStartX);
					QueueDraw ();
				}
			};

			closingTabs = new Dictionary<int, DockNotebookTab> ();
		}

		public void StartOpenAnimation (DockNotebookTab tab)
		{
			tab.WidthModifier = 0;
			this.Animate ("Open" + tab.GetHashCode ().ToString (),
			              f => tab.WidthModifier = f,
			              easing: Easing.CubicOut);
		}

		public void StartCloseAnimation (DockNotebookTab closingTab)
		{
			closingTabs[closingTab.Index] = closingTab;
			this.Animate ("Closing" + closingTab.Index,
			              easing: Easing.CubicOut,
			              start: closingTab.WidthModifier,
			              end: 0,
			              callback: f => closingTab.WidthModifier = f,
			              finished: (f, a) => { if (!a) closingTabs.Remove (closingTab.Index); });
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			base.ForAll (include_internals, callback);
			foreach (var c in children)
				callback (c);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			tabStartX = allocation.X + LeftBarPadding + LeanWidth / 2;
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
			get { return totalHeight - BottomBarPadding + 1; }
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
					dragXProgress = 1.0f;
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
					t.SaveAllocation ();
					t.SaveStrength = 1;
					notebook.ReorderTab ((DockNotebookTab)notebook.CurrentTab, t);

					this.Animate ("TabMotion" + t.GetHashCode ().ToString (),
					              f => t.SaveStrength = f,
					              start: 1.0f,
					              end: 0.0f,
					              easing: Easing.CubicInOut);
				}
				lastDragX = (int)evnt.X;
			}

			if (newTooltip != null && TooltipText != null && TooltipText != newTooltip)
				TooltipText = null;
			else
				TooltipText = newTooltip;

			return base.OnMotionNotifyEvent (evnt);
		}

		bool overCloseOnPress;
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			var t = FindTab ((int)evnt.X, (int)evnt.Y);
			if (t != null) {
				if (evnt.IsContextMenuButton ()) {
					notebook.DoPopupMenu (t.Index, evnt);
					return true;
				}
				// Don't select the tab if we are clicking the close button
				if (IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
					overCloseOnPress = true;
					return true;
				}
				overCloseOnPress = false;

				if (evnt.Type == Gdk.EventType.TwoButtonPress) {
					notebook.OnActivateTab (t);
					buttonPressedOnTab = false;
					return true;
				}
				if (evnt.Button == 2) {
					notebook.OnCloseTab (t);
					return true;
				}

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
			if (!draggingTab && overCloseOnPress) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);
				if (t != null && IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
					notebook.OnCloseTab (t);
					return true;
				}
			}
			overCloseOnPress = false;
			this.Animate ("EndDrag",
			              easing: Easing.CubicOut,
			              start: 1.0f,
			              end: 0.0f,
			              callback: f => dragXProgress = f,
			              finished: (f, a) => draggingTab = false);
			QueueDraw ();
			return base.OnButtonReleaseEvent (evnt);
		}

		DockNotebookTab FindTab (int x, int y)
		{
			for (int n = 0; n < notebook.Tabs.Count; n++) {
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
			if (!tracker.Hovered) {
				UpdateTabWidth (tabEndX - tabStartX);
			} else if (closingTabs.ContainsKey (notebook.Tabs.Count)) {
				UpdateTabWidth (closingTabs[notebook.Tabs.Count].Allocation.Right - tabStartX, true);
			}
			QueueDraw ();
		}

		void UpdateTabWidth (int width, bool adjustLast = false)
		{
			if (notebook.Tabs.Any ())
				TargetWidth = Clamp (width / notebook.Tabs.Count, 50, 175);

			if (adjustLast) {
				// adjust to align close buttons properly
				LastTabWidthAdjustment = width - (TargetWidth * notebook.Tabs.Count) + 1;
				LastTabWidthAdjustment = Math.Abs (LastTabWidthAdjustment) < 50 ? LastTabWidthAdjustment : 0;
			} else {
				LastTabWidthAdjustment = 0;
			}
		}

		int Clamp (int val, int min, int max)
		{
			return Math.Max (min, Math.Min (max, val));
		}

		void DrawBackground (Cairo.Context ctx, Gdk.Rectangle region)
		{
			var h = region.Height;
			ctx.Rectangle (0, 0, region.Width, h);
			Cairo.LinearGradient gr = new LinearGradient (0, 0, 0, h);
			gr.AddColorStop (0, Styles.TabBarGradientStartColor);
			gr.AddColorStop (1, Styles.TabBarGradientMidColor);
			ctx.Pattern = gr;
			ctx.Fill ();
			
			ctx.MoveTo (region.X, 0.5);
			ctx.LineTo (region.Right + 1, 0.5);
			ctx.LineWidth = 1;
			ctx.Color = Styles.TabBarGradientShadowColor;
			ctx.Stroke ();
		}

		int GetRenderOffset ()
		{
			int tabArea = tabEndX - tabStartX;
			if (notebook.CurrentTabIndex >= 0) {
				int normalizedArea = (tabArea / TargetWidth) * TargetWidth;
				int maxOffset = Math.Max (0, (notebook.Tabs.Count * TargetWidth) - normalizedArea);

				int distanceToTabEdge = TargetWidth * notebook.CurrentTabIndex;
				int window = normalizedArea - TargetWidth;
				targetOffset = Math.Min (maxOffset, Clamp (renderOffset, distanceToTabEdge - window, distanceToTabEdge));

				if (targetOffset != animationTarget) {
					this.Animate ("ScrollTabs",
					              easing: Easing.CubicOut,
					              start: renderOffset,
					              end: targetOffset,
					              callback: f => renderOffset = (int) f);
					animationTarget = targetOffset;
				}
			}
			
			return tabStartX - renderOffset;
		}

		int DrawClosingTab (Cairo.Context context, int index, Gdk.Rectangle region)
		{
			int width = 0;
			if (closingTabs.ContainsKey (index)) {
				DockNotebookTab closingTab = closingTabs[index];
				width = (int) (closingTab.WidthModifier * TabWidth);
				DrawTab (context, closingTab, new Gdk.Rectangle (region.X, region.Y, width, region.Height), false, false, false);
			}
			return width;
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				DrawBackground (ctx, Allocation);
			}

			// Draw buttons
			foreach (var c in children)
				PropagateExpose (c, evnt);

			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				// Draw breadcrumb bar header
				if (notebook.Tabs.Count > 0) {
					ctx.Rectangle (0, Allocation.Height - BottomBarPadding, Allocation.Width, BottomBarPadding);
					ctx.Color = Styles.BreadcrumbBackgroundColor;
					ctx.Fill ();
				}

				int tabArea = tabEndX - tabStartX;
				int x = GetRenderOffset ();
				int y = 0;

				ctx.Rectangle (tabStartX - LeanWidth / 2, Allocation.Y, tabArea + LeanWidth, Allocation.Height);
				ctx.Clip ();
				int n = 0;
				Action<Cairo.Context> drawActive = c => {};
				List<Action<Cairo.Context>> drawCommands = new List<Action<Context>> ();
				for (; n < notebook.Tabs.Count; n++) {
					if (x + TabWidth < tabStartX) {
						x += TabWidth;
						continue;
					}

					if (x > tabEndX)
						break;

					x += DrawClosingTab (ctx, n, new Gdk.Rectangle (x, y, 0, Allocation.Height));
					var tab = (DockNotebookTab)notebook.Tabs [n];

					bool active = tab == notebook.CurrentTab;

					int width = Math.Min (TabWidth, Math.Max (50, tabEndX - x - 1));
					if (tab == notebook.Tabs.Last ())
						width += LastTabWidthAdjustment;
					width = (int) (width * tab.WidthModifier);

					if (active) {
						int tmp = x;
						drawActive = c => DrawTab (c, tab, new Gdk.Rectangle (tmp, y, width, Allocation.Height), true, true, draggingTab);
					} else {
						int tmp = x;
						bool highlighted = tab == highlightedTab;

						if (tab.SaveStrength > 0.0f) {
							tmp = (int) (tab.SavedAllocation.X + (tmp - tab.SavedAllocation.X) * (1.0f - tab.SaveStrength));
						}

						drawCommands.Add (c => DrawTab (c, tab, new Gdk.Rectangle (tmp, y, width, Allocation.Height), highlighted, false, false));
					}
					tab.Allocation = new Gdk.Rectangle (x, Allocation.Y, width, Allocation.Height);

					x += width;
				}

				drawCommands.Reverse ();
				foreach (var cmd in drawCommands)
					cmd (ctx);

				// make sure we got a final closing tab if it exists
				DrawClosingTab (ctx, n, new Gdk.Rectangle (x, y, 0, Allocation.Height));
				ctx.ResetClip ();

				// Redraw the dragging tab here to be sure its on top. We drew it before to get the sizing correct, this should be fixed.
				drawActive (ctx);
			}
			return base.OnExposeEvent (evnt);
		}

		void DrawTab (Cairo.Context ctx, DockNotebookTab tab, Gdk.Rectangle region, bool highlight, bool active, bool dragging)
		{
			// This logic is stupid to have here, should be in the caller!
			if (dragging) {
				region.X = (int) (region.X + (dragX - region.X) * dragXProgress);
				region.X = Clamp (region.X, tabStartX, tabEndX - region.Width);
			}

			int padding = LeftRightPadding;
			padding = (int) (padding * Math.Min (1.0, Math.Max (0.5, (region.Width - 30) / 70.0)));


			ctx.LineWidth = 1;
			DrawTabBorder (ctx, region.Width, region.X, 0, active);
			ctx.ClosePath ();
			Cairo.LinearGradient gr = new LinearGradient (region.X, TopBarPadding, region.X, Allocation.Bottom);
			if (active) {
				gr.AddColorStop (0, Styles.BreadcrumbGradientStartColor);
				gr.AddColorStop (1, Styles.BreadcrumbBackgroundColor);
			} else {
				gr.AddColorStop (0, CairoExtensions.ParseColor ("f4f4f4"));
				gr.AddColorStop (1, CairoExtensions.ParseColor ("cecece"));
			}
			ctx.Pattern = gr;
			ctx.Fill ();
			
			ctx.Color = new Cairo.Color (1, 1, 1, .5);
			DrawTabBorder (ctx, region.Width, region.X, 1, active);
			ctx.Stroke ();

			ctx.Color = Styles.BreadcrumbBorderColor;
			DrawTabBorder (ctx, region.Width, region.X, 0, active);
			ctx.Stroke ();

			// Render Close Button (do this first so we can tell how much text to render)
			
			var ch = Allocation.Height - TopBarPadding - BottomBarPadding + CloseImageTopOffset + ActiveTabVerticalOffset;
			var crect = new Gdk.Rectangle (region.Right - padding - closeSelImage.Width + 3, 
			                               region.Y + TopBarPadding + (ch - closeSelImage.Height) / 2, 
			                               closeSelImage.Width, closeSelImage.Height);
			tab.CloseButtonAllocation = crect;
			tab.CloseButtonAllocation.Inflate (2, 2);

			bool closeButtonHovered = tracker.Hovered && tab.CloseButtonAllocation.Contains (tracker.MousePosition) && tab.WidthModifier >= 1.0f;
			bool drawCloseButton = region.Width > 60 || highlight || closeButtonHovered;
			if (drawCloseButton) {
				var closePix = closeButtonHovered ? closeSelOverImage : closeSelImage;
				CairoHelper.SetSourcePixbuf (ctx, closePix, crect.X, crect.Y);
				ctx.Paint ();
			}

			// Render Text

			var la = CreateTabLayout (tab);
			int w = region.Width - (padding * 2 + closeSelImage.Width);
			if (!drawCloseButton)
				w += closeSelImage.Width;

			int textStart = region.X + padding;

			ctx.MoveTo (textStart, region.Y + TopPadding + ActiveTabVerticalOffset + TextOffset + VerticalTextSize);
			// ellipses are for space wasting ..., we cant afford that
			using (var lg = new LinearGradient (textStart + w - 5, 0, textStart + w + 3, 0)) {
				var color = Styles.TabBarActiveTextColor;
				lg.AddColorStop (0, color);
				color.A = 0;
				lg.AddColorStop (1, color);
				ctx.Pattern = lg;
				Pango.CairoHelper.ShowLayoutLine (ctx, la.GetLine (0));
			}
			la.Dispose ();


		}

		void DrawTabBorder (Cairo.Context ctx, int contentWidth, int px, int margin, bool active = true)
		{
			double x = 0.5 + (double)px;
			double y = (double) Allocation.Height + 0.5 - BottomBarPadding + margin;
			double height = Allocation.Height - TopBarPadding - BottomBarPadding;

			x += TabSpacing + margin;
			contentWidth -= (TabSpacing + margin) * 2;

			double rightx = x + contentWidth;

			int lean = LeanWidth;
			int halfLean = lean / 2;
			int smoothing = 2;
			if (active) {
				ctx.MoveTo (0, y + 0.5);
				ctx.LineTo (0, y);
				ctx.LineTo (x - halfLean, y);
			} else {
				ctx.MoveTo (x - halfLean, y + 0.5);
				ctx.LineTo (x - halfLean, y);
			}
			ctx.CurveTo (new PointD (x + smoothing, y),
			             new PointD (x - smoothing, y - height),
			             new PointD (x + halfLean, y - height));
			ctx.LineTo (rightx - halfLean, y - height);
			ctx.CurveTo (new PointD (rightx + smoothing, y - height),
			             new PointD (rightx - smoothing, y),
			             new PointD (rightx + halfLean, y));

			if (active) {
				ctx.LineTo (Allocation.Width, y);
				ctx.LineTo (Allocation.Width, y + 0.5);
			} else {
				ctx.LineTo (rightx + halfLean, y + 0.5);
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