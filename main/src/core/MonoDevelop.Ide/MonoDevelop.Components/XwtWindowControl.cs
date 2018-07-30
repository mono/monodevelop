﻿//
// XwtWindowControl.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
namespace MonoDevelop.Components
{

	public class XwtWindowControl : Window
	{
		readonly Xwt.WindowFrame window;
		public Xwt.WindowFrame Window {
			get { return window; }
		}

		internal protected XwtWindowControl (Xwt.WindowFrame window) : base (window)
		{
			this.window = window;
		}

#if MAC
		public static implicit operator AppKit.NSWindow (XwtWindowControl d)
		{
			return Xwt.Toolkit.Load (Xwt.ToolkitType.XamMac).GetNativeWindow (d.Window) as AppKit.NSWindow;
		}
#endif

		public static implicit operator Gtk.Window (XwtWindowControl d)
		{
			return Xwt.Toolkit.Load (Xwt.ToolkitType.Gtk3).GetNativeWindow (d.Window) as Gtk.Window;
		}
	}
}
