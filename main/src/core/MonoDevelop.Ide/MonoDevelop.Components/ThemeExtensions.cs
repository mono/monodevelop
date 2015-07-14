//
// ThemeExtensions.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
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
using MonoDevelop.Ide;
using System.Collections.Generic;

#if MAC
using AppKit;
#endif

namespace MonoDevelop.Components
{
	public static class ThemeExtensions
	{
		static ThemeExtensions ()
		{
			IdeApp.Preferences.UserInterfaceSkinChanged += Preferences_UserInterfaceSkinChanged;
		}
#if MAC
		static HashSet<NSWindow> nsWindows = new HashSet<NSWindow> ();

		public static void ApplyTheme (this NSWindow window)
		{
			if (nsWindows.Add (window)) {
				window.WillClose += Window_WillClose;
				SetTheme (window);
			}
		}

		static void SetTheme (NSWindow window)
		{
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light)
				window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameAqua);
			else
				window.Appearance = NSAppearance.GetAppearance (NSAppearance.NameVibrantDark);
		}

		static void Window_WillClose (object sender, EventArgs e)
		{
			var n = (Foundation.NSNotification) sender;
			var w = (NSWindow)n.Object;
			w.WillClose -= Window_WillClose;
			nsWindows.Remove (w);
		}

		static void UpdateMacWindows ()
		{
			foreach (var w in nsWindows)
				SetTheme (w);
		}

		static void OnGtkWindowRealized (object s, EventArgs a)
		{
			var nsw = MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow ((Gtk.Window) s);
			if (nsw != null)
				nsw.ApplyTheme ();
		}
#endif

		static void Preferences_UserInterfaceSkinChanged (object sender, Core.PropertyChangedEventArgs e)
		{
			#if MAC
			UpdateMacWindows ();
			#endif
		}

		public static void ApplyTheme (this Gtk.Window window)
		{
			#if MAC
			window.Realized += OnGtkWindowRealized;
			if (window.IsRealized) {
				var nsw = MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow (window);
				if (nsw != null)
					nsw.ApplyTheme ();
			}
			#endif
		}
	}
}

