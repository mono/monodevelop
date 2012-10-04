//
// TopLevelWidgetExtension.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using MonoDevelop.Core;
using Mono.TextEditor;
using System.Collections.Generic;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Ide.TextEditing
{

	public abstract class TopLevelWidgetExtension: FileLineExtension
	{
		public HorizontalAlignment HorizontalAlignment { get; set; }
		public VerticalAlignment VerticalAlignment { get; set; }
		public int OffsetX { get; set; }
		public int OffsetY { get; set; }
		public bool AllowMove { get; set; }

		public abstract Gtk.Widget CreateWidget ();

		public TopLevelWidgetExtension ()
		{
			HorizontalAlignment = HorizontalAlignment.LineRight;
			VerticalAlignment = VerticalAlignment.LineCenter;
		}

		public void ScrollToView ()
		{
			if (ScrollToViewRequested != null)
				ScrollToViewRequested (this, EventArgs.Empty);
		}

		public event EventHandler ScrollToViewRequested;
	}
	
	}
