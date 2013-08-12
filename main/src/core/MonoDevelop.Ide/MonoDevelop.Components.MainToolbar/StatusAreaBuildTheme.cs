//
// StatusAreaBuildTheme.cs
//
// Author:
//       Jason Smith <jason@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using System.Diagnostics;
using Gtk;
using MonoDevelop.Components;
using Cairo;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;

using StockIcons = MonoDevelop.Ide.Gui.Stock;

namespace MonoDevelop.Components.MainToolbar
{
	internal class StatusAreaBuildTheme : StatusAreaTheme
	{
		protected override void DrawBackground (Context context, Gdk.Rectangle region)
		{
			LayoutRoundedRectangle (context, region);
			context.Clip ();

			context.Color = CairoExtensions.ParseColor ("D3E6FF");
			context.Paint ();

			context.Save ();
			context.Translate (region.X + region.Width / 2.0, region.Y + region.Height);

			using (var rg = new RadialGradient (0, 0, 0, 0, 0, region.Height * 1.2)) {
				var color = CairoExtensions.ParseColor ("E5F0FF");
				rg.AddColorStop (0, color);
				color.A = 0;
				rg.AddColorStop (1, color);

				context.Scale (region.Width / (double)region.Height, 1.0);
				context.Pattern = rg;
				context.Paint ();
			}

			context.Restore ();

			LayoutRoundedRectangle (context, region, -3, -3, 2);
			context.Color = new Cairo.Color (1, 1, 1, 0.4);
			context.LineWidth = 1;
			context.StrokePreserve ();

			context.Clip ();

			int boxSize = 11;
			int x = region.Left + (region.Width % boxSize) / 2;
			for (; x < region.Right; x += boxSize) {
				context.MoveTo (x + 0.5, region.Top);
				context.LineTo (x + 0.5, region.Bottom);
			}

			int y = region.Top + (region.Height % boxSize) / 2;
			y += boxSize / 2;
			for (; y < region.Bottom; y += boxSize) {
				context.MoveTo (region.Left, y + 0.5);
				context.LineTo (region.Right, y + 0.5);
			}

			context.Color = new Cairo.Color (1, 1, 1, 0.2);
			context.Stroke ();

			context.ResetClip ();
		}
	}
}

