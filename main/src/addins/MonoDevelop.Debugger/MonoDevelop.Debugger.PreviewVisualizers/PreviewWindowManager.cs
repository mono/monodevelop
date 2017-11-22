//
// PreviewWindowManager.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Ide;
using Mono.Debugging.Client;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Debugger
{
	public class PreviewWindowManager
	{
		static PreviewVisualizerWindow wnd;

		public static bool IsVisible {
			get {
				return wnd != null && wnd.Visible;
			}
		}

		public static void Show (ObjectValue val, Control widget, Gdk.Rectangle previewButtonArea)
		{
			DestroyWindow ();
			wnd = new PreviewVisualizerWindow (val, widget);
			IdeApp.CommandService.RegisterTopWindow (wnd);
			wnd.ShowPopup (widget, previewButtonArea, PopupPosition.Left);
			wnd.Destroyed += HandleDestroyed;
			OnWindowShown (EventArgs.Empty);
		}

		static void HandleDestroyed (object sender, EventArgs e)
		{
			wnd = null;
			OnWindowClosed (EventArgs.Empty);
		}

		public static void RepositionWindow (Gdk.Rectangle? newCaret = null)
		{
			if (!IsVisible)
				return;
			wnd.RepositionWindow (newCaret);
		}

		static PreviewWindowManager ()
		{
			if (IdeApp.Workbench != null) {
				IdeApp.Workbench.RootWindow.Destroyed += (sender, e) => DestroyWindow ();
			}
			IdeApp.CommandService.KeyPressed += HandleKeyPressed;
			DebuggingService.StoppedEvent += delegate {
				DestroyWindow ();
			};
		}

		static void HandleKeyPressed (object sender, KeyPressArgs e)
		{
			if (e.Key == Gdk.Key.Escape) {
				DestroyWindow ();
			}
		}

		public static void DestroyWindow ()
		{
			if (wnd != null) {
				wnd.Destroy ();
				wnd = null;
			}
		}

		static void OnWindowClosed (EventArgs e)
		{
			var handler = WindowClosed;
			if (handler != null)
				handler (null, e);
		}

		public static event EventHandler WindowClosed;

		static void OnWindowShown (EventArgs e)
		{
			var handler = WindowShown;
			if (handler != null)
				handler (null, e);
		}

		public static event EventHandler WindowShown;
	}
}

