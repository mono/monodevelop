//
// TabStrip.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.Linq;
using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using Cairo;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using Xwt.Motion;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Runtime.InteropServices;

namespace MonoDevelop.Components.DockNotebook
{
	class DragDockNotebookTabManager
	{
		public int X { get; set; }
		public int Offset { get; set; }

		public int LastX { get; internal set; }
		public double Progress { get; internal set; }

		public bool IsDragging => Content != null;
		public DockNotebookTab Content { get; private set; }

		public DragDockNotebookTabManager ()
		{

		}

		public void Start (DockNotebookTab element)
		{
			Content = element;
		}

		public void Cancel ()
		{
			Content = null;
		}

		public void Reset ()
		{
			Cancel ();
			X = 0;
		}
	}
}
