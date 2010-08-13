// HelperMethods.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mono.TextEditor
{
	public static class HelperMethods
	{
		public static T Kill<T>(this T gc) where T : IDisposable
		{
			if (gc != null) 
				gc.Dispose ();
			return default(T);
		}
		
		public static T Kill<T>(this T gc, Action<T> action) where T : IDisposable
		{
			if (gc != null) {
				action (gc);
				gc.Dispose ();
			}
			
			return default(T);
		}
		
		
		[DllImport("libpangocairo-1.0-0.dll", CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_cairo_show_layout (IntPtr cr, IntPtr layout);

		public static void ShowLayout (this Cairo.Context cr, Pango.Layout layout)
		{
			pango_cairo_show_layout (cr == null ? IntPtr.Zero : cr.Handle, layout == null ? IntPtr.Zero : layout.Handle);
		}

		[DllImport("libpangocairo-1.0-0.dll", CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_cairo_create_layout (IntPtr cr);

		public static Pango.Layout CreateLayout (this Cairo.Context cr)
		{
			IntPtr raw_ret = pango_cairo_create_layout (cr == null ? IntPtr.Zero : cr.Handle);
			return GLib.Object.GetObject (raw_ret) as Pango.Layout;
		}

		[DllImport("libpangocairo-1.0-0.dll", CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_cairo_layout_path (IntPtr cr, IntPtr layout);

		public static void LayoutPath (this Cairo.Context cr, Pango.Layout layout)
		{
			pango_cairo_layout_path (cr == null ? IntPtr.Zero : cr.Handle, layout == null ? IntPtr.Zero : layout.Handle);
		}

		[DllImport("libpangocairo-1.0-0.dll", CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_cairo_context_set_resolution (IntPtr pango_context, double dpi);

		public static void ContextSetResolution (this Pango.Context context, double dpi)
		{
			pango_cairo_context_set_resolution (context == null ? IntPtr.Zero : context.Handle, dpi);
		}

		[DllImport("libpangocairo-1.0-0.dll", CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_layout_get_context (IntPtr layout);

		public static string GetColorString (Gdk.Color color)
		{
			return string.Format ("#{0:X02}{1:X02}{2:X02}", color.Red / 256, color.Green / 256, color.Blue / 256);
		}

		public static Pango.Context LayoutGetContext (this Pango.Layout layout)
		{
			IntPtr handle = pango_layout_get_context (layout.Handle);
			return handle.Equals (IntPtr.Zero) ? null : GLib.Object.GetObject (handle) as Pango.Context;
		}
		
		public static void DrawLine (this Cairo.Context cr, Cairo.Color color, double x1, double y1, double x2, double y2)
		{
			cr.Color = color;
			cr.MoveTo (x1, y1);
			cr.LineTo (x2, y2);
			cr.Stroke ();
		}
		
		public static void Line (this Cairo.Context cr, double x1, double y1, double x2, double y2)
		{
			cr.MoveTo (x1, y1);
			cr.LineTo (x2, y2);
		}
	}
}
