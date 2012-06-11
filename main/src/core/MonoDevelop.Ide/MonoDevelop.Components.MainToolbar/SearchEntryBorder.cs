//
// SearchEntryBorder.cs
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

namespace MonoDevelop.Components.MainToolbar
{
	public class SearchEntryBorder : HBox
	{
		Cairo.Color borderColor;
		Cairo.Color borderColor2;

		public SearchEntryBorder ()
		{
			borderColor = CairoExtensions.ParseColor ("8d8d8d");
			borderColor2 = CairoExtensions.ParseColor ("eaeaea");

			
		}

		static void RoundBorder (Cairo.Context ctx, double x, double y, double w, double h)
		{
			double r = h / 2;
			ctx.Arc (x + r, y + r, r, Math.PI / 2, Math.PI + Math.PI / 2);
			ctx.LineTo (x + w - r, y);

			ctx.Arc (x + w, y + r, r, Math.PI + Math.PI / 2, Math.PI + Math.PI + Math.PI / 2);
			
			ctx.LineTo (x + r, y + h);

			ctx.ClosePath ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			const int height = 22;

			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.LineWidth = 1;
				RoundBorder (context, 
				             4 + Allocation.X + 0.5,  
				             Allocation.Y + 0.5 + (Allocation.Height - height) / 2, 
				             Allocation.Width - height, 
				             height);
				context.Color = new Cairo.Color (1, 1, 1);
				context.Fill ();


				RoundBorder (context, 
				             4 + Allocation.X + 0.5,  
				             1 + Allocation.Y + 0.5 + (Allocation.Height - height) / 2, 
				             Allocation.Width - height, 
				             height);
				context.Color = borderColor2;
				context.Stroke ();

				RoundBorder (context, 
				             4 + Allocation.X + 0.5,  
				             Allocation.Y + 0.5 + (Allocation.Height - height) / 2, 
				             Allocation.Width - height, 
				             height);
				context.LineWidth = 1;
				context.Color = borderColor;
				context.Stroke ();

			}

			return base.OnExposeEvent (evnt);
		}
	}
}

