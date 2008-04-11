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
using System.Runtime.InteropServices;

namespace MonoDevelop.Projects.Gui.Completion
{
	
	
	public class WindowTransparencyDecorator
	{
		Gtk.Window window;
		bool semiTransparent;
		bool snooperInstalled;
		uint snooperID;
		const double opacity = 0.3;
		Gtk.KeySnoopFunc snoopFunc;
		
		public WindowTransparencyDecorator (Gtk.Window window)
		{
			this.window = window;
			
			//FIXME: access this property directly when we use GTK# 2.12
			if (CanSetOpacity) {
				window.Shown += ShownHandler;
				window.Hidden += HiddenHandler;
				window.Destroyed += DestroyedHandler;
			}
		}
		
		void ShownHandler (object sender, EventArgs args)
		{
			if (!snooperInstalled){
				if (snoopFunc == null)
					snoopFunc = new Gtk.KeySnoopFunc (TransparencyKeySnooper);
				snooperID = Gtk.Key.SnooperInstall (snoopFunc);
			}
			snooperInstalled = true;
		}
		
		void HiddenHandler (object sender, EventArgs args)
		{
			if (snooperInstalled)
				Gtk.Key.SnooperRemove (snooperID);
			snooperInstalled = false;
			SemiTransparent = false;
		}
		
		void DestroyedHandler (object sender, EventArgs args)
		{
			//remove the snooper
			HiddenHandler (null,  null);
			
			//annul allreferences between this and the window
			window.Shown -= ShownHandler;
			window.Hidden -= HiddenHandler;
			window.Destroyed -= DestroyedHandler;
			window = null;
		}
		
		int TransparencyKeySnooper (Gtk.Widget widget, Gdk.EventKey evnt)
		{
			if (evnt != null && evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R)
				SemiTransparent = (evnt.Type == Gdk.EventType.KeyPress);
			
			return 0; //gboolean FALSE
		}
		
		bool SemiTransparent {
			set {
				if (semiTransparent != value) {
					semiTransparent = value;
					TrySetTransparency (window, semiTransparent? opacity : 1.0);
				}
			}
		}
		
		#region Static setter reflecting code -- property is only in GTK+ 2.12
		
		[DllImport("libgobject-2.0.so.0")]
		static extern IntPtr g_type_class_peek (IntPtr gtype);

		[DllImport("libgobject-2.0.so.0")]
		static extern IntPtr g_object_class_find_property (IntPtr klass, string name);

		[DllImport("libgobject-2.0.so.0")]
		static extern void g_object_set (IntPtr obj, string property, double value, IntPtr nullarg);
		
		static bool triedToFindSetters = false;
		
		//if we have GTK# 2.12 we can use Mono reflection
		static System.Reflection.PropertyInfo opacityProp = null;
		
		//if we have GTK+ 2.12 but an older GTK#, we can use GObject reflection
		static IntPtr opacityMeth = IntPtr.Zero;
		
		static bool CanSetOpacity {
			get {
				if (triedToFindSetters)
					return (opacityMeth != IntPtr.Zero || opacityProp != null);
				
				triedToFindSetters = true;
				
				//FIXME: remove this when the crasher has been fixed
				string envVar = Environment.GetEnvironmentVariable ("MONODEVELOP_ENABLE_UNSTABLE_TRANSPARENCY");
				if (string.IsNullOrEmpty (envVar) || envVar.ToLower () != "true")
					return false;
				
				opacityProp = typeof (Gtk.Window).GetProperty ("Opacity");
				if (opacityProp != null)
					return true;
				
				GLib.GType gtype = (GLib.GType) typeof (Gtk.Window);
				try {
					IntPtr klass = g_type_class_peek (gtype.Val);
					opacityMeth = g_object_class_find_property (klass, "opacity");
				} catch (DllNotFoundException) {}
				
				return opacityMeth != IntPtr.Zero;
			}
		}
		
		static void TrySetTransparency (Gtk.Window window, double opacity)
		{
			if (opacityMeth != IntPtr.Zero)
				g_object_set (window.Handle, "opacity", opacity, IntPtr.Zero);
			else if (opacityProp != null)
				opacityProp.SetValue (window, opacity, null);
		}
		
		#endregion
	}
}
