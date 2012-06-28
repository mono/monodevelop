//
// StyledProgressBar.cs
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
using Cairo;

namespace MonoDevelop.Components.MainToolbar
{
	class StyledProgressBar : EventBox
	{
		Color borderColor;
		Color progressColor;

		double fraction;
		public double Fraction {
			get {
				return fraction;
			}
			set {
				fraction = value;
				QueueDraw ();
			}
		}

		bool showProgress;
		public bool ShowProgress {
			get {
				return showProgress;
			}
			set {
				showProgress = value;
				QueueDraw ();
			}
		}
		public StyledProgressBar ()
		{
			borderColor = CairoExtensions.ParseColor ("777a7a");
			progressColor = CairoExtensions.ParseColor ("95999a");
			VisibleWindow = false;
			SetSizeRequest (200, height);
		}

		const int height = 14;

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (!showProgress)
				return base.OnExposeEvent (evnt);
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.LineWidth = 1;
				var y = Allocation.Y + (Allocation.Height - height) / 2;
				const int leftBorder = 0;
				const int rightBorder = 6;
				var barWidth = Allocation.Width - leftBorder - rightBorder;
				context.Rectangle (Allocation.X + 0.5 + leftBorder, y + 0.5, barWidth, height);
				context.Color = borderColor;
				context.Stroke ();
				context.Color = progressColor;
				for (double x = leftBorder; x < barWidth * fraction; x += 3) {
					context.Rectangle (Allocation.X + x + 0.5, y + 1 + 0.5, 2, height - 2);
					context.Fill ();
				}

			}
			return base.OnExposeEvent (evnt);
		}

	}
}

