//
// GtkSpell.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2007 David Makovský
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gtk;

namespace MonoDevelop.Gettext.Editor
{
	// as GtkSpell sharp looks quite old and unmaintained, here is simple wrapper 
	internal static class GtkSpell
	{
		static bool isSupported;
		
#region Native methods
		[DllImport ("libgtkspell", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtkspell_new_attach (IntPtr textView, string locale, IntPtr error);

		[DllImport ("libgtkspell", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtkspell_detach (IntPtr ptr);

		[DllImport ("libgtkspell", CallingConvention = CallingConvention.Cdecl)]
		static extern void gtkspell_recheck_all (IntPtr ptr);

		[DllImport ("libgtkspell", CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr gtkspell_get_from_text_view (IntPtr textView);
		
//		[DllImport ("libgtkspell")]
//		static extern bool gtkspell_set_language (IntPtr spell, string lang, IntPtr error);
#endregion
		
		public static bool IsSupported {
			get { return isSupported; }
		}
		
		static GtkSpell ()
		{
			try {
				TextView tv = new TextView ();
				IntPtr ptr = gtkspell_new_attach (tv.Handle, null, IntPtr.Zero);
				if (ptr != IntPtr.Zero)
					gtkspell_detach (ptr);
				tv.Destroy ();
				tv = null;
				isSupported = true;
			} catch {
				isSupported = false;
			}
		}
		
		public static void Recheck (TextView tv)
		{
			Debug.Assert (IsSupported);
			Debug.Assert (tv != null);
			Debug.Assert (tv.Handle != IntPtr.Zero);
			IntPtr ptr = gtkspell_get_from_text_view (tv.Handle);
			if (ptr != IntPtr.Zero)
				gtkspell_recheck_all (ptr);
		}
		
		public static IntPtr Attach (TextView tv, string locale)
		{
			Debug.Assert (IsSupported);
			Debug.Assert (tv != null);
			Debug.Assert (tv.Handle != IntPtr.Zero);
			return gtkspell_new_attach (tv.Handle, locale, IntPtr.Zero);
		}
		
		public static void Detach (TextView tv)
		{
			gtkspell_detach (tv.Handle);
		}
	}
}
