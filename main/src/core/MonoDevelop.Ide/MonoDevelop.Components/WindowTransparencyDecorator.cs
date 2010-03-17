// 
// WindowTransparencyDecorator.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Based on code derived from Banshee.Widgets.EllipsizeLabel
// by Aaron Bockover (aaron@aaronbock.net)
// 
// Copyright (C) 2005-2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MonoDevelop.Components
{
	public class WindowTransparencyDecorator
	{
		Gtk.Window window;
		bool semiTransparent;
		bool snooperInstalled;
		uint snooperID;
		const double opacity = 0.2;
		Delegate snoopFunc;
		
		WindowTransparencyDecorator (Gtk.Window window)
		{
			this.window = window;
			//HACK: Workaround for GTK# crasher bug where GC collects internal wrapper delegates
			snoopFunc = TryBindGtkInternals (this);
			
			if (snoopFunc != null) {
				window.Shown += ShownHandler;
				window.Hidden += HiddenHandler;
				window.Destroyed += DestroyedHandler;
			} else {
				snoopFunc = null;
				window = null;
			}
		}
		
		public static WindowTransparencyDecorator Attach (Gtk.Window window)
		{
			return new WindowTransparencyDecorator (window);
		}
		
		public void Detach ()
		{
			if (window == null)
			return;
			
			//remove the snooper
			HiddenHandler (null,  null);
			
			//annul allreferences between this and the window
			window.Shown -= ShownHandler;
			window.Hidden -= HiddenHandler;
			window.Destroyed -= DestroyedHandler;
			snoopFunc = null;
			window = null;
		}
		
		void ShownHandler (object sender, EventArgs args)
		{
			if (!snooperInstalled)
				snooperID = InstallSnooper (snoopFunc);
			snooperInstalled = true;
			
			//NOTE: we unset transparency when showing, instead of when hiding
			//because the latter case triggers a metacity+compositing bug that shows the window again
			SemiTransparent = false;
		}
		
		void HiddenHandler (object sender, EventArgs args)
		{
			if (snooperInstalled)
				RemoveSnooper (snooperID);
			snooperInstalled = false;
		}
		
		void DestroyedHandler (object sender, EventArgs args)
		{
			Detach ();
		}
		
		#pragma warning disable 0169
		
		int TransparencyKeySnooper (IntPtr widget, IntPtr rawEvnt, IntPtr data)
		{
			if (rawEvnt != IntPtr.Zero) {
				Gdk.EventKey evnt = new Gdk.EventKey (rawEvnt);
				if (evnt != null && evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R)
					SemiTransparent = (evnt.Type == Gdk.EventType.KeyPress);
			}
			return 0; //gboolean FALSE
		}
		
		#pragma warning restore 0169
		
		bool SemiTransparent {
			set {
				if (semiTransparent != value) {
					semiTransparent = value;
					window.Opacity = semiTransparent? opacity : 1.0;
				}
			}
		}
		
		#region Workaround for GTK# crasher bug where GC collects internal wrapper delegates
		
		static WindowTransparencyDecorator ()
		{
			snooper_install = typeof (Gtk.Key).GetMethod ("gtk_key_snooper_install", BindingFlags.NonPublic | BindingFlags.Static);
			snooper_remove = typeof (Gtk.Key).GetMethod ("gtk_key_snooper_remove", BindingFlags.NonPublic | BindingFlags.Static);
		}
		
		static MethodInfo snooper_install;
		static MethodInfo snooper_remove;
		
		delegate int GtkKeySnoopFunc (IntPtr widget, IntPtr rawEvnt, IntPtr func_data);
		
		static uint InstallSnooper (Delegate del)
		{
			return (uint) snooper_install.Invoke (null, new object[] { del, IntPtr.Zero} );
		}
		
		static void RemoveSnooper (uint id)
		{
			snooper_remove.Invoke (null, new object[] { id });
		}
		
		static bool internalBindingWorks = true;
		static bool internalBindingTried = false;
		
		static Delegate TryBindGtkInternals (WindowTransparencyDecorator instance)
		{
			if (internalBindingTried) {
				if (!internalBindingWorks)
					return null;
			} else {
				internalBindingTried = true;
			}
			
			try {
				Type delType = typeof(Gtk.Widget).Assembly.GetType ("GtkSharp.KeySnoopFuncNative");
				System.Reflection.MethodInfo met = typeof (WindowTransparencyDecorator).GetMethod ("TransparencyKeySnooper", 
				    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
				Delegate ret = Delegate.CreateDelegate (delType, instance, met);
				if (ret != null)
					return ret;
			} catch {}
			
			internalBindingWorks = false;
			MonoDevelop.Core.LoggingService.LogWarning ("GTK# API has changed, and control-transparency will not be available for popups");
			return null;
		}
		
		#endregion
	}
}
