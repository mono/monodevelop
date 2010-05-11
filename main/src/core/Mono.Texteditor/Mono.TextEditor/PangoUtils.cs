// 
// PangoUtils.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using Gtk;
using System.Runtime.InteropServices;

namespace Mono.TextEditor
{
	public static class PangoUtil
	{
		internal const string LIBGTK          = "libgtk-win32-2.0-0.dll";
		internal const string LIBATK          = "libatk-1.0-0.dll";
		internal const string LIBGLIB         = "libglib-2.0-0.dll";
		internal const string LIBGDK          = "libgdk-win32-2.0-0.dll";
		internal const string LIBGOBJECT      = "libgobject-2.0-0.dll";
		internal const string LIBPANGO        = "libpango-1.0-0.dll";
		internal const string LIBPANGOCAIRO   = "libpangocairo-1.0-0.dll";
		
		/// <summary>
		/// This doesn't leak Pango layouts, unlike all other ways to create them in GTK# <= 2.12.10
		/// </summary>
		public static Pango.Layout CreateLayout (Widget widget)
		{
			var ptr = gtk_widget_create_pango_layout (widget.Handle, IntPtr.Zero);
			return ptr == IntPtr.Zero? null : new Pango.Layout (ptr);
		}

		public static Pango.Layout CreateLayout (Widget widget, string text)
		{
			IntPtr textPtr = text == null? IntPtr.Zero : GLib.Marshaller.StringToPtrGStrdup (text);
			
			var ptr = gtk_widget_create_pango_layout (widget.Handle, textPtr);
			
			if (textPtr != IntPtr.Zero)
				GLib.Marshaller.Free (textPtr);
			
			return ptr == IntPtr.Zero? null : new Pango.Layout (ptr);
		}
		
		[DllImport (LIBGTK)]
		static extern IntPtr gtk_widget_create_pango_layout (IntPtr widget, IntPtr text);
	}
	
	/// <summary>
	/// This creates a Pango list and applies attributes to it with much less overhead than the GTK# version.
	/// </summary>
	class FastPangoAttrList : IDisposable
	{
		IntPtr list;
		
		public FastPangoAttrList ()
		{
			
		}
		
		public void AddStyleAttribute (Pango.Style style, uint start, uint end)
		{
			Add (pango_attr_style_new (style), start, end);
		}
		
		public void AddWeightAttribute (Pango.Weight weight, uint start, uint end)
		{
			Add (pango_attr_weight_new (weight), start, end);
		}
		
		public void AddForegroundAttribute (ushort r, ushort g, ushort b, uint start, uint end)
		{
			Add (pango_attr_foreground_new (r, g, b), start, end);
		}
		
		public void AddBackgroundAttribute (ushort r, ushort g, ushort b, uint start, uint end)
		{
			Add (pango_attr_background_new (r, g, b), start, end);
		}
		
		public void AddUnderlineAttribute (Pango.Underline underline, uint start, uint end)
		{
			Add (pango_attr_underline_new (underline), start, end);
		}
		
		void Add (IntPtr attribute, uint start, uint end)
		{
			unsafe {
				PangoAttribute *attPtr = (PangoAttribute *) attribute;
				attPtr->start_index = start;
				attPtr->end_index = end;
			}
		}
		
		[DllImport (PangoUtil.LIBPANGO)]
		static extern IntPtr pango_attr_style_new (Pango.Style style);
		
		[DllImport (PangoUtil.LIBPANGO)]
		static extern IntPtr pango_attr_stretch_new (Pango.Stretch stretch);
		
		[DllImport (PangoUtil.LIBPANGO)]
		static extern IntPtr pango_attr_weight_new (Pango.Weight weight);
		
		[DllImport (PangoUtil.LIBPANGO)]
		static extern IntPtr pango_attr_foreground_new (ushort red, ushort green, ushort blue);
		
		[DllImport (PangoUtil.LIBPANGO)]
		static extern IntPtr pango_attr_background_new (ushort red, ushort green, ushort blue);
		
		[DllImport (PangoUtil.LIBPANGO)]
		static extern IntPtr pango_attr_underline_new (Pango.Underline underline);
		
		struct PangoAttribute
		{
			IntPtr klass;
			public uint start_index;
			public uint end_index;
		}
		
		public void Dispose ()
		{
			GC.SuppressFinalize (this);
			Destroy ();
		}
		
		void Destroy ()
		{
			
		}
		
		~FastPangoAttrList ()
		{
			Destroy ();
		}
	}
}