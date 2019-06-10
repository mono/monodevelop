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
		internal System.Windows.Forms.Integration.ElementHost wpfWidgetHost {
			get;
			private set;
		}

		public GtkWPFWidget (System.Windows.Controls.Control wpfControl)
		{
			wpfWidgetHost = new System.Windows.Forms.Integration.ElementHost
			{
				BackColor = System.Drawing.Color.SeaGreen,
				Child = wpfControl,
			};

			wpfControl.PreviewKeyDown += (sender, e) => {
				// TODO: Some commands check for toplevels, and this window is not a toplevel.
				if (e.Key == System.Windows.Input.Key.Escape)
				{
					System.Windows.Input.Keyboard.ClearFocus();
					MonoDevelop.Ide.IdeApp.Workbench.Present();
					return;
				}

				var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
				e.Handled = Ide.IdeApp.CommandService.ProcessKeyEvent (GtkWin32Interop.ConvertKeyEvent (e.KeyboardDevice.Modifiers, key));
			};
			WidgetFlags |= WidgetFlags.NoWindow;
		}

        protected virtual void RepositionWpfWindow ()
        {
            int scale = (int)MonoDevelop.Components.GtkWorkarounds.GetScaleFactor(this);
            RepositionWpfWindow (scale, scale);
        }

		protected void RepositionWpfWindow (double hscale, double vscale)
		{
			int x, y;
			if (TranslateCoordinates (Toplevel, 0, 0, out x, out y)) {
				wpfWidgetHost.Left = (int)(x * hscale);
				wpfWidgetHost.Top = (int)(y * vscale);
			} else {
				wpfWidgetHost.Left = Allocation.Left;
				wpfWidgetHost.Top = Allocation.Top;
			}
			wpfWidgetHost.Width = (int)((Allocation.Width + 1) * hscale);
			wpfWidgetHost.Height = (int)((Allocation.Height + 1) * vscale);
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();

			// Initial size setting.
			RepositionWpfWindow ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			// Needed for window full screening.
			RepositionWpfWindow ();
		}

		void OnWindowConfigured(object sender, ConfigureEventArgs args)
		{
			RepositionWpfWindow();
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();

			wpfWidgetHost.Dispose();
		}

		protected override void OnShown ()
		{
			base.OnShown ();

			IntPtr gtkWindowPtr = GtkWin32Interop.HWndGet(Ide.IdeApp.Workbench.RootWindow.GdkWindow);
			IntPtr wpfWindowPtr = wpfWidgetHost.Handle;
			GtkWin32Interop.SetWindowLongPtr(wpfWindowPtr, (int)GtkWin32Interop.GWLParameter.GWL_HWNDPARENT, gtkWindowPtr);
			Ide.IdeApp.Workbench.RootWindow.ConfigureEvent += OnWindowConfigured;
		}

		protected override void OnHidden ()
		{
			base.OnHidden ();
			
			Ide.IdeApp.Workbench.RootWindow.ConfigureEvent -= OnWindowConfigured;
		}
	}
}
#endif
