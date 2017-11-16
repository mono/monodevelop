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
using MonoDevelop.Components.AtkCocoaHelper;
using Xwt.Motion;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Runtime.InteropServices;

namespace MonoDevelop.Components.DockNotebook
{
	class TabStrip: EventBox, IAnimatable
	{
		static Xwt.Drawing.Image tabbarPrevImage = Xwt.Drawing.Image.FromResource ("tabbar-prev-12.png");
		static Xwt.Drawing.Image tabbarNextImage = Xwt.Drawing.Image.FromResource ("tabbar-next-12.png");
		static Xwt.Drawing.Image tabbarBackImage = Xwt.Drawing.Image.FromResource ("tabbar-back.9.png");
		readonly static TabStripRenderer renderer = new TabStripRenderer ();

		HBox innerBox;

		readonly DragDockNotebookTabManager dragManager;
		readonly DockNotebook notebook;
		DockNotebookTab highlightedTab;
		bool overCloseButton;
		bool buttonPressedOnTab;
		int tabStartX, tabEndX;
		bool isActiveNotebook;

		MouseTracker tracker;

		int renderOffset;
		int targetOffset;
		int animationTarget;

		Dictionary<int, DockNotebookTab> closingTabs;

		public Button PreviousButton;
		public Button NextButton;
		public MenuButton DropDownButton;

		static readonly int TotalHeight = 32;
		internal static readonly Xwt.WidgetSpacing TabPadding;
		internal static readonly Xwt.WidgetSpacing TabActivePadding;
		static readonly int LeftBarPadding = 44;
		static readonly int RightBarPadding = 22;
		internal const int TabSpacing = 0;
		internal const int LeanWidth = 12;

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
			get { return NextButton.Visible; }
			set {
				if (value == NavigationButtonsVisible)
					return;

				NextButton.Visible = value;
				PreviousButton.Visible = value;

				OnSizeAllocated (Allocation);
			}
		}

		static TabStrip ()
		{
			Xwt.Drawing.NinePatchImage tabBackImage9;
			if (DockNotebookTab.tabBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)DockNotebookTab.tabBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabBackImage9 = DockNotebookTab.tabBackImage as Xwt.Drawing.NinePatchImage;
			TabPadding = tabBackImage9.Padding;


			Xwt.Drawing.NinePatchImage tabActiveBackImage9;
			if (DockNotebookTab.tabActiveBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)DockNotebookTab.tabActiveBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabActiveBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabActiveBackImage9 = DockNotebookTab.tabActiveBackImage as Xwt.Drawing.NinePatchImage;
			TabActivePadding = tabActiveBackImage9.Padding;
		}

		public TabStrip (DockNotebook notebook)
		{
			if (notebook == null)
				throw new ArgumentNullException ("notebook");

			dragManager = new DragDockNotebookTabManager ();

			Accessible.SetRole (AtkCocoa.Roles.AXTabGroup);

			// Handle focus for the tabs.
			CanFocus = true;

			TabWidth = 125;
			TargetWidth = 125;
			tracker = new MouseTracker (this);
			GtkWorkarounds.FixContainerLeak (this);

			innerBox = new HBox (false, 0);
			innerBox.Accessible.SetShouldIgnore (true);
			Add (innerBox);

			this.notebook = notebook;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			Events |= EventMask.PointerMotionMask | EventMask.LeaveNotifyMask | EventMask.ButtonPressMask;

			var arr = new Xwt.ImageView (tabbarPrevImage);
			arr.HeightRequest = arr.WidthRequest = 10;

			var alignment = new Alignment (0.5f, 0.5f, 0.0f, 0.0f);
			alignment.Add (arr.ToGtkWidget ());
			PreviousButton = new Button (alignment);
			PreviousButton.TooltipText = Core.GettextCatalog.GetString ("Switch to previous document");
			PreviousButton.Relief = ReliefStyle.None;
			PreviousButton.CanDefault = false;
			PreviousButton.CanFocus = true;
			PreviousButton.Accessible.Name = "DockNotebook.Tabstrip.PreviousButton";
			PreviousButton.Accessible.SetTitle (Core.GettextCatalog.GetString ("Previous document"));
			PreviousButton.Accessible.Description = Core.GettextCatalog.GetString ("Switch to previous document");

			arr = new Xwt.ImageView (tabbarNextImage);
			arr.HeightRequest = arr.WidthRequest = 10;

			alignment = new Alignment (0.5f, 0.5f, 0.0f, 0.0f);
			alignment.Add (arr.ToGtkWidget ());
			NextButton = new Button (alignment);
			NextButton.TooltipText = Core.GettextCatalog.GetString ("Switch to next document");
			NextButton.Relief = ReliefStyle.None;
			NextButton.CanDefault = false;
			NextButton.CanFocus = true;
			NextButton.Accessible.Name = "DockNotebook.Tabstrip.NextButton";
			NextButton.Accessible.SetTitle (Core.GettextCatalog.GetString ("Next document"));
			NextButton.Accessible.Description = Core.GettextCatalog.GetString ("Switch to next document");

			DropDownButton = new MenuButton ();
			DropDownButton.TooltipText = Core.GettextCatalog.GetString ("Document List");
			DropDownButton.Relief = ReliefStyle.None;
			DropDownButton.CanDefault = false;
			DropDownButton.CanFocus = true;
			DropDownButton.Accessible.Name = "DockNotebook.Tabstrip.DocumentListButton";
			DropDownButton.Accessible.SetTitle (Core.GettextCatalog.GetString ("Document list"));
			DropDownButton.Accessible.Description = Core.GettextCatalog.GetString ("Display the document list menu");

			PreviousButton.ShowAll ();
			PreviousButton.NoShowAll = true;
			NextButton.ShowAll ();
			NextButton.NoShowAll = true;
			DropDownButton.ShowAll ();

			PreviousButton.Name = "MonoDevelop.DockNotebook.BarButton";
			NextButton.Name = "MonoDevelop.DockNotebook.BarButton";
			DropDownButton.Name = "MonoDevelop.DockNotebook.BarButton";

			innerBox.PackStart (PreviousButton, false, false, 0);
			innerBox.PackStart (NextButton, false, false, 0);
			innerBox.PackEnd (DropDownButton, false, false, 0);

			tracker.HoveredChanged += (sender, e) => {
				if (!tracker.Hovered) {
					SetHighlightedTab (null);
					UpdateTabWidth (tabEndX - tabStartX);
					QueueDraw ();
				}
			};
			
			foreach (var tab in notebook.Tabs) {
				Accessible.AddAccessibleElement (tab.Accessible);
			}
			UpdateAccessibilityTabs ();
			notebook.PageAdded += PageAddedHandler;
			notebook.PageRemoved += PageRemovedHandler;
			notebook.TabsReordered += PageReorderedHandler;

			closingTabs = new Dictionary<int, DockNotebookTab> ();
		}

		protected override void OnDestroyed ()
		{
			this.AbortAnimation ("TabWidth");
			this.AbortAnimation ("EndDrag");
			this.AbortAnimation ("ScrollTabs");
			base.OnDestroyed ();
		}

		void PageAddedHandler (object sender, TabEventArgs args)
		{
			var tab = args.Tab;

			Accessible.AddAccessibleElement (tab.Accessible);

			tab.AccessibilityPressTab += OnAccessibilityPressTab;
			tab.AccessibilityPressCloseButton += OnAccessibilityPressCloseButton;
			tab.AccessibilityShowMenu += OnAccessibilityShowMenu;

			QueueResize ();

			UpdateAccessibilityTabs ();
		}

		void PageRemovedHandler (object sender, TabEventArgs args)
		{
			var tab = args.Tab;

			tab.AccessibilityPressTab -= OnAccessibilityPressTab;
			tab.AccessibilityPressCloseButton -= OnAccessibilityPressCloseButton;
			tab.AccessibilityShowMenu -= OnAccessibilityShowMenu;

			Accessible.RemoveAccessibleElement (tab.Accessible);

			tab.Dispose ();

			QueueResize ();

			UpdateAccessibilityTabs ();
		}

		void PageReorderedHandler (DockNotebookTab tab, int oldPlacement, int newPlacement)
		{
			QueueResize ();

			UpdateAccessibilityTabs ();
		}

		void UpdateAccessibilityTabs ()
		{
			var tabs = notebook.Tabs.OrderBy (x => x.Index).Select (x => x.Accessible).ToArray ();
			Accessible.SetTabs (tabs);
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

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (NavigationButtonsVisible) {
				tabStartX = /*allocation.X +*/ LeftBarPadding + LeanWidth / 2;
			} else {
				tabStartX = LeanWidth / 2;
			}
			tabEndX = allocation.Width - RightBarPadding;

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
			if (dragManager.IsDragging && placeholderWindow == null && !mouseHasLeft)
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
			if (dragManager.IsDragging && mouseHasLeft) {
				var sr = GetScreenRect ();
				sr.Height = BarHeight;
				sr.Inflate (30, 30);

				int x, y;
				Gdk.Display.Default.GetPointer (out x, out y);

				if (x < sr.Left || x > sr.Right || y < sr.Top || y > sr.Bottom) {
					dragManager.Cancel ();
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

			if (!dragManager.IsDragging) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);

				// If the user clicks and drags on the 'x' which closes the current
				// tab we can end up with a null tab here
				if (t == null) {
					TooltipText = null;
					return base.OnMotionNotifyEvent (evnt);
				}
				SetHighlightedTab (t);

				var newOver = t.IsOverCloseButton ((int)evnt.X, (int)evnt.Y);
				if (newOver != overCloseButton) {
					overCloseButton = newOver;
					QueueDraw ();
				}
				if (!overCloseButton && !dragManager.IsDragging && buttonPressedOnTab) {
					dragManager.Start (t);
					mouseHasLeft = false;
					dragManager.Progress = 1.0f;
					int x = (int)evnt.X;
					dragManager.Offset = x - t.Allocation.X;
					dragManager.X = x - dragManager.Offset;
					dragManager.LastX = (int)evnt.X;
				} else if (t != null)
					newTooltip = t.Tooltip;
			} else if (evnt.State.HasFlag (ModifierType.Button1Mask)) {
				dragManager.X = (int)evnt.X - dragManager.Offset;
				QueueDraw ();

				var t = FindTab ((int)evnt.X, (int)TabPadding.Top + 3);
				if (t == null) {
					var last = (DockNotebookTab)notebook.Tabs.Last ();
					if (dragManager.X > last.Allocation.Right)
						t = last;
					if (dragManager.X < 0)
						t = (DockNotebookTab)notebook.Tabs.First ();
				}
				if (t != null && t != notebook.CurrentTab && (
					((int)evnt.X > dragManager.LastX && t.Index > notebook.CurrentTab.Index) ||
					((int)evnt.X < dragManager.LastX && t.Index < notebook.CurrentTab.Index))) {
					t.SaveAllocation ();
					t.SaveStrength = 1;
					notebook.ReorderTab ((DockNotebookTab)notebook.CurrentTab, t);

					t.Animate ("TabMotion",
						f => t.SaveStrength = f,
						1.0f,
						0.0f,
						easing: Easing.CubicInOut);
				}
				dragManager.LastX = (int)evnt.X;
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
					DockNotebook.ActiveNotebook = notebook;
					notebook.CurrentTab = t;
					notebook.DoPopupMenu (notebook, t.Index, evnt);
					return true;
				}
				// Don't select the tab if we are clicking the close button
				if (t.IsOverCloseButton ((int)evnt.X, (int)evnt.Y)) {
					overCloseOnPress = true;
					return true;
				}
				overCloseOnPress = false;

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

			if (!dragManager.IsDragging && overCloseOnPress) {
				var t = FindTab ((int)evnt.X, (int)evnt.Y);
				if (t != null && t.IsOverCloseButton ((int)evnt.X, (int)evnt.Y)) {
					notebook.OnCloseTab (t);
					allowDoubleClick = false;
					return true;
				}
			}
			overCloseOnPress = false;
			allowDoubleClick = true;
			if (dragManager.X != 0)
				this.Animate ("EndDrag",
					f => dragManager.Progress = f,
					1.0d,
					0.0d,
					easing: Easing.CubicOut,
				              finished: (f, a) => dragManager.Cancel ());
			QueueDraw ();
			return base.OnButtonReleaseEvent (evnt);
		}

		void OnAccessibilityPressTab (object sender, EventArgs args)
		{
			DockNotebook.ActiveNotebook = notebook;
			notebook.CurrentTab = sender as DockNotebookTab;

			QueueDraw ();
		}

		void OnAccessibilityPressCloseButton (object sender, EventArgs args)
		{
			notebook.OnCloseTab (sender as DockNotebookTab);

			QueueDraw ();
		}

		void OnAccessibilityShowMenu (object sender, EventArgs args)
		{
			var tab = sender as DockNotebookTab;
			DockNotebook.ActiveNotebook = notebook;
			notebook.CurrentTab = tab;

			int x = tab.Allocation.X + (tab.Allocation.Width / 2);
			int y = tab.Allocation.Y + (tab.Allocation.Height / 2);

			// Fake an event, but all we need is the x and y.
			// Ugly but the only way without messing up the public API.
			var nativeEvent = new NativeEventButtonStruct {
				x = x,
				y = y,
			};

			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent);
			try {
				Gdk.EventButton evnt = new Gdk.EventButton (ptr);
				notebook.DoPopupMenu (notebook, tab.Index, evnt);
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}

		Widget currentFocus = null;
		int currentFocusTab = -1;
		bool currentFocusCloseButton;

		protected override void OnActivate ()
		{
			if (currentFocusTab == -1) {
				return;
			}
			var tab = notebook.Tabs [currentFocusTab];
			if (currentFocusCloseButton) {
				notebook.OnCloseTab (tab);

				currentFocusTab--;
				currentFocusCloseButton = true;

				// Focus the next tab
				OnFocused (DirectionType.TabForward);
			} else {
				notebook.CurrentTab = tab;
			}
		}

		enum FocusWidget
		{
			None,
			BackButton,
			NextButton,
			Tabs,
			TabCloseButton,
			MenuButton
		};

		bool FocusCurrentWidget (DirectionType direction)
		{
			if (currentFocus == null) {
				return false;
			}

			return currentFocus.ChildFocus (direction);
		}

		bool MoveFocusToWidget (FocusWidget widget, DirectionType direction)
		{
			switch (widget) {
			case FocusWidget.BackButton:
				currentFocus = PreviousButton;
				return PreviousButton.ChildFocus (direction);

			case FocusWidget.NextButton:
				currentFocus = NextButton;
				return NextButton.ChildFocus (direction);

			case FocusWidget.MenuButton:
				currentFocus = DropDownButton;
				return DropDownButton.ChildFocus (direction);

			case FocusWidget.Tabs:
			case FocusWidget.TabCloseButton:
				GrabFocus ();
				currentFocus = null;
				QueueDraw ();
				return true;

			case FocusWidget.None:
				break;
			}
			return false;
		}

		FocusWidget GetNextWidgetToFocus (FocusWidget widget, DirectionType direction)
		{
			switch (widget) {
			case FocusWidget.BackButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (notebook.Tabs.Count > 0) {
						currentFocusTab = 0;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}

				case DirectionType.TabBackward:
				case DirectionType.Left:
					return FocusWidget.None;
				}
				break;

			case FocusWidget.NextButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (notebook.Tabs.Count > 0) {
						currentFocusTab = 0;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}

				case DirectionType.TabBackward:
				case DirectionType.Left:
					if (PreviousButton.Sensitive && PreviousButton.Visible) {
						return FocusWidget.BackButton;
					} else {
						return FocusWidget.None;
					}
				}
				break;

			case FocusWidget.Tabs:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					currentFocusCloseButton = true;
					return FocusWidget.TabCloseButton;
					/*
					if (currentFocusTab < notebook.Tabs.Count - 1) {
						currentFocusTab++;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						currentFocusTab = -1;
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}
					*/

				case DirectionType.TabBackward:
				case DirectionType.Left:
					if (currentFocusTab > 0) {
						currentFocusTab--;
						currentFocusCloseButton = true;
						return FocusWidget.TabCloseButton;
					} else if (NextButton.Sensitive && NextButton.Visible) {
						currentFocusTab = -1;
						return FocusWidget.NextButton;
					} else if (PreviousButton.Sensitive && PreviousButton.Visible) {
						currentFocusTab = -1;
						return FocusWidget.BackButton;
					} else {
						currentFocusTab = -1;
						return FocusWidget.None;
					}
				}
				break;

			case FocusWidget.TabCloseButton:
				currentFocusCloseButton = false;
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (currentFocusTab < notebook.Tabs.Count - 1) {
						currentFocusTab++;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						currentFocusTab = -1;
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}

				case DirectionType.TabBackward:
				case DirectionType.Left:
					return FocusWidget.Tabs;
				}
				break;

			case FocusWidget.MenuButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					return FocusWidget.None;

				case DirectionType.TabBackward:
				case DirectionType.Left:
					if (notebook.Tabs.Count > 0) {
						currentFocusTab = notebook.Tabs.Count - 1;
						currentFocusCloseButton = true;
						return FocusWidget.TabCloseButton;
					} else if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (PreviousButton.Sensitive && PreviousButton.Visible) {
						return FocusWidget.BackButton;
					} else {
						return FocusWidget.None;
					}
				}
				break;

			case FocusWidget.None:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (PreviousButton.Sensitive && PreviousButton.Visible) {
						return FocusWidget.BackButton;
					}else if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (notebook.Tabs.Count > 0) {
						currentFocusTab = 0;
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						return FocusWidget.MenuButton;
					} else {
						return FocusWidget.None;
					}

				case DirectionType.TabBackward:
				case DirectionType.Left:
					if (DropDownButton.Sensitive && DropDownButton.Visible) {
						return FocusWidget.MenuButton;
					} else if (notebook.Tabs.Count > 0) {
						currentFocusTab = notebook.Tabs.Count - 1;
						currentFocusCloseButton = true;
						return FocusWidget.TabCloseButton;
					} else if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (PreviousButton.Sensitive && PreviousButton.Visible) {
						return FocusWidget.BackButton;
					} else {
						return FocusWidget.None;
					}
				}
				break;
			}

			return FocusWidget.None;
		}

		protected override bool OnFocused (DirectionType direction)
		{
			if (!FocusCurrentWidget (direction)) {
				FocusWidget focus = FocusWidget.None;

				if (currentFocus == PreviousButton) {
					focus = FocusWidget.BackButton;
				} else if (currentFocus == NextButton) {
					focus = FocusWidget.NextButton;
				} else if (currentFocus == DropDownButton) {
					focus = FocusWidget.MenuButton;
				} else if (IsFocus) {
					focus = currentFocusCloseButton ? FocusWidget.TabCloseButton : FocusWidget.Tabs;
				}

				while ((focus = GetNextWidgetToFocus (focus, direction)) != FocusWidget.None) {
					if (MoveFocusToWidget (focus, direction)) {
						return true;
					}
				}

				currentFocus = null;
				currentFocusTab = -1;
				return false;
			}

			return base.OnFocused (direction);
		}

		protected override void OnUnrealized ()
		{
			// Cancel drag operations and animations
			buttonPressedOnTab = false;
			overCloseOnPress = false;
			allowDoubleClick = true;
			dragManager.Reset ();
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

		internal bool IsElementHovered (DockNotebookTab element) 
		{
			return tracker.Hovered && element.Allocation.Contains (tracker.MousePosition);
		}

		internal bool IsElementHovered (DockNotebookTabButton element)
		{
			return tracker.Hovered && element.Allocation.Contains (tracker.MousePosition);
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
				TargetWidth = HelperMethods.Clamp (width / notebook.Tabs.Count, 50, 200);

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

		int GetRenderOffset ()
		{
			int tabArea = tabEndX - tabStartX;
			if (notebook.CurrentTabIndex >= 0) {
				int normalizedArea = (tabArea / TargetWidth) * TargetWidth;
				int maxOffset = Math.Max (0, (notebook.Tabs.Count * TargetWidth) - normalizedArea);

				int distanceToTabEdge = TargetWidth * notebook.CurrentTabIndex;
				int window = normalizedArea - TargetWidth;
				targetOffset = Math.Min (maxOffset, HelperMethods.Clamp (renderOffset, distanceToTabEdge - window, distanceToTabEdge));

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
				return c => closingTab.OnDraw (c, this, new Gdk.Rectangle (region.X, region.Y, tmp, region.Height), false, false);
			}
			return c => {
			};
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			renderer.Draw (evnt.Window, this);
			return base.OnExposeEvent (evnt);
		}

		class TabStripRenderer
		{
			public void Draw (Gdk.Window window, TabStrip tabStrip)
			{
				using (var context = CairoHelper.Create (window)) {
					OnDraw (context, tabStrip);
				}
			}

			Gdk.Rectangle CalculateBounds (TabStrip tabStrip, Gdk.Rectangle tabBounds, bool dragged)
			{
				if (dragged) {
					tabBounds.X = (int)(tabBounds.X + (tabStrip.dragManager.X - tabBounds.X) * tabStrip.dragManager.Progress);
					tabBounds.X = HelperMethods.Clamp (tabBounds.X, tabStrip.tabStartX, tabStrip.tabEndX - tabBounds.Width);
				}
				return tabBounds;	
			}

			void OnDraw (Context ctx, TabStrip tabStrip)
			{
				int tabArea = tabStrip.tabEndX - tabStrip.tabStartX;
				int x = tabStrip.GetRenderOffset ();
				const int y = 0;
				int n = 0;
				Action<Context> drawActive = null;
				var drawCommands = new List<Action<Context>> ();

				Gdk.Rectangle focusRect = new Gdk.Rectangle (0, 0, 0, 0);

				for (; n < tabStrip.notebook.Tabs.Count; n++) {
					if (x + tabStrip.TabWidth < tabStrip.tabStartX) {
						x += tabStrip.TabWidth;
						continue;
					}

					if (x > tabStrip.tabEndX)
						break;

					int closingWidth;
					var cmd = tabStrip.DrawClosingTab (n, new Gdk.Rectangle (x, y, 0, tabStrip.Allocation.Height), out closingWidth);
					drawCommands.Add (cmd);
					x += closingWidth;

					var tab = (DockNotebookTab)tabStrip.notebook.Tabs [n];
					bool active = tab == tabStrip.notebook.CurrentTab;
					bool focused = (n == tabStrip.currentFocusTab);

					int width = Math.Min (tabStrip.TabWidth, Math.Max (50, tabStrip.tabEndX - x - 1));
					if (tab == tabStrip.notebook.Tabs.Last ())
						width += tabStrip.LastTabWidthAdjustment;
					width = (int)(width * tab.WidthModifier);

					if (active) {
						int tmp = x;
						drawActive = c => {
							var calculatedBounds = CalculateBounds (tabStrip, new Gdk.Rectangle (tmp, y, width, tabStrip.Allocation.Height), tabStrip.dragManager.IsDragging);
							tab.OnDraw (c, tabStrip, calculatedBounds, true, focused);
						};
						tab.Allocation = new Gdk.Rectangle (tmp, tabStrip.Allocation.Y, width, tabStrip.Allocation.Height);
					} else {
						int tmp = x;
						bool highlighted = tab == tabStrip.highlightedTab;

						if (tab.SaveStrength > 0.0f) {
							tmp = (int)(tab.SavedAllocation.X + (tmp - tab.SavedAllocation.X) * (1.0f - tab.SaveStrength));
						}

						drawCommands.Add (c => tab.OnDraw (c, tabStrip, new Gdk.Rectangle (tmp, y, width, tabStrip.Allocation.Height), false, focused));
						tab.Allocation = new Gdk.Rectangle (tmp, tabStrip.Allocation.Y, width, tabStrip.Allocation.Height);
					}

					if (focused) {
						if (tabStrip.currentFocusCloseButton) {
							var closeButton = tab.GetCloseButton ();
							focusRect = closeButton.Allocation;
						} else {
							focusRect = new Gdk.Rectangle (tab.Allocation.X + 5, tab.Allocation.Y + 10, tab.Allocation.Width - 30, tab.Allocation.Height - 15);
						}
					}
					x += width;
				}

				var allocation = tabStrip.Allocation;
				int tabWidth;
				drawCommands.Add (tabStrip.DrawClosingTab (n, new Gdk.Rectangle (x, y, 0, allocation.Height), out tabWidth));
				drawCommands.Reverse ();

				ctx.DrawImage (tabStrip, tabbarBackImage.WithSize (allocation.Width, allocation.Height), 0, 0);

				// Draw breadcrumb bar header
				//			if (notebook.Tabs.Count > 0) {
				//				ctx.Rectangle (0, allocation.Height - BottomBarPadding, allocation.Width, BottomBarPadding);
				//				ctx.SetSourceColor (Styles.BreadcrumbBackgroundColor);
				//				ctx.Fill ();
				//			}

				ctx.Rectangle (tabStrip.tabStartX - LeanWidth / 2, allocation.Y, tabArea + LeanWidth, allocation.Height);
				ctx.Clip ();

				foreach (var cmd in drawCommands)
					cmd (ctx);

				ctx.ResetClip ();

				// Redraw the dragging tab here to be sure its on top. We drew it before to get the sizing correct, this should be fixed.
				drawActive?.Invoke (ctx);

				if (tabStrip.HasFocus) {
					Gtk.Style.PaintFocus (tabStrip.Style, tabStrip.GdkWindow, tabStrip.State, focusRect, tabStrip, "tab", focusRect.X, focusRect.Y, focusRect.Width, focusRect.Height);
				}
			}
		}
	}
}
