//
// InfoFrame.cs
//
// Author:
//       Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

namespace MonoDevelop.Debugger
{
	[System.ComponentModel.ToolboxItem (true)]
	class InfoFrame : Frame
	{
		public InfoFrame ()
		{
			Shadow = ShadowType.None;
		}

		public InfoFrame (Widget child) : this ()
		{
			Child = child;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (GdkWindow)) {
				cr.Rectangle (Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
				cr.ClipPreserve ();

				cr.SetSourceRGB (1.00, 0.98, 0.91);
				cr.FillPreserve ();

				cr.SetSourceRGB (0.87, 0.83, 0.74);
				cr.LineWidth = 2;
				cr.Stroke ();
			}

			return base.OnExposeEvent (evnt);
		}
	}
}
