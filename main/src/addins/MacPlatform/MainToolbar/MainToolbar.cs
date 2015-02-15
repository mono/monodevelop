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
using System.Linq;
using MonoDevelop.Components.Mac;
using MonoDevelop.Components.MainToolbar;
using AppKit;
using CoreGraphics;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	class MainToolbar : IMainToolbarView
	{
		const string MainToolbarId = "XSMainToolbar";
		const string RunButtonId = "RunToolbarItem";
		const string SearchBarId = "SearchBarToolbarItem";

		internal NSToolbar widget;
		internal Gtk.Window gtkWindow;

		int runButtonIdx;
		RunButton runButton {
			get { return (RunButton)widget.Items[runButtonIdx].View; }
		}

		int searchEntryIdx;
		SearchBar searchEntry {
			get { return (SearchBar)widget.Items[searchEntryIdx].View; }
		}

		NSToolbarItem CreateRunToolbarItem ()
		{
			var button = new RunButton ();
			button.Activated += (o, e) => {
				if (RunButtonClicked != null)
					RunButtonClicked (o, e);
			};

			var item = new NSToolbarItem (RunButtonId) {
				View = button,
				MinSize = new CGSize (button.FittingSize.Width + 12, button.FittingSize.Height),
			};
			return item;
		}

		NSToolbarItem CreateSearchBarToolbarItem ()
		{
			var bar = new SearchBar ();
			var item = new NSToolbarItem (SearchBarId) {
				View = bar,
				MinSize = new CGSize (180, bar.FittingSize.Height),
			};

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
				case SearchBarId:
					return CreateSearchBarToolbarItem ();
				}
				throw new NotImplementedException ();
			};
		}

		internal void Initialize ()
		{
			int total = -1;
			widget.InsertItem (RunButtonId, runButtonIdx = ++total);
			widget.InsertItem (NSToolbar.NSToolbarFlexibleSpaceItemIdentifier, ++total);
			widget.InsertItem (SearchBarId, searchEntryIdx = ++total);
		}

		#region IMainToolbarView implementation
		public event EventHandler RunButtonClicked;
		public event EventHandler SearchEntryChanged;
		public event EventHandler<Xwt.KeyEventArgs> SearchEntryKeyPressed;
		public event EventHandler SearchEntryLostFocus;

		#pragma warning disable 0169
		public event EventHandler SearchEntryActivated;
		public event EventHandler SearchEntryResized;
		#pragma warning restore 0169

		public void FocusSearchBar ()
		{
			var entry = searchEntry;
			entry.Window.MakeFirstResponder (entry);
		}

		public void RebuildToolbar (System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<string>> bars)
		{
//			throw new NotImplementedException ();
		}

		public bool RunButtonSensitivity {
			get { return runButton.Enabled; }
			set { runButton.Enabled = value; }
		}

		public OperationIcon RunButtonIcon {
			set { runButton.Icon = value; }
		}

		public bool ConfigurationPlatformSensitivity {
			get {
				return true;
//				throw new NotImplementedException ();
			}
			set {
//				throw new NotImplementedException ();
			}
		}

		public bool SearchSensivitity {
			set { searchEntry.Enabled = value; }
		}

		public SearchMenuItem[] SearchMenuItems {
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
				entry.StringValue = value;
				entry.CurrentEditor.SelectedRange = new Foundation.NSRange (entry.StringValue.Length, entry.StringValue.Length);
			}
		}

		public string SearchText {
			get { return searchEntry.StringValue; }
			set { searchEntry.StringValue = value; }
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
			get {
				return null;
//				throw new NotImplementedException ();
			}
		}
		#endregion
	}
}

