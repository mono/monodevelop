// HelperMethods.cs
// 
// Cut & paste from PangoCairoHelper.
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Collections.Generic;

namespace MonoDevelop.Components
{
	public static class HelperMethods
	{
		[DllImport(PangoUtil.LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_cairo_show_layout (IntPtr cr, IntPtr layout);

		public static void ShowLayout (this Cairo.Context cr, Pango.Layout layout)
		{
			pango_cairo_show_layout (cr == null ? IntPtr.Zero : cr.Handle, layout == null ? IntPtr.Zero : layout.Handle);
		}

		[DllImport(PangoUtil.LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
		static extern IntPtr pango_cairo_create_layout (IntPtr cr);

		public static Pango.Layout CreateLayout (this Cairo.Context cr)
		{
			IntPtr raw_ret = pango_cairo_create_layout (cr == null ? IntPtr.Zero : cr.Handle);
			return GLib.Object.GetObject (raw_ret, true) as Pango.Layout;
		}

		[DllImport(PangoUtil.LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_cairo_layout_path (IntPtr cr, IntPtr layout);

		public static void LayoutPath (this Cairo.Context cr, Pango.Layout layout)
		{
			pango_cairo_layout_path (cr == null ? IntPtr.Zero : cr.Handle, layout == null ? IntPtr.Zero : layout.Handle);
		}

		[DllImport(PangoUtil.LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
		static extern void pango_cairo_context_set_resolution (IntPtr pango_context, double dpi);

		public static void ContextSetResolution (this Pango.Context context, double dpi)
		{
			pango_cairo_context_set_resolution (context == null ? IntPtr.Zero : context.Handle, dpi);
		}

		[DllImport(PangoUtil.LIBPANGOCAIRO, CallingConvention=CallingConvention.Cdecl)]
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

		public static double GetPixelBaseline (this Pango.Layout layout)
		{
			return layout.Iter.Baseline / Pango.Scale.PangoScale;
		}
		
		public static void DrawLine (this Cairo.Context cr, Cairo.Color color, double x1, double y1, double x2, double y2)
		{
			cr.SetSourceColor (color);
			cr.MoveTo (x1, y1);
			cr.LineTo (x2, y2);
			cr.Stroke ();
		}
		
		public static void Line (this Cairo.Context cr, double x1, double y1, double x2, double y2)
		{
			cr.MoveTo (x1, y1);
			cr.LineTo (x2, y2);
		}
		
		public static void SharpLineX (this Cairo.Context cr, double x1, double y1, double x2, double y2)
		{
			cr.MoveTo (x1 + 0.5, y1);
			cr.LineTo (x2 + 0.5, y2);
		}
		
		public static void SharpLineY (this Cairo.Context cr, double x1, double y1, double x2, double y2)
		{
			cr.MoveTo (x1, y1 + 0.5);
			cr.LineTo (x2, y2 + 0.5);
		}

		public static void SetSourceColor (this Cairo.Context cr, Cairo.Color color)
		{
			cr.SetSourceRGBA (color.R, color.G, color.B, color.A);
		}

		public static bool Contains (this Cairo.Rectangle rect, Gdk.Point point)
		{
			return ((point.X >= rect.X) && (point.X < (rect.X + rect.Width)) && 
				(point.Y >= rect.Y) && (point.Y < (rect.Y + rect.Height)));
		}

		public static bool Contains (this Cairo.Rectangle rect, double x, double y)
		{
			return ((x >= rect.X) && (x < (rect.X + rect.Width)) && 
				(y >= rect.Y) && (y < (rect.Y + rect.Height)));
		}

		public static Gdk.Rectangle InflateRect (this Gdk.Rectangle rect, int width, int height)
		{
			return new Gdk.Rectangle (
				rect.X - width,
				rect.Y - height,
				rect.Width + (width * 2),
				rect.Height + (height * 2)
			);
		}

		//this is needed for building against old Mono.Cairo versions
		[Obsolete]
		public static void SetSource (this Cairo.Context cr, Cairo.Pattern pattern)
		{
			cr.Pattern = pattern;
		}

		[Obsolete]
		public static Cairo.Surface GetTarget (this Cairo.Context cr)
		{
			return cr.Target;
		}

		[Obsolete]
		public static void Dispose (this Cairo.Context cr)
		{
			((IDisposable)cr).Dispose ();
		}

		public static double Clamp (double min, double max, double value)
		{
			return Math.Max (min, Math.Min (max, value));
		}

		public static int Clamp (int val, int min, int max)
		{
			return Math.Max (min, Math.Min (max, val));
		}
	}
}
