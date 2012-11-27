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
		bool Notify { get; set; }
		bool Hidden { get; set; }
		bool Dirty { get; set; }
	}

	internal class DockNotebookTab: IDockNotebookTab, Animatable
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

		public bool Notify { get; set; }

		public float WidthModifier { get; set; }

		public float Opacity { get; set; }

		public float GlowStrength { get; set; }

		public bool Hidden { get; set; }

		public float DirtyStrength { get; set; }

		bool dirty;
		public bool Dirty {
			get { return dirty; }
			set { 
				if (dirty == value)
					return;
				dirty = value;
				this.Animate ("Dirty", (f) => DirtyStrength = f,
				              easing: Easing.CubicInOut,
				              start: DirtyStrength, end: value ? 1 : 0);
			}
		}



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

		public void QueueDraw ()
		{
			strip.QueueDraw ();
		}
	}

	class TabEventArgs: EventArgs
	{
		public DockNotebookTab Tab { get; set; }
	}

	class TabStrip: EventBox, Animatable
	{
		List<Gtk.Widget> children = new List<Widget> ();
		DockNotebook notebook;
		DockNotebookTab highlightedTab;
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
		const int CloseButtonSize = 14;

		const int TextOffset = 1;

		// Vertically aligns the close image(s) with the tab label.
		const int CloseImageTopOffset = 3;

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
					SetHighlightedTab (null);
					UpdateTabWidth (tabEndX - tabStartX);
					QueueDraw ();
				}
			};

			notebook.PageAdded += (object sender, EventArgs e) => QueueResize ();
			notebook.PageRemoved += (object sender, EventArgs e) => QueueResize ();

			closingTabs = new Dictionary<int, DockNotebookTab> ();
		}

		public void StartOpenAnimation (DockNotebookTab tab)
		{
			tab.WidthModifier = 0;
			new Animation (f => tab.WidthModifier = f)
				.Insert (0.0f, 0.2f, new Animation (f => tab.Opacity = f))
				.Commit (tab, "Open", easing: Easing.CubicInOut);
		}

		public void StartCloseAnimation (DockNotebookTab tab)
		{
			closingTabs[tab.Index] = tab;
			new Animation (f => tab.WidthModifier = f, tab.WidthModifier, 0)
				.Insert (0.8f, 1.0f, new Animation (f => tab.Opacity = f, tab.Opacity, 0))
				.Commit (tab, "Closing", 
					     easing: Easing.CubicOut,
					     finished: (f, a) => { if (!a) closingTabs.Remove (tab.Index); });
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
			var height = allocation.Height - BottomBarPadding;
			if (height < 0)
				height = 0;

			PreviousButton.SizeAllocate (new Gdk.Rectangle (
				allocation.X,
				allocation.Y,
				LeftBarPadding / 2,
				height
				)
			                             );
			NextButton.SizeAllocate (new Gdk.Rectangle (
				allocation.X + LeftBarPadding / 2,
				allocation.Y,
				LeftBarPadding / 2, height)
			                         );

			DropDownButton.SizeAllocate (new Gdk.Rectangle (
				tabEndX,
				allocation.Y,
				DropDownButton.SizeRequest ().Width,
				height));

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

		void SetHighlightedTab (DockNotebookTab tab)
		{
			if (highlightedTab == tab)
				return;

			if (highlightedTab != null) {
				var tmp = highlightedTab;
				tmp.Animate ("Glow",
				             f => tmp.GlowStrength = f,
				             start: tmp.GlowStrength,
				             end: 0);
			}

			if (tab != null) {
				tab.Animate ("Glow",
				             f => tab.GlowStrength = f,
				             start: tab.GlowStrength,
				             end: 1);
			}

			highlightedTab = tab;
			QueueDraw ();
		}

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			string newTooltip = null;

			if (!draggingTab) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);

				// If the user clicks and drags on the 'x' which closes the current
				// tab we can end up with a null tab here
				if (t == null)
					return base.OnMotionNotifyEvent (evnt);;
				SetHighlightedTab (t);

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
			} else if (evnt.State.HasFlag (ModifierType.Button1Mask)) {
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

					t.Animate ("TabMotion",
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
		bool allowDoubleClick;
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
					if (allowDoubleClick) {
						notebook.OnActivateTab (t);
						buttonPressedOnTab = false;
					}
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
					allowDoubleClick = false;
					return true;
				}
			}
			overCloseOnPress = false;
			allowDoubleClick = true;
			if (dragX != 0)
				this.Animate ("EndDrag",
				              easing: Easing.CubicOut,
				              start: 1.0f,
				              end: 0.0f,
				              callback: f => dragXProgress = f,
				              finished: (f, a) => draggingTab = false);
			QueueDraw ();
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override void OnUnrealized ()
		{
			// Cancel drag operations and animations
			buttonPressedOnTab = false;
			overCloseOnPress = false;
			allowDoubleClick = true;
			draggingTab = false;
			dragX = 0;
			this.AbortAnimation ("EndDrag");
			base.OnUnrealized ();
		}

		DockNotebookTab FindTab (int x, int y)
		{
			// we will not actually draw anything, just do bounds checking
			using (var context = Gdk.CairoHelper.Create (GdkWindow)) {
				DockNotebookTab current = notebook.CurrentTab as DockNotebookTab;
				if (current != null) {
					LayoutTabBorder (context, Allocation, current.Allocation.Width, current.Allocation.X, 0, false);
					if (context.InFill (x, y))
						return current;
				}

				context.NewPath ();
				for (int n = 0; n < notebook.Tabs.Count; n++) {
					DockNotebookTab tab = (DockNotebookTab) notebook.Tabs[n];
					LayoutTabBorder (context, Allocation, tab.Allocation.Width, tab.Allocation.X, 0, false);
					if (context.InFill (x, y))
						return tab;
					context.NewPath ();
				}
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
				TargetWidth = Clamp (width / notebook.Tabs.Count, 50, 200);

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
			using (Cairo.LinearGradient gr = new LinearGradient (0, 0, 0, h)) {
				gr.AddColorStop (0, Styles.TabBarGradientStartColor);
				gr.AddColorStop (1, Styles.TabBarGradientMidColor);
				ctx.Pattern = gr;
				ctx.Fill ();
			}
			
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

		Action<Cairo.Context> DrawClosingTab (int index, Gdk.Rectangle region, out int width)
		{
			width = 0;
			if (closingTabs.ContainsKey (index)) {
				DockNotebookTab closingTab = closingTabs[index];
				width = (int) (closingTab.WidthModifier * TabWidth);
				int tmp = width;
				return (c) => DrawTab (c, closingTab, Allocation, new Gdk.Rectangle (region.X, region.Y, tmp, region.Height), false, false, false, CreateTabLayout (closingTab));
			}
			return (c) => {};
		}

		void Draw (Cairo.Context ctx)
		{
			int tabArea = tabEndX - tabStartX;
			int x = GetRenderOffset ();
			int y = 0;
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

				int closingWidth;
				var cmd = DrawClosingTab (n, new Gdk.Rectangle (x, y, 0, Allocation.Height), out closingWidth);
				drawCommands.Add (cmd);
				x += closingWidth;

				var tab = (DockNotebookTab)notebook.Tabs [n];
				bool active = tab == notebook.CurrentTab;

				int width = Math.Min (TabWidth, Math.Max (50, tabEndX - x - 1));
				if (tab == notebook.Tabs.Last ())
					width += LastTabWidthAdjustment;
				width = (int) (width * tab.WidthModifier);

				if (active) {
					int tmp = x;
					drawActive = c => DrawTab (c, tab, Allocation, new Gdk.Rectangle (tmp, y, width, Allocation.Height), true, true, draggingTab, CreateTabLayout (tab));
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				} else {
					int tmp = x;
					bool highlighted = tab == highlightedTab;

					if (tab.SaveStrength > 0.0f) {
						tmp = (int) (tab.SavedAllocation.X + (tmp - tab.SavedAllocation.X) * (1.0f - tab.SaveStrength));
					}

					drawCommands.Add (c => DrawTab (c, tab, Allocation, new Gdk.Rectangle (tmp, y, width, Allocation.Height), highlighted, false, false, CreateTabLayout (tab)));
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				}

				x += width;
			}

			Gdk.Rectangle allocation = Allocation;
			int tabWidth;
			drawCommands.Add (DrawClosingTab (n, new Gdk.Rectangle (x, y, 0, allocation.Height), out tabWidth));
			drawCommands.Reverse ();

			DrawBackground (ctx, allocation);

			// Draw breadcrumb bar header
			if (notebook.Tabs.Count > 0) {
				ctx.Rectangle (0, allocation.Height - BottomBarPadding, allocation.Width, BottomBarPadding);
				ctx.Color = Styles.BreadcrumbBackgroundColor;
				ctx.Fill ();
			}

			ctx.Rectangle (tabStartX - LeanWidth / 2, allocation.Y, tabArea + LeanWidth, allocation.Height);
			ctx.Clip ();

			foreach (var cmd in drawCommands)
				cmd (ctx);

			ctx.ResetClip ();

			// Redraw the dragging tab here to be sure its on top. We drew it before to get the sizing correct, this should be fixed.
			drawActive (ctx);
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				Draw (context);
			}
			return base.OnExposeEvent (evnt);
		}

		void DrawCloseButton (Cairo.Context context, Gdk.Point center, bool hovered, double opacity, double animationProgress)
		{
			if (hovered) {
				double radius = 6;
				context.Arc (center.X, center.Y, radius, 0, Math.PI * 2);
				context.Color = new Cairo.Color (.6, .6, .6, opacity);
				context.Fill ();

				context.Color = new Cairo.Color (0.95, 0.95, 0.95, opacity);
				context.LineWidth = 2;

				context.MoveTo (center.X - 3, center.Y - 3);
				context.LineTo (center.X + 3, center.Y + 3);
				context.MoveTo (center.X - 3, center.Y + 3);
				context.LineTo (center.X + 3, center.Y - 3);
				context.Stroke ();
			} else {
				double lineColor = .63 - .1 * animationProgress;
				double fillColor = .74;
				
				double heightMod = Math.Max (0, 1.0 - animationProgress * 2);
				context.MoveTo (center.X - 3, center.Y - 3 * heightMod);
				context.LineTo (center.X + 3, center.Y + 3 * heightMod);
				context.MoveTo (center.X - 3, center.Y + 3 * heightMod);
				context.LineTo (center.X + 3, center.Y - 3 * heightMod);
				
				context.LineWidth = 2;
				context.Color = new Cairo.Color (lineColor, lineColor, lineColor, opacity);
				context.Stroke ();
				
				if (animationProgress > 0.5) {
					double partialProg = (animationProgress - 0.5) * 2;
					context.MoveTo (center.X - 3, center.Y);
					context.LineTo (center.X + 3, center.Y);
					
					context.LineWidth = 2 - partialProg;
					context.Color = new Cairo.Color (lineColor, lineColor, lineColor, opacity);
					context.Stroke ();
					
					
					double radius = partialProg * 3.5;

					// Background
					context.Arc (center.X, center.Y, radius, 0, Math.PI * 2);
					context.Color = new Cairo.Color (fillColor, fillColor, fillColor, opacity);
					context.Fill ();

					// Inset shadow
					using (var lg = new Cairo.LinearGradient (0, center.Y - 5, 0, center.Y)) {
						context.Arc (center.X, center.Y + 1, radius, 0, Math.PI * 2);
						lg.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.2 * opacity));
						lg.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
						context.Pattern = lg;
						context.Stroke ();
					}

					// Outline
					context.Arc (center.X, center.Y, radius, 0, Math.PI * 2);
					context.Color = new Cairo.Color (lineColor, lineColor, lineColor, opacity);
					context.Stroke ();

				}
			}
		}

		void DrawTab (Cairo.Context ctx, DockNotebookTab tab, Gdk.Rectangle allocation, Gdk.Rectangle tabBounds, bool highlight, bool active, bool dragging, Pango.Layout la)
		{
			// This logic is stupid to have here, should be in the caller!
			if (dragging) {
				tabBounds.X = (int) (tabBounds.X + (dragX - tabBounds.X) * dragXProgress);
				tabBounds.X = Clamp (tabBounds.X, tabStartX, tabEndX - tabBounds.Width);
			}
			int padding = LeftRightPadding;
			padding = (int) (padding * Math.Min (1.0, Math.Max (0.5, (tabBounds.Width - 30) / 70.0)));


			ctx.LineWidth = 1;
			LayoutTabBorder (ctx, allocation, tabBounds.Width, tabBounds.X, 0, active);
			ctx.ClosePath ();
			Cairo.LinearGradient gr = new LinearGradient (tabBounds.X, TopBarPadding, tabBounds.X, allocation.Bottom);
			if (active) {
				gr.AddColorStop (0, Styles.BreadcrumbGradientStartColor.MultiplyAlpha (tab.Opacity));
				gr.AddColorStop (1, Styles.BreadcrumbBackgroundColor.MultiplyAlpha (tab.Opacity));
			} else {
				gr.AddColorStop (0, CairoExtensions.ParseColor ("f4f4f4").MultiplyAlpha (tab.Opacity));
				gr.AddColorStop (1, CairoExtensions.ParseColor ("cecece").MultiplyAlpha (tab.Opacity));
			}
			ctx.Pattern = gr;
			ctx.Fill ();
			
			ctx.Color = new Cairo.Color (1, 1, 1, .5).MultiplyAlpha (tab.Opacity);
			LayoutTabBorder (ctx, allocation, tabBounds.Width, tabBounds.X, 1, active);
			ctx.Stroke ();

			ctx.Color = Styles.BreadcrumbBorderColor.MultiplyAlpha (tab.Opacity);
			LayoutTabBorder (ctx, allocation, tabBounds.Width, tabBounds.X, 0, active);
			ctx.StrokePreserve ();

			if (tab.GlowStrength > 0) {
				Gdk.Point mouse = tracker.MousePosition;
				using (var rg = new RadialGradient (mouse.X, tabBounds.Bottom, 0, mouse.X, tabBounds.Bottom, 100)) {
					rg.AddColorStop (0, new Cairo.Color (1, 1, 1, 0.4 * tab.Opacity * tab.GlowStrength));
					rg.AddColorStop (1, new Cairo.Color (1, 1, 1, 0));
					
					ctx.Pattern = rg;
					ctx.Fill ();
				}
			} else {
				ctx.NewPath ();
			}

			// Render Close Button (do this first so we can tell how much text to render)
			
			var ch = allocation.Height - TopBarPadding - BottomBarPadding + CloseImageTopOffset;
			var crect = new Gdk.Rectangle (tabBounds.Right - padding - CloseButtonSize + 3, 
			                               tabBounds.Y + TopBarPadding + (ch - CloseButtonSize) / 2, 
			                               CloseButtonSize, CloseButtonSize);
			tab.CloseButtonAllocation = crect;
			tab.CloseButtonAllocation.Inflate (2, 2);

			bool closeButtonHovered = tracker.Hovered && tab.CloseButtonAllocation.Contains (tracker.MousePosition) && tab.WidthModifier >= 1.0f;
			bool drawCloseButton = tabBounds.Width > 60 || highlight || closeButtonHovered;
			if (drawCloseButton) {
				DrawCloseButton (ctx, new Gdk.Point (crect.X + crect.Width / 2, crect.Y + crect.Height / 2), closeButtonHovered, tab.Opacity, tab.DirtyStrength);
			}

			// Render Text
			int w = tabBounds.Width - (padding * 2 + CloseButtonSize);
			if (!drawCloseButton)
				w += CloseButtonSize;

			int textStart = tabBounds.X + padding;

			ctx.MoveTo (textStart, tabBounds.Y + TopPadding + TextOffset + VerticalTextSize);
			// ellipses are for space wasting ..., we cant afford that
			using (var lg = new LinearGradient (textStart + w - 5, 0, textStart + w + 3, 0)) {
				var color = tab.Notify ? new Cairo.Color (0, 0, 1) : Styles.TabBarActiveTextColor;
				color = color.MultiplyAlpha (tab.Opacity);
				lg.AddColorStop (0, color);
				color.A = 0;
				lg.AddColorStop (1, color);
				ctx.Pattern = lg;
				Pango.CairoHelper.ShowLayoutLine (ctx, la.GetLine (0));
			}
			la.Dispose ();
		}

		void LayoutTabBorder (Cairo.Context ctx, Gdk.Rectangle allocation, int contentWidth, int px, int margin, bool active = true)
		{
			double x = 0.5 + (double)px;
			double y = (double) allocation.Height + 0.5 - BottomBarPadding + margin;
			double height = allocation.Height - TopBarPadding - BottomBarPadding;

			x += TabSpacing + margin;
			contentWidth -= (TabSpacing + margin) * 2;

			double rightx = x + contentWidth;

			int lean = Math.Min (LeanWidth, contentWidth / 2);
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
				ctx.LineTo (allocation.Width, y);
				ctx.LineTo (allocation.Width, y + 0.5);
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
