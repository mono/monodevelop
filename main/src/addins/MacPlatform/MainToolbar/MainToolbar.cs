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
using MonoDevelop.Components.MainToolbar;
using AppKit;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	class MainToolbar : IMainToolbarView
	{
		const string MainToolbarId = "XSMainToolbar";
		internal NSToolbar widget;
		internal Gtk.Window gtkWindow;

		public MainToolbar (Gtk.Window window)
		{
			gtkWindow = window;
			widget = new NSToolbar (MainToolbarId);
		}

		internal void Initialize ()
		{
		}

		#region IMainToolbarView implementation
		public event EventHandler RunButtonClicked;
		public event EventHandler SearchEntryChanged;
		public event EventHandler SearchEntryActivated;
		public event EventHandler<Xwt.KeyEventArgs> SearchEntryKeyPressed;
		public event EventHandler SearchEntryResized;
		public event EventHandler SearchEntryLostFocus;

		public void FocusSearchBar ()
		{
//			throw new NotImplementedException ();
		}

		public void RebuildToolbar (System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<string>> bars)
		{
//			throw new NotImplementedException ();
		}

		public bool RunButtonSensitivity {
			get {
				return true;
//				throw new NotImplementedException ();
			}
			set {
//				throw new NotImplementedException ();
			}
		}

		public OperationIcon RunButtonIcon {
			set {
//				throw new NotImplementedException ();
			}
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
			set {
//				throw new NotImplementedException ();
			}
		}

		public SearchMenuItem[] SearchMenuItems {
			set {
//				throw new NotImplementedException ();
			}
		}

		public string SearchCategory {
			set {
//				throw new NotImplementedException ();
			}
		}

		public string SearchText {
			get {
				return "";
//				throw new NotImplementedException ();
			}
			set {
//				throw new NotImplementedException ();
			}
		}

		public Gtk.Widget PopupAnchor {
			get {
				return null;
//				throw new NotImplementedException ();
			}
		}

		public string SearchPlaceholderMessage {
			set {
//				throw new NotImplementedException ();
			}
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

