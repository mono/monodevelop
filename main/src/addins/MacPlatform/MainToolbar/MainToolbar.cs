//
// MainToolbar.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Components.Mac;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide;
using Xwt;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	class MainToolbar : IMainToolbarView
	{
		const string MainToolbarId = "XSMainToolbar";
		const string RunButtonId = "RunToolbarItem";
		const string ButtonBarId = "ButtonBarToolbarItem";
		const string SelectorId = "SelectorToolbarItem";
		const string SearchBarId = "SearchBarToolbarItem";
		const string StatusBarId = "StatusBarToolbarItem";
		const string CenteringSpaceId = "CenteringSpaceToolbarItem";

		internal NSToolbar widget;
		internal Gtk.Window gtkWindow;

		int runButtonIdx;
		RunButton runButton {
			get { return (RunButton)widget.Items[runButtonIdx].View; }
		}

		int buttonBarStartIdx, buttonBarCount;

		CenteringSpaceToolbarItem centeringSpace {
			get { return (CenteringSpaceToolbarItem)widget.Items[buttonBarStartIdx + buttonBarCount]; }
		}

		int statusBarIdx;
		StatusBar statusBar {
			get { return (StatusBar)widget.Items[statusBarIdx + buttonBarCount].View; }
		}

		int selectorIdx;
		SelectorView selector {
			get { return (SelectorView)widget.Items[selectorIdx].View; }
		}

		SelectorView.PathSelectorView selectorView {
			get { return (SelectorView.PathSelectorView)widget.Items[selectorIdx].View.Subviews [0]; }
		}

		int searchEntryIdx;
		SearchBar searchEntry {
			get { return (SearchBar)widget.Items[searchEntryIdx + buttonBarCount].View; }
		}

		// TODO: Remove this when XamMac 2.2 goes stable.
		static HashSet<object> viewCache = new HashSet<object> ();
		static HashSet<ButtonBar> buttonBarCache = new HashSet<ButtonBar> ();

		NSToolbarItem CreateRunToolbarItem ()
		{
			var button = new RunButton ();
			viewCache.Add (button);
			button.Activated += (o, e) => {
				if (RunButtonClicked != null)
					RunButtonClicked (o, e);
			};

			var item = new NSToolbarItem (RunButtonId) {
				View = button,
				MinSize = new CGSize (button.FittingSize.Width + 12, button.FittingSize.Height),
				MaxSize = new CGSize (button.FittingSize.Width + 12, button.FittingSize.Height),
			};
			return item;
		}

		OverflowInfoEventArgs FillOverflowInfo (OverflowInfoEventArgs e)
		{
			var visibleItems = widget.VisibleItems;
			var allItems = widget.Items;

			e.WindowWidth = gtkWindow.Allocation.Width;
			foreach (var iter in allItems) {
				e.AllItemsWidth += iter.MinSize.Width;
				if (!visibleItems.Contains (iter))
					e.ItemsInOverflowWidth += iter.MinSize.Width;
			}
			// Add spacings.
			nfloat spacing = (allItems.Length - 1) * 16;
			e.AllItemsWidth += spacing;

			return e;
		}

		bool IsCorrectNotification (NSView view, NSObject notifObject)
		{
			var window = selector.Window;

			// Skip updates with a null Window. Only crashes on Mavericks.
			// The View gets updated once again when the window resize finishes.
			// We're getting notified about all windows in the application (for example, NSPopovers) that change size when really we only care about
			// the window the bar is in.
			return window != null && notifObject == window;
		}

		NSToolbarItem CreateSelectorToolbarItem ()
		{
			var selector = new SelectorView ();
			viewCache.Add (selector);
			var item = new NSToolbarItem (SelectorId) {
				View = selector,
				MinSize = new CGSize (150, 25),
				MaxSize = new CGSize (150, 25),
			};
			selector.ResizeRequested += (o, e) => {
				item.MinSize = item.MaxSize = e.Size;
				centeringSpace.UpdateWidth ();
			};
			selector.OverflowInfoRequested += (o, e) => {
				FillOverflowInfo (e);
			};

			IDisposable resizeTimer = null;
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.WillStartLiveResizeNotification, notif => DispatchService.GuiDispatch (() => {
				if (!IsCorrectNotification (selector, notif.Object))
					return;

				if (resizeTimer != null)
					resizeTimer.Dispose ();

				resizeTimer = Application.TimeoutInvoke (100, () => {
					if (widget.Items.Length != widget.VisibleItems.Length)
						selector.RequestResize ();
					return true;
				});
			}));

			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResizeNotification, notif => DispatchService.GuiDispatch (() => {
				if (!IsCorrectNotification (selector, notif.Object))
					return;

				// Don't check difference in overflow menus. This could cause issues since we're doing resizing of widgets and the views might go in front
				// or behind while we're doing the resize request.
				selector.RequestResize ();
			}));

			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidEndLiveResizeNotification, notif => DispatchService.GuiDispatch (() => {
				if (!IsCorrectNotification (selector, notif.Object))
					return;

				if (resizeTimer != null)
					resizeTimer.Dispose ();

				resizeTimer = Application.TimeoutInvoke (300, selector.RequestResize);
			}));

			var pathSelector = (SelectorView.PathSelectorView)selector.Subviews [0];
			pathSelector.ConfigurationChanged += (sender, e) => {
				if (ConfigurationChanged != null)
					ConfigurationChanged (sender, e);
			};
			pathSelector.RuntimeChanged += (sender, ea) => {
				if (RuntimeChanged != null)
					RuntimeChanged (sender, ea);
			};
			return item;
		}

		NSToolbarItem CreateButtonBarToolbarItem ()
		{
			var bar = new ButtonBar (barItems);
			buttonBarCache.Add (bar);

			// Note: We're leaving a 1 dead pixel size here because Apple bug
			// on Yosemite. Segmented controls have 3px padding on left and right
			// on Mavericks.
			nfloat size = 6 + 33 * bar.SegmentCount;

			// By default, Cocoa doesn't want to duplicate items in the toolbar.
			// Use different Ids to prevent this and not have to subclass.
			var item = new NSToolbarItem (ButtonBarId + buttonBarCount) {
				View = bar,
				MinSize = new CGSize (size, bar.FittingSize.Height),
				MaxSize = new CGSize (size, bar.FittingSize.Height),
			};
			bar.ResizeRequested += (o, e) => {
				nfloat resize = 6 + 33 * bar.SegmentCount;
				item.MinSize = new CGSize (resize, bar.FittingSize.Height);
				item.MaxSize = new CGSize (resize, bar.FittingSize.Height);
				selector.RequestResize ();
				centeringSpace.UpdateWidth ();
			};
			return item;
		}

		void AttachToolbarEvents (SearchBar bar)
		{
			if (bar.EventsAttached)
				return;

			bar.Changed += (o, e) => {
				if (SearchEntryChanged != null)
					SearchEntryChanged (o, e);
			};
			bar.KeyPressed += (o, e) => {
				if (SearchEntryKeyPressed != null)
					SearchEntryKeyPressed (o, e);
			};
			bar.LostFocus += (o, e) => {
				if (SearchEntryLostFocus != null)
					SearchEntryLostFocus (o, e);
			};
			bar.Activated += (o, e) => {
				if (SearchEntryActivated != null)
					SearchEntryActivated (o, e);
			};
			bar.EventsAttached = true;
		}

		NSToolbarItem CreateSearchBarToolbarItem ()
		{
			var bar = new SearchBar ();

			// Remove the focus from the Gtk system when Cocoa has focus
			// Fixes BXC #29601
			bar.GainedFocus += (o, e) => IdeApp.Workbench.RootWindow.Focus = null;

			viewCache.Add (bar);
			var item = new NSToolbarItem (SearchBarId) {
				View = bar,
				MinSize = new CGSize (150, bar.FittingSize.Height),
				MaxSize = new CGSize (270, bar.FittingSize.Height),
			};
			AttachToolbarEvents (bar);
			return item;
		}

		NSToolbarItem CreateStatusBarToolbarItem ()
		{
			var bar = new StatusBar ();
			viewCache.Add (bar);
			var item = new NSToolbarItem (StatusBarId) {
				View = bar,
				// Place some temporary values in there.
				MinSize = new CGSize (360, 22),
				MaxSize = new CGSize (360, 22),
			};

			Action<NSNotification> resizeAction = notif => DispatchService.GuiDispatch (() => {
				// Skip updates with a null Window. Only crashes on Mavericks.
				// The View gets updated once again when the window resize finishes.
				if (bar.Window == null)
					return;

				// We're getting notified about all windows in the application (for example, NSPopovers) that change size when really we only care about
				// the window the bar is in.
				if (notif.Object != bar.Window)
					return;

				double maxSize = Math.Round (bar.Window.Frame.Width * 0.30f);
				double minSize = Math.Round (bar.Window.Frame.Width * 0.25f);
				item.MinSize = new CGSize ((nfloat)Math.Max (220, minSize), 22);
				item.MaxSize = new CGSize ((nfloat)Math.Min (700, maxSize), 22);
				bar.RepositionStatusLayers ();
			});

			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResizeNotification, resizeAction);
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidEndLiveResizeNotification, resizeAction);
			return item;
		}

		NSToolbarItem CreateCenteringSpaceItem ()
		{
			var item = new CenteringSpaceToolbarItem (CenteringSpaceId);
			viewCache.Add (item.View);
			return item;
		}

		public MainToolbar (Gtk.Window window)
		{
			gtkWindow = window;
			widget = new NSToolbar (MainToolbarId) {
				DisplayMode = NSToolbarDisplayMode.Icon,
			};
			widget.WillInsertItem = (tool, id, send) => {
				switch (id) {
				case RunButtonId:
					return CreateRunToolbarItem ();
				case ButtonBarId:
					return CreateButtonBarToolbarItem ();
				case SearchBarId:
					return CreateSearchBarToolbarItem ();
				case SelectorId:
					return CreateSelectorToolbarItem ();
				case StatusBarId:
					return CreateStatusBarToolbarItem ();
				case CenteringSpaceId:
					return CreateCenteringSpaceItem ();
				}
				throw new NotImplementedException ();
			};
		}

		internal void Initialize ()
		{
			int total = -1;
			widget.InsertItem (RunButtonId, runButtonIdx = ++total);
			widget.InsertItem (SelectorId, selectorIdx = ++total);
			widget.InsertItem (CenteringSpaceId, buttonBarStartIdx = ++total);
			widget.InsertItem (StatusBarId, statusBarIdx = ++total);
			widget.InsertItem (NSToolbar.NSToolbarFlexibleSpaceItemIdentifier, ++total);
			widget.InsertItem (SearchBarId, searchEntryIdx = ++total);

			// NSButton -> NSToolbarItemViewer -> _NSToolbarClipView -> NSToolbarView -> NSToolbarClippedItemsIndicator
			viewCache.Add (runButton.Superview.Superview.Superview);
		}

		#region IMainToolbarView implementation
		public event EventHandler RunButtonClicked;
		public event EventHandler SearchEntryChanged;
		public event EventHandler<Xwt.KeyEventArgs> SearchEntryKeyPressed;
		public event EventHandler SearchEntryLostFocus;

		#pragma warning disable 0067
		public event EventHandler SearchEntryActivated;
		public event EventHandler SearchEntryResized;
		#pragma warning restore 0067

		public void FocusSearchBar ()
		{
			searchEntry.Focus ();

			var entry = searchEntry;
			entry.SelectText (entry);
		}

		List<IButtonBarButton> barItems = new List<IButtonBarButton> ();
		public void RebuildToolbar (IEnumerable<IButtonBarButton> buttons)
		{
			buttonBarCache.Clear ();
			while (buttonBarCount > 0) {
				widget.RemoveItem (buttonBarStartIdx);
				--buttonBarCount;
			}

			foreach (var item in buttons) {
				if (item.IsSeparator) {
					widget.InsertItem (ButtonBarId, buttonBarStartIdx + buttonBarCount++);
					barItems.Clear ();
				} else {
					barItems.Add (item);
				}
			}
		}

		public bool RunButtonSensitivity {
			get { return runButton.Enabled; }
			set { runButton.Enabled = value; }
		}

		public OperationIcon RunButtonIcon {
			set { runButton.Icon = value; }
		}

		public bool ConfigurationPlatformSensitivity {
			get { return selectorView.Enabled; }
			set { selectorView.Enabled = value; }
		}

		public event EventHandler ConfigurationChanged;
		public event EventHandler<HandledEventArgs> RuntimeChanged;

		public bool PlatformSensitivity {
			set {
				var cell = (NSPathCell)selectorView.Cell;
				cell.PathComponentCells [SelectorView.RuntimeIdx].Enabled = value;
			}
		}

		public IConfigurationModel ActiveConfiguration {
			get { return selectorView.ActiveConfiguration; }
			set { selectorView.ActiveConfiguration = value; }
		}

		public IRuntimeModel ActiveRuntime {
			get { return selectorView.ActiveRuntime; }
			set { selectorView.ActiveRuntime = value; }
		}

		public IEnumerable<IConfigurationModel> ConfigurationModel {
			get { return selectorView.ConfigurationModel; }
			set { selectorView.ConfigurationModel = value; }
		}

		public IEnumerable<IRuntimeModel> RuntimeModel {
			get { return selectorView.RuntimeModel; }
			set { selectorView.RuntimeModel = value; }
		}

		public bool SearchSensivitity {
			set { searchEntry.Enabled = value; }
		}

		public bool ButtonBarSensitivity {
			set {
				for (int start = buttonBarStartIdx; start < buttonBarStartIdx + buttonBarCount; ++start)
					((ButtonBar)widget.Items [start].View).Enabled = value;
			}
		}

		public IEnumerable<ISearchMenuModel> SearchMenuItems {
			set {
				var menu = new NSMenu {
					AutoEnablesItems = false,
				};
				foreach (var item in value)
					menu.AddItem (new NSMenuItem (item.DisplayString, (o, e) => item.NotifyActivated ()));

				searchEntry.SearchMenuTemplate = menu;
			}
		}

		public string SearchCategory {
			set {
				var entry = searchEntry;
				entry.SelectText (entry);
				entry.StringValue = value;
				entry.CurrentEditor.SelectedRange = new Foundation.NSRange (value.Length, 0);
			}
		}

		public string SearchText {
			get {
				return searchEntry.StringValue;
			}
			set {
				searchEntry.StringValue = value;
			}
		}

		public Gtk.Widget PopupAnchor {
			get {
				var entry = searchEntry;
				var widget = entry.gtkWidget;
				var window = GtkMacInterop.GetGtkWindow (entry.Window);
				if (window != null) {
					widget.GdkWindow = window.GdkWindow;
					widget.Allocation = new Gdk.Rectangle ((int)entry.Superview.Frame.X, (int)entry.Superview.Frame.Y, (int)entry.Superview.Frame.Width, 0);
				} else {
					// Reset the Gtk Widget each time since we can't set the GdkWindow to null.
					widget.Dispose ();
					widget = entry.gtkWidget = GtkMacInterop.NSViewToGtkWidget (entry);

					var nsWindows = NSApplication.SharedApplication.Windows;
					var fullscreenToolbarNsWindow = nsWindows.FirstOrDefault (nswin =>
						nswin.IsVisible && nswin.Description.StartsWith ("<NSToolbarFullScreenWindow", StringComparison.Ordinal));
					var workbenchNsWindow = nsWindows.FirstOrDefault (nswin =>
						GtkMacInterop.GetGtkWindow (nswin) is MonoDevelop.Ide.Gui.DefaultWorkbench);

					widget.Allocation = new Gdk.Rectangle (0, (int)(fullscreenToolbarNsWindow.Frame.Bottom - workbenchNsWindow.Frame.Height),
						(int)fullscreenToolbarNsWindow.Frame.Width, 0);
				}
				return widget;
			}
		}

		public string SearchPlaceholderMessage {
			// Analysis disable once ValueParameterNotUsed
			set { }
		}

		public MonoDevelop.Ide.StatusBar StatusBar {
			get { return statusBar; }
		}
		#endregion
	}
}

