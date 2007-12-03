//
// GettextDomain.cs: Wrappers for the libintl library.
//
// Authors:
//   Edd Dumbill (edd@usefulinc.com)
//   Jonathan Pryor (jonpryor@vt.edu)
//   Lluis Sanchez Gual (lluis@novell.com)
//
// (C) 2004 Edd Dumbill
// (C) 2005-2006 Jonathan Pryor
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Text;

namespace Mono.Addins.Localization
{
	class GettextDomain
	{
		[DllImport("intl")]
		static extern IntPtr bindtextdomain (IntPtr domainname, IntPtr dirname);
		[DllImport("intl")]
		static extern IntPtr bind_textdomain_codeset (IntPtr domainname, IntPtr codeset);
		[DllImport("intl")]
		static extern IntPtr dgettext (IntPtr domainname, IntPtr instring);
		
		IntPtr ipackage;
		
		public void Init (String package, string localedir)
		{
			if (localedir == null) {
				localedir = System.Reflection.Assembly.GetEntryAssembly ().CodeBase;
				FileInfo f = new FileInfo (localedir);
				string prefix = f.Directory.Parent.Parent.Parent.ToString ();
				prefix = Path.Combine (Path.Combine (prefix, "share"), "locale");
			}
			
			ipackage = StringToPtr (package);
			IntPtr ilocaledir = StringToPtr (localedir);
			IntPtr iutf8 = StringToPtr ("UTF-8");
			
			try {
				if (bindtextdomain (ipackage, ilocaledir) == IntPtr.Zero)
					throw new InvalidOperationException ("Gettext localizer: bindtextdomain failed");
				if (bind_textdomain_codeset (ipackage, iutf8) == IntPtr.Zero)
					throw new InvalidOperationException ("Gettext localizer: bind_textdomain_codeset failed");
			}
			finally {
				Marshal.FreeHGlobal (ilocaledir);
				Marshal.FreeHGlobal (iutf8);
			}
		}
		
		~GettextDomain ()
		{
			Marshal.FreeHGlobal (ipackage);
		}

		public String GetString (String s)
		{
			IntPtr ints = StringToPtr (s);
			try {
				// gettext(3) returns the input pointer if no translation is found
				IntPtr r = dgettext (ipackage, ints);
				if (r != ints)
					return PtrToString (r);
				return s;
			}
			finally {
				Marshal.FreeHGlobal (ints);
			}
		}
		
		static IntPtr StringToPtr (string s)
		{
			if (s == null)
				return IntPtr.Zero;
            byte[] marshal = Encoding.UTF8.GetBytes (s);
            IntPtr mem = Marshal.AllocHGlobal (marshal.Length + 1);
            Marshal.Copy (marshal, 0, mem, marshal.Length);
            Marshal.WriteByte (mem, marshal.Length, 0);
            return mem;		
		}
		
		static string PtrToString (IntPtr ptr)
		{
            if (ptr == IntPtr.Zero)
                return null;
			int sz = 0;
			while (Marshal.ReadByte (ptr, sz) != 0)
				sz++;
            byte[] bytes = new byte [sz - 1];
            Marshal.Copy (ptr, bytes, 0, sz - 1);
            return Encoding.UTF8.GetString (bytes);
		}
	}
}
