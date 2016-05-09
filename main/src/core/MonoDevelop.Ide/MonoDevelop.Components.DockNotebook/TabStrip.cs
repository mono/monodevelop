//
// TabStrip.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.Linq;
using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using Cairo;
using MonoDevelop.Components;
using Xwt.Motion;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.DockNotebook
{
	class TabStrip: EventBox, IAnimatable
	{
		static Xwt.Drawing.Image tabbarPrevImage = Xwt.Drawing.Image.FromResource ("tabbar-prev-12.png");
		static Xwt.Drawing.Image tabbarNextImage = Xwt.Drawing.Image.FromResource ("tabbar-next-12.png");
		static Xwt.Drawing.Image tabActiveBackImage = Xwt.Drawing.Image.FromResource ("tabbar-active.9.png");
		static Xwt.Drawing.Image tabPinnedActiveBackImage = Xwt.Drawing.Image.FromResource ("tabbar-pinned.9.png");
		static Xwt.Drawing.Image tabPinnedBackImage = Xwt.Drawing.Image.FromResource ("tabbar-inactive-pinned.9.png");
		static Xwt.Drawing.Image tabBackImage = Xwt.Drawing.Image.FromResource ("tabbar-inactive.9.png");
		static Xwt.Drawing.Image tabbarBackImage = Xwt.Drawing.Image.FromResource ("tabbar-back.9.png");
		static Xwt.Drawing.Image tabCloseImage = Xwt.Drawing.Image.FromResource ("tab-close-9.png");
		static Xwt.Drawing.Image tabPinnedImage = Xwt.Drawing.Image.FromResource ("tab-pinned-9.png");
		static Xwt.Drawing.Image tabUnPinnedImage = Xwt.Drawing.Image.FromResource ("tab-unpinned-9.png");
		static Xwt.Drawing.Image tabDirtyImage = Xwt.Drawing.Image.FromResource ("tab-dirty-9.png");

		List<Widget> children = new List<Widget> ();
		readonly DockNotebook notebook;
		DockNotebookTab highlightedTab;
		bool overCloseButton;
		bool buttonPressedOnTab;
		int tabStartX, tabEndX;
		bool isActiveNotebook;

		MouseTracker tracker;

		bool draggingTab;
		int dragX;
		int dragOffset;
		double dragXProgress;

		int renderOffset;
		int targetOffset;
		int animationTarget;

		Dictionary<int, DockNotebookTab> closingTabs;

		public Button PreviousButton;
		public Button NextButton;
		public MenuButton DropDownButton;

		static readonly int TotalHeight = 32;
		static readonly Xwt.WidgetSpacing TabPadding;
		static readonly Xwt.WidgetSpacing TabActivePadding;
		static readonly int LeftBarPadding = 44;
		static readonly int RightBarPadding = 22;
		static readonly int VerticalTextSize = 11;
		const int TabSpacing = 0;
		const int LeanWidth = 12;
		const int ButtonSize = 14;
		const double CloseButtonMarginRight = 0;
		const double CloseButtonMarginBottom = -1.0;
		const double PinButtonMarginRight = 0;
		const double PinButtonMarginBottom = -1.0;

		const int TextOffset = 1;

		int TabWidth { get; set; }

		int LastTabWidthAdjustment { get; set; }

		int targetWidth;

		int TargetWidth {
			get { return targetWidth; }
			set {
				targetWidth = value;
				if (TabWidth != value) {
					this.Animate ("TabWidth",
						f => TabWidth = (int)f,
						TabWidth,
						value,
						easing: Easing.CubicOut);
				}
			}
		}

		public bool  NavigationButtonsVisible {
			get { return children.Contains (PreviousButton); }
			set {
				if (value == NavigationButtonsVisible)
					return;
				if (value) {
					children.Add (NextButton);
					children.Add (PreviousButton);
					OnSizeAllocated (Allocation);
					PreviousButton.ShowAll ();
					NextButton.ShowAll ();
				} else {
					children.Remove (PreviousButton);
					children.Remove (NextButton);
					OnSizeAllocated (Allocation);
				}
			}
		}

		static TabStrip ()
		{
			Xwt.Drawing.NinePatchImage tabBackImage9;
			if (tabBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)tabBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabBackImage9 = tabBackImage as Xwt.Drawing.NinePatchImage;
			TabPadding = tabBackImage9.Padding;


			Xwt.Drawing.NinePatchImage tabActiveBackImage9;
			if (tabActiveBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)tabActiveBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabActiveBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabActiveBackImage9 = tabBackImage as Xwt.Drawing.NinePatchImage;
			TabActivePadding = tabActiveBackImage9.Padding;
		}

		public TabStrip (DockNotebook notebook)
		{
			if (notebook == null)
				throw new ArgumentNullException ("notebook");
			TabWidth = 125;
			TargetWidth = 125;
			tracker = new MouseTracker (this);
			GtkWorkarounds.FixContainerLeak (this);

			this.notebook = notebook;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			Events |= EventMask.PointerMotionMask | EventMask.LeaveNotifyMask | EventMask.ButtonPressMask;

			var arr = new Xwt.ImageView (tabbarPrevImage);
			arr.HeightRequest = arr.WidthRequest = 10;

			var alignment = new Alignment (0.5f, 1, 0.0f, 0.0f);
			alignment.Add (arr.ToGtkWidget ());
			PreviousButton = new Button (alignment);
			PreviousButton.TooltipText = Core.GettextCatalog.GetString ("Switch to previous document");
			PreviousButton.Relief = ReliefStyle.None;
			PreviousButton.CanDefault = PreviousButton.CanFocus = false;

			arr = new Xwt.ImageView (tabbarNextImage);
			arr.HeightRequest = arr.WidthRequest = 10;

			alignment = new Alignment (0.5f, 1, 0.0f, 0.0f);
			alignment.Add (arr.ToGtkWidget ());
			NextButton = new Button (alignment);
			NextButton.TooltipText = Core.GettextCatalog.GetString ("Switch to next document");
			NextButton.Relief = ReliefStyle.None;
			NextButton.CanDefault = NextButton.CanFocus = false;

			DropDownButton = new MenuButton ();
			DropDownButton.TooltipText = Core.GettextCatalog.GetString ("Document List"); 
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

			notebook.PageAdded += (sender, e) => QueueResize ();
			notebook.PageRemoved += (sender, e) => QueueResize ();

			closingTabs = new Dictionary<int, DockNotebookTab> ();
		}

		void IAnimatable.BatchBegin ()
		{
		}

		void IAnimatable.BatchCommit ()
		{
			QueueDraw ();
		}

		public bool IsActiveNotebook {
			get {
				return this.isActiveNotebook;
			}
			set {
				isActiveNotebook = value;
				QueueDraw ();
			}
		}

		public void StartOpenAnimation (DockNotebookTab tab)
		{
			tab.WidthModifier = 0;
			new Animation (f => tab.WidthModifier = f)
				.AddConcurrent (new Animation (f => tab.Opacity = f), 0.0d, 0.2d)
				.Commit (tab, "Open", easing: Easing.CubicInOut);
		}

		public void StartCloseAnimation (DockNotebookTab tab)
		{
			closingTabs [tab.Index] = tab;
			new Animation (f => tab.WidthModifier = f, tab.WidthModifier, 0)
				.AddConcurrent (new Animation (f => tab.Opacity = f, tab.Opacity, 0), 0.8d)
				.Commit (tab, "Closing",
				easing: Easing.CubicOut,
				finished: (f, a) => {
					if (!a)
						closingTabs.Remove (tab.Index);
				});
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			base.ForAll (include_internals, callback);
			foreach (var c in children.ToArray ())
				callback (c);
		}

		protected override void OnRemoved (Widget widget)
		{
			children.Remove (widget);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (NavigationButtonsVisible) {
				tabStartX = /*allocation.X +*/ LeftBarPadding + LeanWidth / 2;
			} else {
				tabStartX = LeanWidth / 2;
			}
			tabEndX = allocation.Width - RightBarPadding;
			var height = allocation.Height;

			PreviousButton.SizeAllocate (new Gdk.Rectangle (
				0, // allocation.X,
				0, // allocation.Y,
				LeftBarPadding / 2,
				height
			)
			);
			NextButton.SizeAllocate (new Gdk.Rectangle (
				LeftBarPadding / 2,
				0,
				LeftBarPadding / 2, height)
			);

			var image = PreviousButton.Child;
			int buttonWidth = LeftBarPadding / 2;
			image.SizeAllocate (new Gdk.Rectangle (
				(buttonWidth - 12) / 2,
				(height - 12) / 2,
				12, 12)
			);

			image = NextButton.Child;
			image.SizeAllocate (new Gdk.Rectangle (
				buttonWidth + (buttonWidth - 12) / 2,
				(height - 12) / 2,
				12, 12)
			);

			DropDownButton.SizeAllocate (new Gdk.Rectangle (
				tabEndX,
				allocation.Y,
				DropDownButton.SizeRequest ().Width,
				height));

			base.OnSizeAllocated (allocation);
			Update ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Height = TotalHeight;
			requisition.Width = 0;
		}

		internal void InitSize ()
		{
			return;
		}

		public int BarHeight {
			get { return TotalHeight; }
		}

		int lastDragX;

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

		class PadTitleWindow: Gtk.Window
		{
			public PadTitleWindow (DockFrame frame, DockItem draggedItem) : base (Gtk.WindowType.Popup)
			{
				SkipTaskbarHint = true;
				Decorated = false;
				TransientFor = (Gtk.Window)frame.Toplevel;
				TypeHint = WindowTypeHint.Utility;

				var mainBox = new VBox ();

				var box = new HBox (false, 3);
				if (draggedItem.Icon != null) {
					var img = new Xwt.ImageView (draggedItem.Icon);
					box.PackStart (img.ToGtkWidget (), false, false, 0);
				}
				var la = new Label ();
				la.Markup = draggedItem.Label;
				box.PackStart (la, false, false, 0);

				mainBox.PackStart (box, false, false, 0);

				var f = new CustomFrame ();
				f.SetPadding (12, 12, 12, 12);
				f.SetMargins (1, 1, 1, 1);
				f.Add (mainBox);

				Add (f);
				ShowAll ();
			}
		}

		PlaceholderWindow placeholderWindow;
		bool mouseHasLeft;

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			if (draggingTab && placeholderWindow == null && !mouseHasLeft)
				mouseHasLeft = true;
			return base.OnLeaveNotifyEvent (evnt);
		}

		void CreatePlaceholderWindow ()
		{
			var tab = notebook.CurrentTab;
			placeholderWindow = new PlaceholderWindow (tab);

			int x, y;
			Gdk.Display.Default.GetPointer (out x, out y);
			placeholderWindow.MovePosition (x, y);
			placeholderWindow.Show ();

			placeholderWindow.Destroyed += delegate {
				placeholderWindow = null;
				buttonPressedOnTab = false;
			};
		}

		Gdk.Rectangle GetScreenRect ()
		{
			int ox, oy;
			ParentWindow.GetOrigin (out ox, out oy);
			var alloc = notebook.Allocation;
			alloc.X += ox;
			alloc.Y += oy;
			return alloc;
		}

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (draggingTab && mouseHasLeft) {
				var sr = GetScreenRect ();
				sr.Height = BarHeight;
				sr.Inflate (30, 30);

				int x, y;
				Gdk.Display.Default.GetPointer (out x, out y);

				if (x < sr.Left || x > sr.Right || y < sr.Top || y > sr.Bottom) {
					draggingTab = false;
					mouseHasLeft = false;
					CreatePlaceholderWindow ();
				}
			}

			string newTooltip = null;
			if (placeholderWindow != null) {
				int x, y;
				Gdk.Display.Default.GetPointer (out x, out y);
				placeholderWindow.MovePosition (x, y);
				return base.OnMotionNotifyEvent (evnt);
			}

			if (!draggingTab) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);

				// If the user clicks and drags on the 'x' which closes the current
				// tab we can end up with a null tab here
				if (t == null) {
					TooltipText = null;
					return base.OnMotionNotifyEvent (evnt);
				}
				SetHighlightedTab (t);

				var newOver = IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y);
				if (newOver != overCloseButton) {
					overCloseButton = newOver;
					QueueDraw ();
				}
				if (!overCloseButton && !draggingTab && buttonPressedOnTab) {
					draggingTab = true;
					mouseHasLeft = false;
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

				var t = FindTab ((int)evnt.X, (int)TabPadding.Top + 3);
				if (t == null) {
					var last = (DockNotebookTab)notebook.Tabs.Last ();
					if (dragX > last.Allocation.Right)
						t = last;
					if (dragX < 0)
						t = (DockNotebookTab)notebook.Tabs.First ();
				}
				if (t != null && t != notebook.CurrentTab && (
				        ((int)evnt.X > lastDragX && t.Index > notebook.CurrentTab.Index) ||
				        ((int)evnt.X < lastDragX && t.Index < notebook.CurrentTab.Index))) {
					t.SaveAllocation ();
					t.SaveStrength = 1;
					notebook.ReorderTab ((DockNotebookTab)notebook.CurrentTab, t);

					t.Animate ("TabMotion",
						f => t.SaveStrength = f,
						1.0f,
						0.0f,
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

		bool overPinOnPress;
		bool overCloseOnPress;
		bool allowDoubleClick;

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			var t = FindTab ((int)evnt.X, (int)evnt.Y);
			if (t != null) {
				if (evnt.IsContextMenuButton ()) {
					DockNotebook.ActiveNotebook = notebook;
					notebook.CurrentTab = t;
					notebook.DoPopupMenu (notebook, t.Index, evnt);
					return true;
				}
				// Don't select the tab if we are clicking the close button
				if (IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
					overCloseOnPress = true;
					return true;
				}
				overCloseOnPress = false;

				// Don't select the tab if we are clicking the pin button
				if (IsOverPinButton (t, (int)evnt.X, (int)evnt.Y)) {
					overPinOnPress = true;
					return true;
				}
				overPinOnPress = false;

				if (evnt.Type == EventType.TwoButtonPress) {
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

				DockNotebook.ActiveNotebook = notebook;
				buttonPressedOnTab = true;
				notebook.CurrentTab = t;
				return true;
			}
			buttonPressedOnTab = true;
			QueueDraw ();
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			buttonPressedOnTab = false;

			if (placeholderWindow != null) {
				placeholderWindow.PlaceWindow (notebook);
				return base.OnButtonReleaseEvent (evnt);
			}

			if (!draggingTab) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);
				if (t != null && overCloseOnPress && IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
					notebook.OnCloseTab (t);
					allowDoubleClick = false;
					return true;
				} else if (t != null && overPinOnPress && IsOverPinButton (t, (int)evnt.X, (int)evnt.Y)) {
					t.IsPinned = !t.IsPinned;
					notebook.OnPinTab (t);
					allowDoubleClick = false;
					return true;
				}
			}
			overCloseOnPress = false;
			overPinOnPress = false;
			allowDoubleClick = true;
			if (dragX != 0)
				this.Animate ("EndDrag",
					f => dragXProgress = f,
					1.0d,
					0.0d,
					easing: Easing.CubicOut,
					finished: (f, a) => draggingTab = false);
			QueueDraw ();
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override void OnUnrealized ()
		{
			// Cancel drag operations and animations
			buttonPressedOnTab = false;
			overCloseOnPress = false;
			overPinOnPress = false;
			allowDoubleClick = true;
			draggingTab = false;
			dragX = 0;
			this.AbortAnimation ("EndDrag");
			base.OnUnrealized ();
		}

		DockNotebookTab FindTab (int x, int y)
		{
			var current = notebook.CurrentTab as DockNotebookTab;
			if (current != null) {
				var allocWithLean = current.Allocation;
				allocWithLean.X -= LeanWidth / 2;
				allocWithLean.Width += LeanWidth;
				if (allocWithLean.Contains (x, y))
					return current;
			}

			for (int n = 0; n < notebook.Tabs.Count; n++) {
				var tab = (DockNotebookTab)notebook.Tabs [n];
				if (tab.Allocation.Contains (x, y))
					return tab;
			}
			return null;
		}

		static bool IsOverCloseButton (DockNotebookTab tab, int x, int y)
		{
			return tab != null && tab.CloseButtonActiveArea.Contains (x, y);
		}

		static bool IsOverPinButton (DockNotebookTab tab, int x, int y)
		{
			return tab != null && tab.PinButtonActiveArea.Contains (x, y);
		}

		public void Update ()
		{
			if (!tracker.Hovered) {
				UpdateTabWidth (tabEndX - tabStartX);
			} else if (closingTabs.ContainsKey (notebook.Tabs.Count)) {
				UpdateTabWidth (closingTabs [notebook.Tabs.Count].Allocation.Right - tabStartX, true);
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
			if (!IsRealized)
				TabWidth = TargetWidth;
		}

		static int Clamp (int val, int min, int max)
		{
			return Math.Max (min, Math.Min (max, val));
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
						callback: f => renderOffset = (int)f);
					animationTarget = targetOffset;
				}
			}

			return tabStartX - renderOffset;
		}

		Action<Context> DrawClosingTab (int index, Gdk.Rectangle region, out int width)
		{
			width = 0;
			if (closingTabs.ContainsKey (index)) {
				DockNotebookTab closingTab = closingTabs [index];
				width = (int)(closingTab.WidthModifier * TabWidth);
				int tmp = width;
				return c => DrawTab (c, closingTab, Allocation, new Gdk.Rectangle (region.X, region.Y, tmp, region.Height), false, false, false, CreateTabLayout (closingTab));
			}
			return c => {
			};
		}

		void Draw (Context ctx)
		{
			int tabArea = tabEndX - tabStartX;
			int x = GetRenderOffset ();
			const int y = 0;
			int n = 0;
			Action<Context> drawActive = c => {
			};
			var drawCommands = new List<Action<Context>> ();
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
				width = (int)(width * tab.WidthModifier);

				if (active) {
					int tmp = x;
					drawActive = c => DrawTab (c, tab, Allocation, new Gdk.Rectangle (tmp, y, width, Allocation.Height), true, true, draggingTab, CreateTabLayout (tab, true));
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				} else {
					int tmp = x;
					bool highlighted = tab == highlightedTab;

					if (tab.SaveStrength > 0.0f) {
						tmp = (int)(tab.SavedAllocation.X + (tmp - tab.SavedAllocation.X) * (1.0f - tab.SaveStrength));
					}

					drawCommands.Add (c => DrawTab (c, tab, Allocation, new Gdk.Rectangle (tmp, y, width, Allocation.Height), highlighted, false, false, CreateTabLayout (tab)));
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				}

				x += width;
			}

			var allocation = Allocation;
			int tabWidth;
			drawCommands.Add (DrawClosingTab (n, new Gdk.Rectangle (x, y, 0, allocation.Height), out tabWidth));
			drawCommands.Reverse ();

			ctx.DrawImage (this, tabbarBackImage.WithSize (allocation.Width, allocation.Height), 0, 0);

			// Draw breadcrumb bar header
//			if (notebook.Tabs.Count > 0) {
//				ctx.Rectangle (0, allocation.Height - BottomBarPadding, allocation.Width, BottomBarPadding);
//				ctx.SetSourceColor (Styles.BreadcrumbBackgroundColor);
//				ctx.Fill ();
//			}

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
			using (var context = CairoHelper.Create (evnt.Window)) {
				Draw (context);
			}
			return base.OnExposeEvent (evnt);
		}

		void DrawTab (Context ctx, DockNotebookTab tab, Gdk.Rectangle allocation, Gdk.Rectangle tabBounds, bool highlight, bool active, bool dragging, Pango.Layout la)
		{
			// This logic is stupid to have here, should be in the caller!
			if (dragging) {
				tabBounds.X = (int)(tabBounds.X + (dragX - tabBounds.X) * dragXProgress);
				tabBounds.X = Clamp (tabBounds.X, tabStartX, tabEndX - tabBounds.Width);
			}
			double rightPadding = (active ? TabActivePadding.Right : TabPadding.Right) - (LeanWidth / 2);
			rightPadding = (rightPadding * Math.Min (1.0, Math.Max (0.5, (tabBounds.Width - 30) / 70.0)));
			double leftPadding = (active ? TabActivePadding.Left : TabPadding.Left) - (LeanWidth / 2);
			leftPadding = (leftPadding * Math.Min (1.0, Math.Max (0.5, (tabBounds.Width - 30) / 70.0)));
			double bottomPadding = active ? TabActivePadding.Bottom : TabPadding.Bottom;

			DrawTabBackground (this, ctx, allocation, tabBounds.Width, tabBounds.X, active, tab.IsPinned);

			ctx.LineWidth = 1;
			ctx.NewPath ();

			// Render Close Button (do this first so we can tell how much text to render)

			var closeButtonAlloation = new Cairo.Rectangle (tabBounds.Right - rightPadding - (tabCloseImage.Width / 2) - CloseButtonMarginRight,
			                                 tabBounds.Height - bottomPadding - tabCloseImage.Height - CloseButtonMarginBottom,
			                                 tabCloseImage.Width, tabCloseImage.Height);
			
			var spinButtonAllocation = new Cairo.Rectangle (closeButtonAlloation.X - rightPadding - (tabPinnedImage.Width / 2) - PinButtonMarginRight,
											 closeButtonAlloation.Y,
											 tabPinnedImage.Width, tabPinnedImage.Height);

			tab.CloseButtonActiveArea = closeButtonAlloation.Inflate (2, 2);
			tab.PinButtonActiveArea = spinButtonAllocation.Inflate (2, 2);

			bool closeButtonHovered = tracker.Hovered && tab.CloseButtonActiveArea.Contains (tracker.MousePosition);
			bool pinButtonHovered = tracker.Hovered && tab.PinButtonActiveArea.Contains (tracker.MousePosition);
			bool tabHovered = tracker.Hovered && tab.Allocation.Contains (tracker.MousePosition);
			bool drawCloseButton = active || tabHovered;
			bool drawPinButton = tabHovered;

			if (!closeButtonHovered && tab.DirtyStrength > 0.5) {
				ctx.DrawImage (this, tabDirtyImage, closeButtonAlloation.X, closeButtonAlloation.Y);
				drawCloseButton = false;
			}

			if (drawCloseButton)
				ctx.DrawImage (this, tabCloseImage.WithAlpha ((closeButtonHovered ? 1.0 : 0.5) * tab.Opacity), closeButtonAlloation.X, closeButtonAlloation.Y);
			
			if (drawPinButton)
				ctx.DrawImage (this, (tab.IsPinned ? tabPinnedImage : tabUnPinnedImage).WithAlpha ((pinButtonHovered ? 1.0 : 0.5) * tab.Opacity), spinButtonAllocation.X, spinButtonAllocation.Y);

			// Render Text
			double tw = tabBounds.Width - (leftPadding + rightPadding);
			if (drawCloseButton || tab.DirtyStrength > 0.5)
				tw -= closeButtonAlloation.Width / 2;

			if (drawPinButton || tab.DirtyStrength > 0.5)
				tw -= spinButtonAllocation.Width / 2;

			double tx = tabBounds.X + leftPadding;
			var baseline = la.GetLine (0).Layout.GetPixelBaseline ();
			double ty = tabBounds.Height - bottomPadding - baseline;

			ctx.MoveTo (tx, ty);
			if (!MonoDevelop.Core.Platform.IsMac && !MonoDevelop.Core.Platform.IsWindows) {
				// This is a work around for a linux specific problem.
				// A bug in the proprietary ATI driver caused TAB text not to draw.
				// If that bug get's fixed remove this HACK asap.
				la.Ellipsize = Pango.EllipsizeMode.End;
				la.Width = (int)(tw * Pango.Scale.PangoScale);
				ctx.SetSourceColor ((tab.Notify ? Styles.TabBarNotifyTextColor : (active ? Styles.TabBarActiveTextColor : Styles.TabBarInactiveTextColor)).ToCairoColor ());
				Pango.CairoHelper.ShowLayout (ctx, la.GetLine (0).Layout);
			} else {
				// ellipses are for space wasting ..., we cant afford that
				using (var lg = new LinearGradient (tx + tw - 10, 0, tx + tw, 0)) {
					var color = (tab.Notify ? Styles.TabBarNotifyTextColor : (active ? Styles.TabBarActiveTextColor : Styles.TabBarInactiveTextColor)).ToCairoColor ();
					color = color.MultiplyAlpha (tab.Opacity);
					lg.AddColorStop (0, color);
					color.A = 0;
					lg.AddColorStop (1, color);
					ctx.SetSource (lg);
					Pango.CairoHelper.ShowLayout (ctx, la.GetLine (0).Layout);
				}
			}
            la.Dispose ();
		}

		static void DrawTabBackground (Widget widget, Context ctx, Gdk.Rectangle allocation, int contentWidth, int px, bool active = true, bool isPinned = false)
		{
			int lean = Math.Min (LeanWidth, contentWidth / 2);
			int halfLean = lean / 2;

			double x = px + TabSpacing - halfLean;
			double y = 0;
			double height = allocation.Height;
			double width = contentWidth - (TabSpacing * 2) + lean;

			var image = active ? 
				isPinned ? tabPinnedActiveBackImage : tabActiveBackImage : 
				isPinned ? tabPinnedBackImage : tabBackImage;
			image = image.WithSize (width, height);

			ctx.DrawImage (widget, image, x, y);
		}

		Pango.Layout CreateSizedLayout (bool active)
		{
			var la = new Pango.Layout (PangoContext);
			la.FontDescription = Ide.Fonts.FontService.SansFont.Copy ();
			if (!Core.Platform.IsWindows)
				la.FontDescription.Weight = Pango.Weight.Bold;
			la.FontDescription.AbsoluteSize = Pango.Units.FromPixels (VerticalTextSize);

			return la;
		}

		Pango.Layout CreateTabLayout (DockNotebookTab tab, bool active = false)
		{
			Pango.Layout la = CreateSizedLayout (active);
			if (!string.IsNullOrEmpty (tab.Markup))
				la.SetMarkup (tab.Markup);
			else if (!string.IsNullOrEmpty (tab.Text))
				la.SetText (tab.Text);
			return la;
		}
	}
}
