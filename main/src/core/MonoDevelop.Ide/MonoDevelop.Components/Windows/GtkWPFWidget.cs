//
// GtkWPFWidget.cs
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

#if WIN32
using System;
using System.Windows;
using System.Windows.Interop;
using Gtk;
using Gdk;
using System.Threading;
using System.Runtime.InteropServices;

namespace MonoDevelop.Components.Windows
{
	public class GtkWPFWidget : Widget
	{
		bool fromGtk;
		internal System.Windows.Window wpfWindow {
			get;
			private set;
		}

		public GtkWPFWidget (System.Windows.Controls.Control wpfControl)
		{
			wpfWindow = new System.Windows.Window {
				Content = wpfControl,
				AllowsTransparency = true,
				WindowStyle = WindowStyle.None,
				Background = System.Windows.Media.Brushes.Transparent,
			};
			wpfWindow.PreviewKeyDown += (sender, e) => {
				// TODO: Some commands check for toplevels, and this window is not a toplevel.
				var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
				e.Handled = Ide.IdeApp.CommandService.ProcessKeyEvent (GtkWin32Interop.ConvertKeyEvent (e.KeyboardDevice.Modifiers, key));
			};

			wpfWindow.Closed += (sender, e) => {
				if (fromGtk)
					return;

				Ide.IdeApp.Exit ();
			};
			wpfWindow.ShowInTaskbar = false;
			WidgetFlags |= WidgetFlags.NoWindow;
		}

		void RepositionWpfWindow ()
		{
			int x, y;

			var gtkWnd = Toplevel as Gtk.Window;
			int offset = 0;
			if (gtkWnd.Decorated)
				offset = System.Windows.Forms.SystemInformation.CaptionHeight;

			int root_x, root_y;
			gtkWnd.GetPosition (out root_x, out root_y);
			if (TranslateCoordinates (Toplevel, root_x, root_y + offset, out x, out y)) {
				wpfWindow.Left = x;
				wpfWindow.Top = y;
			} else {
				wpfWindow.Left = Allocation.Left;
				wpfWindow.Top = Allocation.Top;
			}
			wpfWindow.Width = Allocation.Width;
			wpfWindow.Height = Allocation.Height;
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();

			RepositionWpfWindow ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			RepositionWpfWindow ();
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();

			fromGtk = true;
			wpfWindow.Close ();
		}

		protected override void OnShown ()
		{
			base.OnShown ();

			wpfWindow.Show ();
			AttachWindow ();
		}

		void AttachWindow ()
		{
			IntPtr gtkWindowPtr = GtkWin32Interop.HWndGet (Ide.IdeApp.Workbench.RootWindow.GdkWindow);
			IntPtr wpfWindowPtr = new WindowInteropHelper (wpfWindow).Handle;
			GtkWin32Interop.SetWindowLongPtr (wpfWindowPtr, (int)GtkWin32Interop.GWLParameter.GWL_HWNDPARENT, gtkWindowPtr);
			Ide.IdeApp.Workbench.RootWindow.ConfigureEvent += OnWindowConfigured;
		}

		void OnWindowConfigured (object sender, ConfigureEventArgs args)
		{
			RepositionWpfWindow ();
		}

		protected override void OnHidden ()
		{
			base.OnHidden ();

			wpfWindow.Hide ();
			Ide.IdeApp.Workbench.RootWindow.ConfigureEvent -= OnWindowConfigured;
		}
	}
}
#endif
