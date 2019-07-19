//
// GtkWorkarounds.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2019 
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
using System.Runtime.InteropServices;

using MonoDevelop.Core;

// This partial of the GtkWorkarounds class is shared between MonoDevelop.Ide.dll and md-tool.exe
// Anything that only needs to be in MonoDevelop.Ide should go into the other part.
namespace MonoDevelop.Components
{
	public static partial class GtkWorkarounds
	{
		const string LIBOBJC = "/usr/lib/libobjc.dylib";

		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern IntPtr objc_msgSend_IntPtr (IntPtr klass, IntPtr selector);

		[DllImport (LIBOBJC, EntryPoint = "sel_registerName")]
		static extern IntPtr sel_registerName (string selector);

		[DllImport (LIBOBJC, EntryPoint = "objc_getClass")]
		static extern IntPtr objc_getClass (string klass);

		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern void objc_msgSend_NSInt64_NSInt32 (IntPtr klass, IntPtr selector, int arg);

		static void MacTerminate ()
		{
			var sel_terminate = sel_registerName ("terminate:");
			var app = objc_msgSend_IntPtr (objc_getClass ("NSApplication"), sel_registerName ("sharedApplication"));

			objc_msgSend_NSInt64_NSInt32 (app, sel_terminate, 0);
		}

		public static void Terminate ()
		{
			if (Platform.IsMac)
				MacTerminate ();
		}
	}
}
