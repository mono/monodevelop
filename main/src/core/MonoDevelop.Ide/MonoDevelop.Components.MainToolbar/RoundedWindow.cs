//
// SearchPopupWindow.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
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
using Gtk;
using MonoDevelop.Ide;
using Mono.TextEditor;
using Gdk;

namespace MonoDevelop.Components.MainToolbar
{
	class RoundedWindow : Gtk.Window
	{
		public RoundedWindow () : base(Gtk.WindowType.Popup)
		{
			SkipTaskbarHint = true;
			SkipPagerHint = true;
			AppPaintable = true;
			TypeHint = WindowTypeHint.Tooltip;
			CheckScreenColormap ();
		}

		public bool SupportsAlpha {
			get;
			private set;
		}

		void CheckScreenColormap ()
		{
			SupportsAlpha = Screen.RgbaColormap != null;
			if (SupportsAlpha) {
				Colormap = Screen.RgbaColormap;
			} else {
				Colormap = Screen.RgbColormap;
			}
		}

		protected override void OnScreenChanged (Gdk.Screen previous_screen)
		{
			base.OnScreenChanged (previous_screen);
			CheckScreenColormap ();
		}

		protected void DrawTransparentBackground (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				if (SupportsAlpha) {
					context.SetSourceRGBA (1, 1, 1, 0);
				} else {
					context.SetSourceRGB (1, 1, 1);
				}
				context.Operator = Cairo.Operator.DestIn;
				context.Paint ();
			}
		}

		protected void BorderPath (Cairo.Context ctx)
		{
			CairoExtensions.RoundedRectangle (ctx, 0.5, 0.5, Allocation.Width - 1, Allocation.Height - 1, 5);
		}
	}
}

