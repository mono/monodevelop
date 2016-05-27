﻿//
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
using MonoDevelop.MacIntegration.OverlaySearch;
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

			selectorView.ConfigurationChanged += (sender, e) => {
				if (ConfigurationChanged != null)
					ConfigurationChanged (sender, e);
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
						MaxSize = new CGSize (1024, AwesomeBar.ToolbarWidgetHeight)
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

				item.MinSize = size;
				item.MaxSize = size;
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
#pragma warning disable 0067
		public event EventHandler<Xwt.KeyEventArgs> SearchEntryKeyPressed;
		public event EventHandler SearchEntryLostFocus;

		public event EventHandler SearchEntryActivated;
		public event EventHandler SearchEntryResized;
#pragma warning restore 0067

		Searchlight searchWindow;
		public void FocusSearchBar ()
		{
			searchWindow = new Searchlight ();
			searchWindow.WindowWasDismissed += EndModal;
			searchWindow.SearchRequested += DoSearch;

			NSWindow nswin = GtkMacInterop.GetNSWindow (gtkWindow);
			CGRect parentRect = nswin.Frame;
			searchWindow.SetFrame (new CGRect ((parentRect.X + parentRect.Width) / 2 - 325.0f, (parentRect.Y + parentRect.Height) / 4 * 3, 650.0f, 40.0f), true);

			searchWindow.MakeKeyAndOrderFront (null);
			nswin.AddChildWindow (searchWindow, NSWindowOrderingMode.Above);
		}

		void DoSearch (object sender, EventArgs args)
		{
			SearchEntryChanged?.Invoke (sender, args);
		}

		void EndModal (object sender, EventArgs args)
		{
			NSWindow nswin = GtkMacInterop.GetNSWindow (gtkWindow);
			nswin.RemoveChildWindow (searchWindow);
			searchWindow.WindowWasDismissed -= EndModal;
			searchWindow.SearchRequested -= DoSearch;

			searchWindow.OrderOut (null);
			searchWindow = null;
		}

		public void RebuildToolbar (IEnumerable<IButtonBarButton> buttons)
		{
			List<IButtonBarButton> barItems = new List<IButtonBarButton> ();
			List<ButtonBar> buttonBars = new List<ButtonBar> ();

			foreach (var item in buttons) {
				if (item.IsSeparator) {
					var bar = new ButtonBar (barItems);
					buttonBars.Add (bar);

					barItems.Clear ();
				} else {
					barItems.Add (item);
				}
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
			set { }
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
			}
		}

		public string SearchCategory {
			set {
			}
		}

		public string SearchText {
			get {
				return searchWindow == null ? "" : searchWindow.SearchString;
			}
			set {
			}
		}

		public ISearchResultsDisplay CreateSearchResultsDisplay ()
		{
			return searchWindow.ResultsDisplay;
		}

		public Gtk.Widget PopupAnchor {
			get {
				return null;
			}
		}

		public string SearchPlaceholderMessage {
			set {
			}
		}

		public Ide.StatusBar StatusBar {
			get { return statusBar; }
		}
		#endregion
	}
}

