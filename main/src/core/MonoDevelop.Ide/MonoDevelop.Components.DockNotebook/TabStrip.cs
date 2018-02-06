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

		static Xwt.Drawing.Image tabActiveBackImage = Xwt.Drawing.Image.FromResource ("tabbar-active.9.png");
		static Xwt.Drawing.Image tabBackImage = Xwt.Drawing.Image.FromResource ("tabbar-inactive.9.png");

		static Xwt.Drawing.Image tabPreviewActiveBackImage = Xwt.Drawing.Image.FromResource ("tabbar-preview-active.9.png");
		static Xwt.Drawing.Image tabPreviewBackImage = Xwt.Drawing.Image.FromResource ("tabbar-preview-inactive.9.png");

		static Xwt.Drawing.Image tabbarBackImage = Xwt.Drawing.Image.FromResource ("tabbar-back.9.png");
		static Xwt.Drawing.Image tabbarPreviewBackImage = Xwt.Drawing.Image.FromResource ("tabbar-preview-back.9.png");

		static Xwt.Drawing.Image tabCloseImage = Xwt.Drawing.Image.FromResource ("tab-close-9.png");
		static Xwt.Drawing.Image tabDirtyImage = Xwt.Drawing.Image.FromResource ("tab-dirty-9.png");

		HBox innerBox;

		readonly DockNotebook notebook;
		DockNotebookTab highlightedTab;
		bool overCloseButton;
		bool buttonPressedOnTab;
		bool isActiveNotebook;

		MouseTracker tracker;

		bool draggingTab;
		int dragX;
		int dragOffset;
		double dragXProgress;

		int renderOffset, prevRenderOffset;
		int targetOffset;
		int animationTarget;

		List<DockNotebookTab> closingTabs;

		public Button PreviousButton;
		public Button NextButton;
		public MenuButton DropDownButton;

		static readonly int TotalHeight = 32;
		static readonly Xwt.WidgetSpacing TabPadding;
		static readonly Xwt.WidgetSpacing TabActivePadding;
		static readonly int LeftBarPadding = 44;
		static readonly int RightBarPadding = 22;

		readonly TabContainer tabContainer;
		readonly TabContainer previewTabContainer;

		static readonly int VerticalTextSize = 11;
		const int TabSeparationBorder = 20;
		const int TabSpacing = 0;
		const int LeanWidth = 12;
		const int HalfLeanWidth = LeanWidth / 2;
		const double CloseButtonMarginRight = 0;
		const double CloseButtonMarginBottom = -1.0;

		const int TextOffset = 1;

		internal int LastTabWidthAdjustment { get; set; }
		internal int TabWidth { get; set; }

		int targetWidth;

		internal int TargetWidth {
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
				tabActiveBackImage9 = tabActiveBackImage as Xwt.Drawing.NinePatchImage;
			TabActivePadding = tabActiveBackImage9.Padding;
		}

		public TabStrip (DockNotebook notebook)
		{
			TabWidth = 125;
			TargetWidth = 125;

			if (notebook == null)
				throw new ArgumentNullException ("notebook");

			Accessible.SetRole (AtkCocoa.Roles.AXTabGroup);

			// Handle focus for the tabs.
			CanFocus = true;

			tracker = new MouseTracker (this);
			GtkWorkarounds.FixContainerLeak (this);

			innerBox = new HBox (false, 0);
			innerBox.Accessible.SetShouldIgnore (true);
			Add (innerBox);

			this.notebook = notebook;

			previewTabContainer = new TabContainer (this, true);
			tabContainer = new TabContainer (this, false);

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
					UpdateTabWidth (tabContainer.ContentEndX - tabContainer.ContentStartX);
					QueueDraw ();
				}
			};
			
			foreach (var tab in notebook.AllTabs) {
				if (tab.Accessible != null) {
					Accessible.AddAccessibleElement (tab.Accessible);

					tab.AccessibilityPressTab += OnAccessibilityPressTab;
					tab.AccessibilityPressCloseButton += OnAccessibilityPressCloseButton;
					tab.AccessibilityShowMenu += OnAccessibilityShowMenu;
				}
			}
			UpdateAccessibilityTabs ();
			notebook.PageAdded += PageAddedHandler;
			notebook.PageRemoved += PageRemovedHandler;
			notebook.TabsReordered += PageReorderedHandler;

			closingTabs = new List<DockNotebookTab> ();
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

			if (tab.Accessible != null) {
				Accessible.AddAccessibleElement (tab.Accessible);
		
				tab.AccessibilityPressTab += OnAccessibilityPressTab;
				tab.AccessibilityPressCloseButton += OnAccessibilityPressCloseButton;
				tab.AccessibilityShowMenu += OnAccessibilityShowMenu;
			}

			tab.ContentChanged += OnTabContentChanged;

			QueueResize ();

			UpdateAccessibilityTabs ();
		}

		void PageRemovedHandler (object sender, TabEventArgs args)
		{
			var tab = args.Tab;

			if (tab.Accessible != null) {
				tab.AccessibilityPressTab -= OnAccessibilityPressTab;
				tab.AccessibilityPressCloseButton -= OnAccessibilityPressCloseButton;
				tab.AccessibilityShowMenu -= OnAccessibilityShowMenu;
	
				Accessible.RemoveAccessibleElement (tab.Accessible);
			}

			tab.ContentChanged -= OnTabContentChanged;

			tab.Dispose ();

			QueueResize ();

			UpdateAccessibilityTabs ();
		}

		void OnTabContentChanged (object sender, EventArgs args)
		{
			var tab = (DockNotebookTab)sender;
			if (tab.IsPreview) {
				notebook.ChangeTabToPreview (tab); 
			} 
		}

		void PageReorderedHandler (DockNotebookTab tab, int oldPlacement, int newPlacement)
		{
			QueueResize ();

			UpdateAccessibilityTabs ();
		}

		void UpdateAccessibilityTabs ()
		{
			var tabs = notebook.AllTabs
			                   .Where (x => x.Accessible != null)
			                   .OrderBy (x => x.Index)
			                   .Select (x => x.Accessible)
			                   .ToArray ();
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
			closingTabs.Add (tab);
			new Animation (f => tab.WidthModifier = f, tab.WidthModifier, 0)
				.AddConcurrent (new Animation (f => tab.Opacity = f, tab.Opacity, 0), 0.8d)
				.Commit (tab, "Closing",
				easing: Easing.CubicOut,
				finished: (f, a) => {
					if (!a)
						closingTabs.Remove (tab);
				});
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			previewTabContainer.ContentStartX = allocation.Width - RightBarPadding - previewTabContainer.Width;
			previewTabContainer.ContentEndX = previewTabContainer.ContentStartX + previewTabContainer.Width;

			if (NavigationButtonsVisible) {
				tabContainer.ContentStartX = /*allocation.X +*/ LeftBarPadding + HalfLeanWidth;
			} else {
				tabContainer.ContentStartX = HalfLeanWidth;
			}
			tabContainer.ContentEndX = previewTabContainer.ContentStartX - 12;

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
				var t = FindInAllTabs ((int)evnt.X, (int)evnt.Y);

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

				var t = FindInAllTabs ((int)evnt.X, (int)TabPadding.Top + 3);
				if (t == null) {
					if (tabContainer.Tabs.Count > 0 && tabContainer.IsOverArea ((int)evnt.X)) {
						//We need detect if we are in a normal area or preview area
						var last = (DockNotebookTab)tabContainer.Tabs.Last ();
						if (dragX > last.Allocation.Right)
							t = last;
						if (dragX < 0)
							t = (DockNotebookTab) tabContainer.Tabs.First ();
					} else if (previewTabContainer.Tabs.Count > 0 && previewTabContainer.IsOverArea ((int)evnt.X)) {
						//We need detect if we are in a normal area or preview area
						var last = (DockNotebookTab) previewTabContainer.Tabs.Last ();
						if (dragX > last.Allocation.Right)
							t = last;
						if (dragX < 0)
							t = (DockNotebookTab)previewTabContainer.Tabs.First ();
					}
				}
				if (t != null && t != notebook.CurrentTab && notebook.CurrentTab.IsPreview == t.IsPreview && (
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

		bool overCloseOnPress;
		bool allowDoubleClick;

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			var t = FindInAllTabs ((int)evnt.X, (int)evnt.Y);
			if (t != null) {
				if (evnt.IsContextMenuButton ()) {
					DockNotebook.ActiveNotebook = notebook;
					notebook.CurrentTab = t;
					notebook.DoPopupMenu (notebook, t, evnt);
					return true;
				}
				// Don't select the tab if we are clicking the close button
				if (IsOverCloseButton (t, (int)evnt.X, (int)evnt.Y)) {
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

			if (!draggingTab && overCloseOnPress) {
				var t = FindInAllTabs ((int)evnt.X, (int)evnt.Y);
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
					f => dragXProgress = f,
					1.0d,
					0.0d,
					easing: Easing.CubicOut,
					finished: (f, a) => draggingTab = false);
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
				notebook.DoPopupMenu (notebook, tab, evnt);
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}

		Widget currentFocus = null;
		DockNotebookTab currentFocusTab = null;
		bool currentFocusCloseButton;

		protected override void OnActivate ()
		{
			if (currentFocusTab == null) {
				return;
			}

			if (currentFocusCloseButton) {

				var tab = notebook.CurrentTab;

				//var index = tab.Idex
				notebook.OnCloseTab (tab);

				var tabCollection = tab.IsPreview ? notebook.PreviewTabs : notebook.NormalTabs;

				currentFocusTab = tab.Index > 0 ? tabCollection[tab.Index-1] : (DockNotebookTab)null;

				currentFocusCloseButton = true;

				// Focus the next tab
				OnFocused (DirectionType.TabForward);
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
			var currentFocusTabCollection = currentFocusTab == null || !currentFocusTab.IsPreview ? notebook.NormalTabs : notebook.PreviewTabs;

			switch (widget) {
			case FocusWidget.BackButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (currentFocusTabCollection.Count > 0) {
						currentFocusTab = currentFocusTabCollection.FirstOrDefault ();
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
					if (currentFocusTabCollection.Count > 0) {
						currentFocusTab = currentFocusTabCollection.FirstOrDefault ();
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
					if (currentFocusTab.Index > 0) {
						currentFocusTab = currentFocusTabCollection[currentFocusTab.Index-1];
						currentFocusCloseButton = true;
						return FocusWidget.TabCloseButton;
					} else if (NextButton.Sensitive && NextButton.Visible) {
						currentFocusTab = null;
						return FocusWidget.NextButton;
					} else if (PreviousButton.Sensitive && PreviousButton.Visible) {
						currentFocusTab = null;
						return FocusWidget.BackButton;
					} else {
						currentFocusTab = null;
						return FocusWidget.None;
					}
				}
				break;

			case FocusWidget.TabCloseButton:
				currentFocusCloseButton = false;
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
					if (currentFocusTab.Index < currentFocusTabCollection.Count - 1) {
						currentFocusTab = currentFocusTabCollection[currentFocusTab.Index+1];
						return FocusWidget.Tabs;
					} else if (DropDownButton.Sensitive && DropDownButton.Visible) {
						currentFocusTab = null;
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
					if (currentFocusTabCollection.Count > 0) {
						currentFocusTab = currentFocusTabCollection[currentFocusTabCollection.Count-1];
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
					} else if (NextButton.Sensitive && NextButton.Visible) {
						return FocusWidget.NextButton;
					} else if (currentFocusTabCollection.Count > 0) {
						currentFocusTab = currentFocusTabCollection.FirstOrDefault ();
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
					} else if (currentFocusTabCollection.Count > 0) {
						currentFocusTab = currentFocusTabCollection[currentFocusTabCollection.Count - 1];
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
				currentFocusTab = null;
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
			draggingTab = false;
			dragX = 0;
			this.AbortAnimation ("EndDrag");
			base.OnUnrealized ();
		}

		DockNotebookTab FindInAllTabs (int x, int y)
		{
			return tabContainer.FindTab (x, y) ?? previewTabContainer.FindTab (x, y);
		}
	
		static bool IsOverCloseButton (DockNotebookTab tab, int x, int y)
		{
			return tab != null && tab.CloseButtonActiveArea.Contains (x, y);
		}

		public void Update ()
		{
			if (!tracker.Hovered) {
				UpdateTabWidthBasedInContentSize ();
			} else if (closingTabs.Count > 0) {
				var tab = closingTabs[0];
				if (!tab.IsPreview) {
					UpdateTabWidth (tab.Allocation.Right - tabContainer.ContentStartX, true);
				}
			}
			QueueDraw ();
		}

		static int Clamp (int val, int min, int max)
		{
			return Math.Max (min, Math.Min (max, val));
		}

		int GetRenderOffset ()
		{
			return tabContainer.ContentStartX;

			if (notebook.CurrentTab?.IsPreview ?? true) {
				return tabContainer.ContentStartX;
			}
			if (notebook.CurrentTab.Index >= 0) {
				int normalizedArea = (tabContainer.ContentWidth / TargetWidth) * TargetWidth;
				int maxOffset = Math.Max (0, (tabContainer.Tabs.Count * TargetWidth) - normalizedArea);

				int distanceToTabEdge = tabContainer.ContentWidth * notebook.CurrentTab.Index;
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
			return tabContainer.ContentStartX - renderOffset;
		}

		Action<Context> DrawClosingTab (TabContainer container,  DockNotebookTab tab, Gdk.Rectangle region, int tabWidth, out int width)
		{
			width = (int)(tab.WidthModifier * tabWidth);
			int tmp = width;
			return c => DrawTab (c, tab, container, new Gdk.Rectangle (region.X, region.Y, tmp, region.Height), false, false, false, CreateTabLayout (tab), false);
		}

		void Draw (Context ctx)
		{
			var allocation = Allocation;
			int tabArea = tabContainer.ContentWidth;
			var contentStartX = tabContainer.ContentStartX;
			var contentEndX = tabContainer.ContentEndX;

			// background image based in preview tabs
			ctx.DrawImage (this, (previewTabContainer.Tabs.Count > 0 ? tabbarPreviewBackImage : tabbarBackImage).WithSize (allocation.Width, allocation.Height), 0, 0);

			//Draw container background
			//ctx.SetSourceColor (new Cairo.Color (0, 0.2, 0.3));
			//ctx.Rectangle (tabContainer.ContentStartX, allocation.Y, tabContainer.Width , allocation.Height);
			//ctx.Fill ();

			//Draw  previewTabContainer background
			//ctx.SetSourceColor (new Cairo.Color (0.7, 0.6, 0.6));
			//ctx.Rectangle(previewTabContainer.ContentStartX, allocation.Y, previewTabContainer.Width, allocation.Height);
			//ctx.Fill ();

			var tabsData = CalculateTabs (tabContainer, GetRenderOffset ());
			var tabsPreviewData = CalculateTabs (previewTabContainer, previewTabContainer.ContentStartX);

			// Draw breadcrumb bar header
			//			if (notebook.Tabs.Count > 0) {
			//				ctx.Rectangle (0, allocation.Height - BottomBarPadding, allocation.Width, BottomBarPadding);
			//				ctx.SetSourceColor (Styles.BreadcrumbBackgroundColor);
			//				ctx.Fill ();
			//			}

			//Clipping rectangles
			//ctx.Rectangle(tabContainer.ContentStartX, allocation.Y, tabContainer.Width + previewTabContainer.Width + LeanWidth * 3, allocation.Height);
			//ctx.Clip ();

			//ctx.Rectangle(tabContainer.ContentStartX - LeanWidth, allocation.Y, tabContainer.Width + TabWidth + LeanWidth * 3, allocation.Height);
			//ctx.Clip ();

			foreach (var cmd in tabsData.DrawCommands)
				cmd (ctx);

			//ctx.Rectangle (previewTabContainer.ContentStartX - LeanWidth, allocation.Y, previewTabContainer.Width + LeanWidth * 3, allocation.Height);
			//ctx.Clip ();
		
			foreach (var cmd in tabsPreviewData.DrawCommands)
				cmd (ctx);

			foreach (var cmd in tabsData.DrawClosingCommands)
				cmd (ctx);

			foreach (var cmd in tabsPreviewData.DrawClosingCommands)
				cmd (ctx);

			//ctx.ResetClip ();

			// Redraw the dragging tab here to be sure its on top. We drew it before to get the sizing correct, this should be fixed.
			tabsData.DrawActive?.Invoke (ctx);
			tabsPreviewData.DrawActive?.Invoke (ctx);

			if (HasFocus) {
				DrawFocusRectangle (tabsData.FocusRect != Gdk.Rectangle.Zero ? tabsData.FocusRect : tabsPreviewData.FocusRect);
			}
		}

		(List<Action<Context>> DrawCommands, List<Action<Context>> DrawClosingCommands, Action<Context> DrawActive, Gdk.Rectangle FocusRect) CalculateTabs (TabContainer container, int offsetX)
		{
			Action<Context> drawActive = null;
			var drawCommands = new List<Action<Context>> ();
			var drawClosingCommands = new List<Action<Context>> ();

			Gdk.Rectangle focusRect = Gdk.Rectangle.Zero;
			const int y = 0;

			int n = 0;

			for (; n < container.Tabs.Count; n++) {

				if (offsetX + TabWidth < container.ContentStartX) {
					offsetX += TabWidth;
					continue;
				}

				if (offsetX > container.ContentEndX)
					break;

				var tab = container.Tabs [n];

				int closingWidth;

				if (closingTabs.Contains (tab)) {
					drawClosingCommands.Add (DrawClosingTab (container, tab, new Gdk.Rectangle (offsetX, y, 0, Allocation.Height), TabWidth, out closingWidth));
					offsetX += closingWidth;
				}

				bool active = tab == notebook.CurrentTab;
				bool focused = (tab == currentFocusTab);

				int width = Math.Min (TabWidth, Math.Max (50, container.ContentEndX - offsetX - 1));
				if (tab == container.Tabs.Last ())
					width += LastTabWidthAdjustment;
				width = (int)(width * tab.WidthModifier);

				if (active) {
					int tmp = offsetX;
					drawActive = c => DrawTab (c, tab, container, new Gdk.Rectangle (tmp, y, width, Allocation.Height), true, true, draggingTab, CreateTabLayout (tab, true), focused);
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				} else {
					int tmp = offsetX;
					bool highlighted = tab == highlightedTab;

					if (tab.SaveStrength > 0.0f) {
						tmp = (int)(tab.SavedAllocation.X + (tmp - tab.SavedAllocation.X) * (1.0f - tab.SaveStrength));
					}

					drawCommands.Add (c => DrawTab (c, tab, container, new Gdk.Rectangle (tmp, y, width, Allocation.Height), highlighted, false, false, CreateTabLayout (tab), focused));
					tab.Allocation = new Gdk.Rectangle (tmp, Allocation.Y, width, Allocation.Height);
				}

				if (focused) {
					if (currentFocusCloseButton) {
						focusRect = new Gdk.Rectangle ((int)tab.CloseButtonActiveArea.X, (int)tab.CloseButtonActiveArea.Y, (int)tab.CloseButtonActiveArea.Width, (int)tab.CloseButtonActiveArea.Height);
					} else {
						focusRect = new Gdk.Rectangle (tab.Allocation.X + 5, tab.Allocation.Y + 10, tab.Allocation.Width - 30, tab.Allocation.Height - 15);
					}
				}
				offsetX += width;
			}

			//int expectedTabWidth;
			//drawCommands.Add (DrawClosingTab (container, n, new Gdk.Rectangle (offsetX, y, 0, Allocation.Height), TabWidth, out expectedTabWidth));
			drawCommands.Reverse ();
			return (drawCommands, drawClosingCommands, drawActive, focusRect);
		}

		void DrawFocusRectangle (Gdk.Rectangle rect)
		{
			Gtk.Style.PaintFocus (Style, GdkWindow, State, rect, this, "tab", rect.X, rect.Y, rect.Width, rect.Height);
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var context = CairoHelper.Create (evnt.Window)) {
				Draw (context);
			}
			return base.OnExposeEvent (evnt);
		}

		void DrawTab (Context ctx, DockNotebookTab tab, TabContainer container, Gdk.Rectangle tabBounds, bool highlight, bool active, bool dragging, Pango.Layout la, bool focused)
		{
			// This logic is stupid to have here, should be in the caller!
			if (dragging) {
				tabBounds.X = (int)(tabBounds.X + (dragX - tabBounds.X) * dragXProgress);
				tabBounds.X = Clamp (tabBounds.X, container.ContentStartX, container.ContentEndX - tabBounds.Width);
			}

			double rightPadding = (active ? TabActivePadding.Right : TabPadding.Right) - HalfLeanWidth;
			rightPadding = (rightPadding * Math.Min (1.0, Math.Max (0.5, (tabBounds.Width - 30) / 70.0)));
			double leftPadding = (active ? TabActivePadding.Left : TabPadding.Left) - HalfLeanWidth;
			leftPadding = (leftPadding * Math.Min (1.0, Math.Max (0.5, (tabBounds.Width - 30) / 70.0)));
			double bottomPadding = active ? TabActivePadding.Bottom : TabPadding.Bottom;

			DrawTabBackground (this, tab, ctx, Allocation.Height, tabBounds.Width, tabBounds.X, active);

			ctx.LineWidth = 1;
			ctx.NewPath ();

			// Render Close Button (do this first so we can tell how much text to render)

			var closeButtonAlloation = new Cairo.Rectangle (tabBounds.Right - rightPadding - (tabCloseImage.Width / 2) - CloseButtonMarginRight,
											 tabBounds.Height - bottomPadding - tabCloseImage.Height - CloseButtonMarginBottom,
											 tabCloseImage.Width, tabCloseImage.Height);

			tab.CloseButtonActiveArea = closeButtonAlloation.Inflate (2, 2);

			bool closeButtonHovered = tracker.Hovered && tab.CloseButtonActiveArea.Contains (tracker.MousePosition);
			bool tabHovered = tracker.Hovered && tab.Allocation.Contains (tracker.MousePosition);
			bool drawCloseButton = active || tabHovered || focused;

			if (!closeButtonHovered && tab.DirtyStrength > 0.5) {
				ctx.DrawImage (this, tabDirtyImage, closeButtonAlloation.X, closeButtonAlloation.Y);
				drawCloseButton = false;
			}

			if (drawCloseButton)
				ctx.DrawImage (this, tabCloseImage.WithAlpha ((closeButtonHovered ? 1.0 : 0.5) * tab.Opacity), closeButtonAlloation.X, closeButtonAlloation.Y);

			// Render Text
			double tw = tabBounds.Width - (leftPadding + rightPadding);
			if (drawCloseButton || tab.DirtyStrength > 0.5)
				tw -= closeButtonAlloation.Width / 2;

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

		static void DrawTabBackground (Widget widget, DockNotebookTab tab, Context ctx, int height, int contentWidth, int px, bool active = true)
		{
			int lean = Math.Min (LeanWidth, contentWidth / 2);
			int halfLean = lean / 2;

			double x = px + TabSpacing - halfLean;
			double y = 0;
			double width = contentWidth - (TabSpacing * 2) + lean;

			Xwt.Drawing.Image image;
			if (tab.IsPreview) {
				image = active ? tabPreviewActiveBackImage : tabPreviewBackImage;
			} else {
				image = active ? tabActiveBackImage : tabBackImage;
			}
		
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

		public void UpdateTabWidthBasedInContentSize ()
		{
			UpdateTabWidth (Allocation.Width - LeftBarPadding - RightBarPadding - 11);
		}

		public void UpdateTabWidth (int width, bool adjustLast = false)
		{
			if (width <= 0) {
				return;
			}

			var totalTabCount = notebook.AllTabs.Count ();

			if (totalTabCount > 0) {
				TargetWidth = Clamp (width / totalTabCount, 50, 200);
			}

			if (adjustLast) {
				// adjust to align close buttons properly
				LastTabWidthAdjustment = width - (TargetWidth * totalTabCount) + 1;
				LastTabWidthAdjustment = Math.Abs (LastTabWidthAdjustment) < 50 ? LastTabWidthAdjustment : 0;
			} else {
				LastTabWidthAdjustment = 0;
			}
			if (!IsRealized)
				TabWidth = TargetWidth;
		}

		class TabContainer
		{
			readonly public System.Collections.ObjectModel.ReadOnlyCollection<DockNotebookTab> Tabs;
			readonly TabStrip tabStrip;

			public int Width => tabStrip.TabWidth * Tabs.Count;

			public Gdk.Rectangle Allocation => new Gdk.Rectangle (tabStrip.Allocation.Width - Width + TabStrip.TabSeparationBorder, tabStrip.Allocation.Y, Width - TabStrip.TabSeparationBorder, tabStrip.Allocation.Height);

			public TabContainer (TabStrip tabStrip, bool previewContainer)
			{
				this.tabStrip = tabStrip;
				this.Tabs = previewContainer ? tabStrip.notebook.PreviewTabs : tabStrip.notebook.NormalTabs;
			}

			public DockNotebookTab FindTab (int x, int y)
			{
				var currentTab = this.tabStrip.notebook.CurrentTab;
				if (currentTab != null) {
					var allocWithLean = currentTab.Allocation;
					allocWithLean.X -= HalfLeanWidth;
					allocWithLean.Width += LeanWidth;
					if (allocWithLean.Contains (x, y))
						return currentTab;
				}

				for (int n = 0; n < Tabs.Count; n++) {
					var tab = (DockNotebookTab)Tabs.ElementAt (n);
					if (tab.Allocation.Contains (x, y))
						return tab;
				}
				return null;
			}

			internal int ContentStartX, ContentEndX;
			internal int ContentWidth => ContentEndX - ContentStartX;

			public bool IsOverArea (int x)
			{
				return Allocation.Contains (x, Allocation.Y);
			}
		}
	}
}
