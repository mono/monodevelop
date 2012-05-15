// 
// ButtonBar.cs
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
using Gdk;
using System.Collections.Generic;
using MonoDevelop.Components;
using Cairo;

namespace MonoDevelop.Compontents.MainToolbar
{
	public class ButtonBar : EventBox
	{
		const int buttonWidth = 30;
		List<Pixbuf> buttons = new List<Pixbuf> ();

		Cairo.Color borderColor;

		Cairo.Color fill0Color;
		Cairo.Color fill1Color;
		Cairo.Color fill2Color;
		Cairo.Color fill3Color;

		Cairo.Color separator1Color;
		Cairo.Color separator2Color;


		public ButtonBar ()
		{
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask;

			borderColor = CairoExtensions.ParseColor ("8c8c8c");
			fill0Color = CairoExtensions.ParseColor ("ffffff");
			fill1Color = CairoExtensions.ParseColor ("fbfbfb");
			fill2Color = CairoExtensions.ParseColor ("d6d6d6");
			fill3Color = CairoExtensions.ParseColor ("767676");

			separator1Color = CairoExtensions.ParseColor ("c4c4c4", 0.9);
			separator2Color = CairoExtensions.ParseColor ("efefef", 0.9);

		}

		public void Add (Pixbuf pixbuf)
		{
			buttons.Add (pixbuf);
			SetSizeRequest (buttons.Count * buttonWidth, -1);
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				CairoExtensions.RoundedRectangle (context, 0, 0, Allocation.Width, Allocation.Height, 4);

				var lg = new LinearGradient (0, 0, 0, Allocation.Height);
				if (State == StateType.Selected) {
					lg.AddColorStop (0, fill2Color);
					lg.AddColorStop (1, fill3Color);
				} else if (State == StateType.Prelight) {
					lg.AddColorStop (0, fill0Color);
					lg.AddColorStop (1, fill1Color);
				} else {
					lg.AddColorStop (0, fill1Color);
					lg.AddColorStop (1, fill2Color);
				}
				context.Pattern = lg;
				context.FillPreserve ();

				context.LineWidth = 1;
				context.Color = borderColor;
				context.Stroke ();


				for (int i = 0; i < buttons.Count; i++) {
					int x = i * buttonWidth;
					evnt.Window.DrawPixbuf (Style.WhiteGC, 
					                        buttons [i], 
					                        0, 0, 
					                        x + (buttonWidth - buttons [i].Width) / 2, 
					                        (Allocation.Height - buttons [i].Height) / 2, 
					                        buttons [i].Width, buttons [i].Height,
					                        RgbDither.None, 0, 0);
					const int sepHeight = 8;
					context.Color = separator1Color;
					context.MoveTo (x + 0.5, (Allocation.Height - sepHeight) / 2);
					context.LineTo (x + 0.5, (Allocation.Height - sepHeight) / 2 + sepHeight);
					context.Stroke ();
					context.Color = separator2Color;
					context.MoveTo (x + 0.5 + 1, (Allocation.Height - sepHeight) / 2);
					context.LineTo (x + 0.5 + 1, (Allocation.Height - sepHeight) / 2 + sepHeight);
					context.Stroke ();
				}
			}
			return base.OnExposeEvent (evnt);
		}


	}
}

