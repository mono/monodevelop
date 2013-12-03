//
// CairoHelper.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

namespace MonoDevelop.Components
{
	public static class PangoCairoHelper
	{
		const string LIBPANGOCAIRO = "libpangocairo-1.0-0.dll";
		[DllImport(LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
		private static extern void pango_cairo_show_layout (IntPtr cr, IntPtr layout);

		public static void ShowLayout (Cairo.Context cr, Pango.Layout layout)
		{
			pango_cairo_show_layout (cr == null ? IntPtr.Zero : cr.Handle, layout == null ? IntPtr.Zero : layout.Handle);
		}

		[DllImport(LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr pango_cairo_create_layout (IntPtr cr);

		public static Pango.Layout CreateLayout (Cairo.Context cr)
		{
			IntPtr raw_ret = pango_cairo_create_layout (cr == null ? IntPtr.Zero : cr.Handle);
			return GLib.Object.GetObject (raw_ret) as Pango.Layout;
		}

		[DllImport(LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
		private static extern void pango_cairo_layout_path (IntPtr cr, IntPtr layout);

		public static void LayoutPath (Cairo.Context cr, Pango.Layout layout, bool iUnderstandThePerformanceImplications)
		{
			pango_cairo_layout_path (cr == null ? IntPtr.Zero : cr.Handle, layout == null ? IntPtr.Zero : layout.Handle);
		}

		[DllImport(LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
		private static extern void pango_cairo_context_set_resolution (IntPtr pango_context, double dpi);

		public static void ContextSetResolution (Pango.Context context, double dpi)
		{
			pango_cairo_context_set_resolution (context == null ? IntPtr.Zero : context.Handle, dpi);
		}

		[DllImport(LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr pango_layout_get_context (IntPtr layout);

		public static string GetColorString (Gdk.Color color)
		{
			return string.Format ("#{0:X02}{1:X02}{2:X02}", color.Red / 256, color.Green / 256, color.Blue / 256);
		}

		public static Pango.Context LayoutGetContext (Pango.Layout layout)
		{
			IntPtr handle = pango_layout_get_context (layout.Handle);
			return handle.Equals (IntPtr.Zero) ? null : GLib.Object.GetObject (handle) as Pango.Context;
		}
	}
}
