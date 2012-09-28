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
			VisibleWindow = false;
			HeightRequest = 1;
		}

		const int height = 14;

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (!showProgress)
				return base.OnExposeEvent (evnt);
			using (var ctx = Gdk.CairoHelper.Create (evnt.Window)) {
				ctx.LineWidth = 1;
				ctx.MoveTo (Allocation.X + 0.5, Allocation.Y + 0.5);
				ctx.RelLineTo (Allocation.Width, 0);
				ctx.Color = new Cairo.Color (0.8, 0.8, 0.8);
				ctx.Stroke ();

				ctx.MoveTo (Allocation.X + 0.5, Allocation.Y + 0.5);
				ctx.RelLineTo ((double)Allocation.Width * fraction, 0);
				ctx.Color = new Cairo.Color (0.1, 0.1, 0.1);
				ctx.Stroke ();
			}
			return base.OnExposeEvent (evnt);
		}

	}
}

