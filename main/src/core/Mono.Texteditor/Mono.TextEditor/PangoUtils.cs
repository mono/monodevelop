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
	public static class PangoUtils
	{
		const string LIBGTK          = "libgtk-win32-2.0-0.dll";
		const string LIBATK          = "libatk-1.0-0.dll";
		const string LIBGLIB         = "libglib-2.0-0.dll";
		const string LIBGDK          = "libgdk-win32-2.0-0.dll";
		const string LIBGOBJECT      = "libgobject-2.0-0.dll";
		const string LIBPANGO        = "libpango-1.0-0.dll";
		const string LIBPANGOCAIRO   = "libpangocairo-1.0-0.dll";

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
}

