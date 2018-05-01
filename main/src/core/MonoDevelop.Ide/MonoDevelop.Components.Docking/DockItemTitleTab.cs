//
// DockItemTitleTab.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using Gtk;

using System;
using MonoDevelop.Ide.Gui;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Components.Docking
{
	class DockItemTitleTab
	{
		internal IDockItemTitleTabControl Control { get; set; }
		internal DockItem Item { get; private set; }
		internal DockFrame Frame { get; private set; }

		internal static Xwt.Drawing.Image DockTabActiveBackImage = Xwt.Drawing.Image.FromResource ("padbar-active.9.png");
		internal static Xwt.Drawing.Image DockTabBackImage = Xwt.Drawing.Image.FromResource ("padbar-inactive.9.png");

		internal static Xwt.Drawing.Image PixClose;
		internal static Xwt.Drawing.Image PixAutoHide;
		internal static Xwt.Drawing.Image PixDock;

		internal static readonly Xwt.WidgetSpacing TabPadding;
		internal static readonly Xwt.WidgetSpacing TabActivePadding;

		static DockItemTitleTab ()
		{
			PixClose = Xwt.Drawing.Image.FromResource ("pad-close-9.png");
			PixAutoHide = Xwt.Drawing.Image.FromResource ("pad-minimize-9.png");
			PixDock = Xwt.Drawing.Image.FromResource ("pad-dock-9.png");

			Xwt.Drawing.NinePatchImage tabBackImage9;
			if (DockTabBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)DockTabBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabBackImage9 = DockTabBackImage as Xwt.Drawing.NinePatchImage;
			TabPadding = tabBackImage9.Padding;


			Xwt.Drawing.NinePatchImage tabActiveBackImage9;
			if (DockTabActiveBackImage is Xwt.Drawing.ThemedImage) {
				var img = ((Xwt.Drawing.ThemedImage)DockTabActiveBackImage).GetImage (Xwt.Drawing.Context.GlobalStyles);
				tabActiveBackImage9 = img as Xwt.Drawing.NinePatchImage;
			} else
				tabActiveBackImage9 = DockTabActiveBackImage as Xwt.Drawing.NinePatchImage;
			TabActivePadding = tabActiveBackImage9.Padding;
		}

		internal DockItemTitleTab (DockItem item, DockFrame frame)
		{
			Item = item;
			Frame = frame;

			Control = new DockItemTitleTabControl ();
			Control.Initialize (this);
		}

		internal bool BelongsToTabStrip { get => ParentTabStrip != null; }
		internal TabStrip ParentTabStrip { get; set; }
		DockVisualStyle visualStyle;
		public DockVisualStyle VisualStyle {
			get { return visualStyle; }
			set {
				visualStyle = value;
				Control.UpdateVisualStyle ();
				//QueueDraw (); FIXME: Do we need to force a redraw here, we don't redraw any other time we call UpdateVisualStyle
			}
		}

		internal void UpdateRole (bool isTab, TabStrip tabStrip)
		{
			Control.UpdateRole (isTab, tabStrip);
		}

		internal double InactiveIconAlpha {
			get {
				if (IdeApp.Preferences == null || IdeApp.Preferences.UserInterfaceTheme == Theme.Light)
					return 0.8;
				else
					return 0.6;
			}
		}

		public bool Active {
			get {
				return Control.Active;
			}
			set {
				Control.Active = value;
			}
		}

		public Control Page {
			get {
				return Control.Page;
			}
		}

		public void RemoveFromParent ()
		{
			Control.RemoveFromParent ();
		}

		internal void SetLinkedItemContainer (DockItemContainer container)
		{
			Control.SetLinkedItemContainer (container);
		}
	}

	interface IDockItemTitleTabControl
	{
		DockItemTitleTab ParentTab { get; }
		void Initialize (DockItemTitleTab parentTab);
		void UpdateRole (bool isTab, TabStrip tabStrip);
		void UpdateVisualStyle ();
		void UpdateBehavior ();
		bool Active { get; set; }
		Control Page { get; }
		event EventHandler<EventArgs> TabPressed;
		void SetLabel (DockItemContainer container, Xwt.Drawing.Image icon, string label);
		int MinWidth { get; }
		int LabelWidth { get; }
		void ShowAll ();
		Gdk.Rectangle Size { get; set; }
		void RemoveFromParent ();
		void SetLinkedItemContainer (DockItemContainer container);
	}

	class DockItemTitleTabControl : Gtk.EventBox, IDockItemTitleTabControl
	{
		DockItemTitleTab parentTab;

		bool active;
		Control page;
		ExtendedLabel labelWidget;
		int labelWidth;
		int minWidth;
		ImageView tabIcon;
		Gtk.HBox box;
		string label;
		ImageButton btnDock;
		ImageButton btnClose;
		Gtk.Alignment al;
		bool allowPlaceholderDocking;
		bool mouseOver;
		Widget currentFocus = null; // Currently focused child

		IDisposable subscribedLeaveEvent;

		static Gdk.Cursor fleurCursor = new Gdk.Cursor (Gdk.CursorType.Fleur);

		public event EventHandler<EventArgs> TabPressed;

		public DockItemTitleTabControl ()
		{
			var actionHandler = new ActionDelegate (this);
			actionHandler.PerformPress += HandlePress;
			actionHandler.PerformShowMenu += HandleShowMenu;

			UpdateRole (false, null);

			CanFocus = true;

			this.VisibleWindow = false;
			NoShowAll = true;

			Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask;
			KeyPressEvent += HeaderKeyPress;
			KeyReleaseEvent += HeaderKeyRelease;

			subscribedLeaveEvent = this.SubscribeLeaveEvent (OnLeave);
		}

		public void Initialize (DockItemTitleTab tab)
		{
			parentTab = tab;
			UpdateVisualStyle ();
		}

		public void UpdateRole (bool isTab, TabStrip strip)
		{
			Atk.Object fromAccessible = null, toAccessible = null;
			Widget stripControl = null;

			if (strip != null) {
				stripControl = strip.Control as Widget;

				if (stripControl == null) {
					throw new ToolkitMismatchException ();
				}
			}

			if (!isTab) {
				Accessible.SetRole (AtkCocoa.Roles.AXGroup, "pad header");
				Accessible.SetSubRole ("XAPadHeader");

				// Take the button accessibles back from the strip
				if (stripControl != null) {
					fromAccessible = stripControl.Accessible;
					toAccessible = Accessible;
				}
			} else {
				Accessible.SetRole (AtkCocoa.Roles.AXRadioButton, "tab");
				Accessible.SetSubRole ("");

				// Give the button accessibles to the strip
				if (stripControl != null) {
					fromAccessible = Accessible;
					toAccessible = stripControl.Accessible;
				}
			}

			if (fromAccessible != null && toAccessible != null) {
				fromAccessible.TransferAccessibleChild (toAccessible, btnDock.Accessible);
				fromAccessible.TransferAccessibleChild (toAccessible, btnClose.Accessible);
			}
		}

		public DockItemTitleTab ParentTab {
			get {
				return parentTab;
			}
		}

		protected override void OnDestroyed ()
		{
			subscribedLeaveEvent.Dispose ();
			base.OnDestroyed ();
		}

		public void UpdateVisualStyle ()
		{

			if (labelWidget?.Visible == true && label != null) {
				if (parentTab.VisualStyle.UppercaseTitles.Value)
					labelWidget.Text = label.ToUpper ();
				else
					labelWidget.Text = label;
				labelWidget.UseMarkup = true;
				if (parentTab.VisualStyle.ExpandedTabs.Value)
					labelWidget.Xalign = 0.5f;

				if (!parentTab.BelongsToTabStrip) {
					labelWidget.Xalign = 0;
				}
			}

			if (tabIcon != null) {
				tabIcon.Image = tabIcon.Image.WithAlpha (active ? 1.0 : parentTab.InactiveIconAlpha);
				tabIcon.Visible = parentTab.VisualStyle.ShowPadTitleIcon.Value;
			}

			if (IsRealized && labelWidget?.Visible == true) {
				var font = FontService.SansFont.CopyModified (null, Pango.Weight.Bold);
				font.AbsoluteSize = Pango.Units.FromPixels (11);
				labelWidget.ModifyFont (font);
				labelWidget.ModifyText (StateType.Normal, (active ? parentTab.VisualStyle.PadTitleLabelColor.Value : parentTab.VisualStyle.InactivePadTitleLabelColor.Value).ToGdkColor ());
			}

			var r = WidthRequest;
			WidthRequest = -1;
			labelWidth = SizeRequest ().Width + 1;
			WidthRequest = r;

			if (parentTab?.VisualStyle != null)
				HeightRequest = parentTab.VisualStyle.PadTitleHeight != null ? (int)(parentTab.VisualStyle.PadTitleHeight.Value) : -1;
		}

		public void SetLabel (DockItemContainer container, Xwt.Drawing.Image icon, string label)
		{
			string labelNoSpaces = label != null ? label.Replace (' ', '-') : null;
			this.label = label;
			this.page = container.Control;

			if (icon == null)
				icon = ImageService.GetIcon ("md-empty");

			if (box == null) {
				box = new HBox ();
				box.Accessible.SetShouldIgnore (true);
				box.Spacing = -2;

				tabIcon = new ImageView ();
				tabIcon.Accessible.SetShouldIgnore (true);
				tabIcon.Show ();
				box.PackStart (tabIcon, false, false, 3);

				labelWidget = new ExtendedLabel (label);
				// Ignore the label because the title tab already contains its name
				labelWidget.Accessible.SetShouldIgnore (true);
				labelWidget.UseMarkup = true;
				var alignLabel = new Alignment (0.0f, 0.5f, 1, 1);
				alignLabel.Accessible.SetShouldIgnore (true);
				alignLabel.BottomPadding = 0;
				alignLabel.RightPadding = 15;
				alignLabel.Add (labelWidget);
				box.PackStart (alignLabel, false, false, 0);

				btnDock = new ImageButton ();
				btnDock.Image = DockItemTitleTab.PixAutoHide;
				btnDock.TooltipText = GettextCatalog.GetString ("Auto Hide");
				btnDock.CanFocus = true;
				//			btnDock.WidthRequest = btnDock.HeightRequest = 17;
				btnDock.Clicked += OnClickDock;
				btnDock.ButtonPressEvent += (o, args) => args.RetVal = true;
				btnDock.WidthRequest = btnDock.SizeRequest ().Width;
				UpdateDockButtonAccessibilityLabels ();

				btnClose = new ImageButton ();
				btnClose.Image = DockItemTitleTab.PixClose;
				btnClose.TooltipText = GettextCatalog.GetString ("Close");
				btnClose.CanFocus = true;
				//			btnClose.WidthRequest = btnClose.HeightRequest = 17;
				btnClose.WidthRequest = btnDock.SizeRequest ().Width;
				btnClose.Clicked += delegate {
					parentTab.Item.Visible = false;
				};
				btnClose.ButtonPressEvent += (o, args) => args.RetVal = true;

				al = new Alignment (0, 0.5f, 1, 1);
				al.Accessible.SetShouldIgnore (true);
				HBox btnBox = new HBox (false, 0);
				btnBox.Accessible.SetShouldIgnore (true);
				btnBox.PackStart (btnDock, false, false, 3);
				btnBox.PackStart (btnClose, false, false, 1);
				al.Add (btnBox);
				box.PackEnd (al, false, false, 3);

				Add (box);
			}

			tabIcon.Image = icon;

			string realLabel, realHelp;
			if (!string.IsNullOrEmpty (label)) {
				labelWidget.Parent.Show ();
				labelWidget.Name = label;
				btnDock.Name = string.Format ("btnDock_{0}", labelNoSpaces ?? string.Empty);
				btnClose.Name = string.Format ("btnClose_{0}", labelNoSpaces ?? string.Empty);
				realLabel = GettextCatalog.GetString ("Close {0}", label);
				realHelp = GettextCatalog.GetString ("Close the {0} pad", label);
			} else {
				labelWidget.Parent.Hide ();
				realLabel = GettextCatalog.GetString ("Close pad");
				realHelp = GettextCatalog.GetString ("Close the pad");
			}

			btnClose.Accessible.SetLabel (realLabel);
			btnClose.Accessible.Description = realHelp;

			if (label != null) {
				Accessible.Name = $"DockTab.{labelNoSpaces}";
				Accessible.Description = GettextCatalog.GetString ("Switch to the {0} tab", label);
				Accessible.SetTitle (label);
				Accessible.SetLabel (label);
			}

			// Get the required size before setting the ellipsize property, since ellipsized labels
			// have a width request of 0
			box.ShowAll ();
			Show ();

			minWidth = tabIcon.SizeRequest ().Width + al.SizeRequest ().Width + 10;

			UpdateBehavior ();
			UpdateVisualStyle ();
		}

		void UpdateDockButtonAccessibilityLabels ()
		{
			string realLabel;
			string realHelp;
			bool dockable = parentTab.Item.Status != DockItemStatus.Dockable;

			if (string.IsNullOrEmpty (label)) {
				if (dockable) {
					realLabel = GettextCatalog.GetString ("Dock pad");
					realHelp = GettextCatalog.GetString ("Dock the pad into the UI so it will not hide automatically");
				} else {
					realLabel = GettextCatalog.GetString ("Autohide pad");
					realHelp = GettextCatalog.GetString ("Automatically hide the pad when it loses focus");
				}
			} else {
				if (dockable) {
					realLabel = GettextCatalog.GetString ("Dock {0}", label);
					realHelp = GettextCatalog.GetString ("Dock the {0} pad into the UI so it will not hide automatically", label);
				} else {
					realLabel = GettextCatalog.GetString ("Autohide {0}", label);
					realHelp = GettextCatalog.GetString ("Automatically hide the {0} pad when it loses focus", label);
				}
			}
			btnDock.Accessible.SetLabel (realLabel);
			btnDock.Accessible.Description = realHelp;
		}

		public void RemoveFromParent ()
		{
			if (Parent == null) {
				return;
			}

			((Gtk.Container)Parent).Remove (this);
		}

		void OnClickDock (object s, EventArgs a)
		{
			if (parentTab.Item.Status == DockItemStatus.AutoHide || parentTab.Item.Status == DockItemStatus.Floating)
				parentTab.Item.Status = DockItemStatus.Dockable;
			else
				parentTab.Item.Status = DockItemStatus.AutoHide;
		}

		public int LabelWidth {
			get { return labelWidth; }
		}

		public int MinWidth {
			get { return minWidth; }
		}

		public bool Active {
			get {
				return active;
			}
			set {
				if (active != value) {
					active = value;
					UpdateVisualStyle ();
					QueueResize ();
					QueueDraw ();
					UpdateBehavior ();
				}
			}
		}

		public Control Page {
			get {
				return page;
			}
		}

		public Gdk.Rectangle Size {
			get {
				var req = SizeRequest ();
				return new Gdk.Rectangle (0, 0, req.Width, req.Height);
			}

			set {
				SizeAllocate (value);
			}
		}

		public void UpdateBehavior ()
		{
			if (btnClose == null)
				return;

			btnClose.Visible = (parentTab.Item.Behavior & DockItemBehavior.CantClose) == 0;
			btnDock.Visible = (parentTab.Item.Behavior & DockItemBehavior.CantAutoHide) == 0;

			if (active || mouseOver) {
				if (btnClose.Image == null)
					btnClose.Image = DockItemTitleTab.PixClose;
				if (parentTab.Item.Status == DockItemStatus.AutoHide || parentTab.Item.Status == DockItemStatus.Floating) {
					btnDock.Image = DockItemTitleTab.PixDock;
					btnDock.TooltipText = GettextCatalog.GetString ("Dock");
				} else {
					btnDock.Image = DockItemTitleTab.PixAutoHide;
					btnDock.TooltipText = GettextCatalog.GetString ("Auto Hide");
				}
			} else {
				btnDock.Image = null;
				btnClose.Image = null;
			}

			UpdateDockButtonAccessibilityLabels ();
		}

		public void SetLinkedItemContainer (DockItemContainer container)
		{
			var widget = container.ContainerControl as Gtk.Widget;

			Accessible.AddLinkedUIElement (widget.Accessible);
		}

		bool tabPressed, tabActivated;
		double pressX, pressY;

		protected override void OnActivate ()
		{
			TabPressed?.Invoke (this, EventArgs.Empty);

			base.OnActivate ();
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.TriggersContextMenu ()) {
				parentTab.Item.ShowDockPopupMenu (this, evnt);
				return false;
			} else if (evnt.Button == 1) {
				if (evnt.Type == Gdk.EventType.ButtonPress) {
					tabPressed = true;
					pressX = evnt.X;
					pressY = evnt.Y;
					TabPressed?.Invoke (this, EventArgs.Empty);
				} else if (evnt.Type == Gdk.EventType.TwoButtonPress) {
					tabActivated = true;
				}
			}
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			var item = parentTab.Item;
			var frame = parentTab.Frame;
			if (tabActivated) {
				tabActivated = false;
				if (!item.Behavior.HasFlag (DockItemBehavior.CantAutoHide)) {
					if (item.Status == DockItemStatus.AutoHide)
						item.Status = DockItemStatus.Dockable;
					else
						item.Status = DockItemStatus.AutoHide;
				}
			} else if (!evnt.TriggersContextMenu () && evnt.Button == 1) {
				frame.DockInPlaceholder (item);
				frame.HidePlaceholder ();
				if (GdkWindow != null)
					GdkWindow.Cursor = null;
				var controlWidget = frame.Control as Widget;
				if (controlWidget != null) {
					controlWidget.Toplevel.KeyPressEvent -= HeaderKeyPress;
					controlWidget.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
				} else {
					throw new ToolkitMismatchException ();
				}
			}
			tabPressed = false;
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			mouseOver = true;
			UpdateBehavior ();
			return base.OnFocusInEvent (evnt);
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			if (currentFocus == null) {
				mouseOver = false;
				UpdateBehavior ();
			}
			return base.OnFocusOutEvent (evnt);
		}

		void HandlePress (object sender, EventArgs args)
		{
			TabPressed?.Invoke (this, EventArgs.Empty);
		}

		void HandleShowMenu (object sender, EventArgs args)
		{
			// Show the menu at the middle of the widget
			parentTab.Item.ShowDockPopupMenu (this, Allocation.Width / 2, Allocation.Height / 2);
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			var item = parentTab.Item;
			var frame = parentTab.Frame;
			if (tabPressed && !item.Behavior.HasFlag (DockItemBehavior.NoGrip) && Math.Abs (evnt.X - pressX) > 3 && Math.Abs (evnt.Y - pressY) > 3) {
				frame.ShowPlaceholder (item);
				GdkWindow.Cursor = fleurCursor;
				var widgetControl = frame.Control as Widget;
				if (widgetControl != null) {
					widgetControl.Toplevel.KeyPressEvent += HeaderKeyPress;
					widgetControl.Toplevel.KeyReleaseEvent += HeaderKeyRelease;
				}
				allowPlaceholderDocking = true;
				tabPressed = false;
			}
			frame.UpdatePlaceholder (item, Allocation.Size, allowPlaceholderDocking);
			return base.OnMotionNotifyEvent (evnt);
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			mouseOver = true;
			UpdateBehavior ();
			return base.OnEnterNotifyEvent (evnt);
		}

		void OnLeave ()
		{
			mouseOver = false;
			UpdateBehavior ();
		}

		enum FocusWidget
		{
			None,
			Widget,
			DockButton,
			CloseButton
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
			case FocusWidget.Widget:
				GrabFocus ();
				currentFocus = null;
				return true;

			case FocusWidget.DockButton:
				currentFocus = btnDock;
				return btnDock.ChildFocus (direction);

			case FocusWidget.CloseButton:
				currentFocus = btnClose;
				return btnClose.ChildFocus (direction);

			case FocusWidget.None:
				break;
			}

			return false;
		}

		FocusWidget GetNextWidgetToFocus (FocusWidget widget, DirectionType direction)
		{
			FocusWidget nextSite;

			switch (widget) {
			case FocusWidget.CloseButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
				case DirectionType.Down:
					return FocusWidget.None;

				case DirectionType.TabBackward:
				case DirectionType.Left:
				case DirectionType.Up:
					if (btnDock.Image != null) {
						nextSite = FocusWidget.DockButton;
					} else {
						nextSite = FocusWidget.Widget;
					}
					return nextSite;
				}

				break;

			case FocusWidget.DockButton:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
				case DirectionType.Down:
					return btnClose.Image == null ? FocusWidget.None : FocusWidget.CloseButton;

				case DirectionType.TabBackward:
				case DirectionType.Left:
				case DirectionType.Up:
					return FocusWidget.Widget;
				}

				break;

			case FocusWidget.Widget:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
				case DirectionType.Down:
					if (btnDock.Image != null) {
						nextSite = FocusWidget.DockButton;
					} else if (btnClose.Image != null) {
						nextSite = FocusWidget.CloseButton;
					} else {
						nextSite = FocusWidget.None;
					}
					return nextSite;

				case DirectionType.TabBackward:
				case DirectionType.Left:
				case DirectionType.Up:
					return FocusWidget.None;
				}

				break;
			case FocusWidget.None:
				switch (direction) {
				case DirectionType.TabForward:
				case DirectionType.Right:
				case DirectionType.Down:
					return FocusWidget.Widget;

				case DirectionType.TabBackward:
				case DirectionType.Left:
				case DirectionType.Up:
					if (btnClose.Image != null) {
						nextSite = FocusWidget.CloseButton;
					} else if (btnDock.Image != null) {
						nextSite = FocusWidget.DockButton;
					} else {
						nextSite = FocusWidget.Widget;
					}
					return nextSite;
				}

				break;
			}

			return FocusWidget.None;
		}

		protected override bool OnFocused (DirectionType direction)
		{
			if (!FocusCurrentWidget (direction)) {
				FocusWidget focus = FocusWidget.None;

				if (currentFocus == btnClose) {
					focus = FocusWidget.CloseButton;
				} else if (currentFocus == btnDock) {
					focus = FocusWidget.DockButton;
				} else if (IsFocus) {
					focus = FocusWidget.Widget;
				}

				while ((focus = GetNextWidgetToFocus (focus, direction)) != FocusWidget.None) {
					if (MoveFocusToWidget (focus, direction)) {
						return true;
					}
				}

				// Clean up the icons because OnFocusOutEvent has already been called
				// so we need the icons to hide again
				mouseOver = false;
				UpdateBehavior ();

				currentFocus = null;
				return false;
			}

			return true;
		}

		[GLib.ConnectBeforeAttribute]
		void HeaderKeyPress (object ob, Gtk.KeyPressEventArgs a)
		{
			var item = parentTab.Item;
			var frame = parentTab.Frame;
			if (a.Event.Key == Gdk.Key.Control_L || a.Event.Key == Gdk.Key.Control_R) {
				allowPlaceholderDocking = false;
				frame.UpdatePlaceholder (item, Allocation.Size, false);
			}
			if (a.Event.Key == Gdk.Key.Escape) {
				frame.HidePlaceholder ();

				var widgetControl = frame.Control as Widget;
				if (widgetControl != null) {
					widgetControl.Toplevel.KeyPressEvent -= HeaderKeyPress;
					widgetControl.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
				} else {
					throw new ToolkitMismatchException ();
				}
				Gdk.Pointer.Ungrab (0);
			}
		}
		
		[GLib.ConnectBeforeAttribute]
		void HeaderKeyRelease (object ob, Gtk.KeyReleaseEventArgs a)
		{
			if (a.Event.Key == Gdk.Key.Control_L || a.Event.Key == Gdk.Key.Control_R) {
				allowPlaceholderDocking = true;
				parentTab.Frame.UpdatePlaceholder (parentTab.Item, Allocation.Size, true);
			}
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			UpdateVisualStyle ();
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			if (Child != null) {
				req = Child.SizeRequest ();
				req.Width += (int)(DockItemTitleTab.TabPadding.Left + DockItemTitleTab.TabPadding.Right);
				if (active)
					req.Height += (int)(DockItemTitleTab.TabActivePadding.Top + DockItemTitleTab.TabActivePadding.Bottom);
				else
					req.Height += (int)(DockItemTitleTab.TabPadding.Top + DockItemTitleTab.TabPadding.Bottom);
			}
		}
					
		protected override void OnSizeAllocated (Gdk.Rectangle rect)
		{
			base.OnSizeAllocated (rect);

			int leftPadding = (int)DockItemTitleTab.TabPadding.Left;
			int rightPadding = (int)DockItemTitleTab.TabPadding.Right;
			
			rect.X += leftPadding;
			rect.Width -= leftPadding + rightPadding;
			if (rect.Width < 1) {
				rect.Width = 1;
			}

			if (Child != null) {
				var bottomPadding = active ? (int)DockItemTitleTab.TabActivePadding.Bottom : (int)DockItemTitleTab.TabPadding.Bottom;
				var topPadding = active ? (int)DockItemTitleTab.TabActivePadding.Top : (int)DockItemTitleTab.TabPadding.Top;
				int centerY = topPadding + ((rect.Height - bottomPadding - topPadding) / 2);
				var height = Child.SizeRequest ().Height;
				rect.Y += centerY - (height / 2);
				rect.Height = height;
				Child.SizeAllocate (rect);
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (parentTab.VisualStyle.TabStyle == DockTabStyle.Normal)
				DrawAsBrowser (evnt);
			else
				DrawNormal (evnt);

			if (HasFocus) {
				var alloc = labelWidget.Allocation;
				Gtk.Style.PaintFocus (Style, GdkWindow, State, alloc, this, "label",
				                      alloc.X, alloc.Y, alloc.Width, alloc.Height);
			}
			return base.OnExposeEvent (evnt);
		}

		void DrawAsBrowser (Gdk.EventExpose evnt)
		{
			bool drawAsTab = false;
			if (parentTab.BelongsToTabStrip) {
				drawAsTab = parentTab.ParentTabStrip.TabCount != 0;
			}

			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				if (!drawAsTab) {
					ctx.Rectangle (Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
					ctx.SetSourceColor (parentTab.VisualStyle.PadBackgroundColor.Value.ToCairoColor ());
					ctx.Fill ();
				} else {
					var image = Active ? DockItemTitleTab.DockTabActiveBackImage : DockItemTitleTab.DockTabBackImage;
					image = image.WithSize (Allocation.Width, Allocation.Height);

					ctx.DrawImage (this, image, Allocation.X, Allocation.Y);
				}
			}
		}

		void DrawNormal (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				var x = Allocation.X;
				var y = Allocation.Y;

				ctx.Rectangle (x, y + 1, Allocation.Width, Allocation.Height - 1);
				ctx.SetSourceColor (Styles.DockBarBackground.ToCairoColor ());
				ctx.Fill ();

				/*
				if (active) {
					double offset = Allocation.Height * 0.25;
					var rect = new Cairo.Rectangle (x - Allocation.Height + offset, y, Allocation.Height, Allocation.Height);
					var cg = new Cairo.RadialGradient (rect.X + rect.Width / 2, rect.Y + rect.Height / 2, 0, rect.X, rect.Y + rect.Height / 2, rect.Height / 2);
					cg.AddColorStop (0, Styles.DockTabBarShadowGradientStart);
					cg.AddColorStop (1, Styles.DockTabBarShadowGradientEnd);
					ctx.Pattern = cg;
					ctx.Rectangle (rect);
					ctx.Fill ();

					rect = new Cairo.Rectangle (x + Allocation.Width - offset, y, Allocation.Height, Allocation.Height);
					cg = new Cairo.RadialGradient (rect.X + rect.Width / 2, rect.Y + rect.Height / 2, 0, rect.X, rect.Y + rect.Height / 2, rect.Height / 2);
					cg.AddColorStop (0, Styles.DockTabBarShadowGradientStart);
					cg.AddColorStop (1, Styles.DockTabBarShadowGradientEnd);
					ctx.Pattern = cg;
					ctx.Rectangle (rect);
					ctx.Fill ();
				}
				*/
			}
		}
	}
}
