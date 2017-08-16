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
using MonoDevelop.MacIntegration;
using Xwt;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	class MainToolbar : IMainToolbarView
	{
		const string MainToolbarId = "XSMainToolbar";
		const string AwesomeBarId = "AwesomeBarToolbarItem";

		internal NSToolbar widget;
		internal Gtk.Window gtkWindow;

		public static bool IsFullscreen { get; private set; }
		AwesomeBar awesomeBar;

		RunButton runButton {
			get { return awesomeBar.RunButton; }
		}

		StatusBar statusBar {
			get { return awesomeBar.StatusBar; }
		}

		SelectorView selector {
			get { return awesomeBar.SelectorView; }
		}

		SelectorView.PathSelectorView selectorView {
			get { return awesomeBar.SelectorView.RealSelectorView; }
		}

		SearchBar searchEntry {
			get { return awesomeBar.SearchBar; }
		}

		void AttachToolbarEvents (SearchBar bar)
		{
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
			bar.SelectionActivated += (o, e) => {
				if (SearchEntryActivated != null)
					SearchEntryActivated (o, e);
			};
		}

		public void Focus()
		{
			awesomeBar.Window.MakeFirstResponder (awesomeBar.RunButton);
		}

		public MainToolbar (Gtk.Window window)
		{
			gtkWindow = window;
			widget = new NSToolbar (MainToolbarId) {
				DisplayMode = NSToolbarDisplayMode.Icon,
			};

			awesomeBar = new AwesomeBar ();
			awesomeBar.RunButton.Activated += (o, e) => {
				if (RunButtonClicked != null)
					RunButtonClicked (o, e);
			};


			// Remove the focus from the Gtk system when Cocoa has focus
			// Fixes BXC #29601
			awesomeBar.SearchBar.GainedFocus += (o, e) => IdeApp.Workbench.RootWindow.Focus = null;

			AttachToolbarEvents (awesomeBar.SearchBar);

			selectorView.ConfigurationChanged += (sender, e) => {
				if (ConfigurationChanged != null)
					ConfigurationChanged (sender, e);
			};

			selectorView.RunConfigurationChanged += (sender, e) => {
				if (RunConfigurationChanged != null)
					RunConfigurationChanged (sender, e);
			};

			selectorView.RuntimeChanged += (sender, ea) => {
				if (RuntimeChanged != null)
					RuntimeChanged (sender, ea);
			};

			widget.WillInsertItem = (tool, id, send) => {
				switch (id) {
				case AwesomeBarId:
					return new NSToolbarItem (AwesomeBarId) {
						View = awesomeBar,
						MinSize = new CGSize (1024, AwesomeBar.ToolbarWidgetHeight),
						MaxSize = new CGSize (float.PositiveInfinity, AwesomeBar.ToolbarWidgetHeight)
					};

				default:
					throw new NotImplementedException ();
				}
			};

			Action<NSNotification> resizeAction = notif => Runtime.RunInMainThread (() => {
				var win = awesomeBar.Window;
				if (win == null) {
					return;
				}

				var item = widget.Items[0];

				var abFrameInWindow = awesomeBar.ConvertRectToView (awesomeBar.Frame, null);
				var awesomebarHeight = AwesomeBar.ToolbarWidgetHeight;
				var size = new CGSize (win.Frame.Width - abFrameInWindow.X - 4, awesomebarHeight);

				if (item.MinSize != size) {
					item.MinSize = size;
				}
			});

			// We can't use the events that Xamarin.Mac adds for delegate methods as they will overwrite
			// the delegate that Gtk has added
			NSWindow nswin = GtkMacInterop.GetNSWindow (window);
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResizeNotification, resizeAction, nswin);
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidEndLiveResizeNotification, resizeAction, nswin);

			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.WillEnterFullScreenNotification, (note) => IsFullscreen = true, nswin);
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.WillExitFullScreenNotification, (note) => IsFullscreen = false, nswin);
		}

		internal void Initialize ()
		{
			widget.InsertItem (AwesomeBarId, 0);
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
			if (!string.IsNullOrEmpty (entry.StringValue)) {
				entry.SelectText (entry);
			}
		}

		public void RebuildToolbar (IEnumerable<ButtonBarGroup> groups)
		{
			var buttonBars = new List<ButtonBar> ();
			foreach (var g in groups) {
				var bar = new ButtonBar (g.Buttons) {
					Title = g.Title
				};
				buttonBars.Add (bar);
			}

			awesomeBar.ButtonBarContainer.ButtonBars = buttonBars;
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

		public bool RunConfigurationVisible {
			get { return selectorView.RunConfigurationVisible; }
			set { selectorView.RunConfigurationVisible = value; }
		}

		public event EventHandler ConfigurationChanged;
		public event EventHandler RunConfigurationChanged;
		public event EventHandler<HandledEventArgs> RuntimeChanged;

		public bool PlatformSensitivity {
			set {
				selectorView.PlatformSensitivity = value;
			}
		}

		public IConfigurationModel ActiveConfiguration {
			get { return selectorView.ActiveConfiguration; }
			set { selectorView.ActiveConfiguration = value; }
		}

		public IRunConfigurationModel ActiveRunConfiguration {
			get { return selectorView.ActiveRunConfiguration; }
			set { selectorView.ActiveRunConfiguration = value; }
		}

		public IRuntimeModel ActiveRuntime {
			get { return selectorView.ActiveRuntime; }
			set { selectorView.ActiveRuntime = value; }
		}

		public IEnumerable<IConfigurationModel> ConfigurationModel {
			get { return selectorView.ConfigurationModel; }
			set { selectorView.ConfigurationModel = value; }
		}

		public IEnumerable<IRunConfigurationModel> RunConfigurationModel {
			get { return selectorView.RunConfigurationModel; }
			set { selectorView.RunConfigurationModel = value; }
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
				foreach (var bar in awesomeBar.ButtonBarContainer.ButtonBars) {
					bar.Enabled = value;
				}
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

				// window will be null if the app is fullscreen.
				if (window != null) {
					widget.GdkWindow = window.GdkWindow;

					// We need to adjust the position of the frame so the popup will line up correctly
					var abFrameInWindow = awesomeBar.ConvertRectToView (awesomeBar.Frame, null);
					widget.Allocation = new Gdk.Rectangle ((int)(entry.Frame.X + abFrameInWindow.X - 8), (int)entry.Frame.Y, (int)entry.Frame.Width, 0);
				} else {
					// Reset the Gtk Widget each time since we can't set the GdkWindow to null.
					widget.Dispose ();
					widget = entry.gtkWidget = GtkMacInterop.NSViewToGtkWidget (entry);

					var nsWindows = NSApplication.SharedApplication.Windows;
					var fullscreenToolbarNsWindow = nsWindows.FirstOrDefault (nswin =>
						nswin.IsVisible && nswin.Description.StartsWith ("<NSToolbarFullScreenWindow", StringComparison.Ordinal));

					CGPoint gdkOrigin = ScreenMonitor.GdkPointForNSScreen (searchEntry.Window.Screen);

					widget.Allocation = new Gdk.Rectangle (0, (int)(gdkOrigin.Y + fullscreenToolbarNsWindow.Frame.Height - 20),
						(int)(gdkOrigin.X + fullscreenToolbarNsWindow.Frame.Width - 16), 0);
				}
				return widget;
			}
		}

		public string SearchPlaceholderMessage {
			// Analysis disable once ValueParameterNotUsed
			set {
				searchEntry.PlaceholderText = value;
			}
		}

		public MonoDevelop.Ide.StatusBar StatusBar {
			get { return statusBar; }
		}
		#endregion
	}
}

