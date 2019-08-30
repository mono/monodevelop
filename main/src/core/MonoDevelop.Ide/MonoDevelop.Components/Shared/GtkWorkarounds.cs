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
		public static void Terminate ()
		{
#if MAC
			var app = Mac.Messaging.IntPtr_objc_msgSend (
				ObjCRuntime.Class.GetHandle ("NSApplication"),
				ObjCRuntime.Selector.GetHandle ("sharedApplication"));

			Mac.Messaging.void_objc_msgSend_IntPtr (app, ObjCRuntime.Selector.GetHandle ("terminate:"), IntPtr.Zero);
#endif
		}
	}
}
