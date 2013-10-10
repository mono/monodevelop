//
// HoverCloseButton.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.SourceEditor
{
	public class HoverCloseButton : EventBox
	{
		bool hovered;

		public HoverCloseButton ()
		{
			VisibleWindow = false;
			Events |= EventMask.LeaveNotifyMask | EventMask.EnterNotifyMask | EventMask.ButtonPressMask | EventMask.ButtonReleaseMask;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = requisition.Height = 16;
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			hovered = true;
			QueueDraw ();
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			hovered = false;
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		bool click;
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button == 1 && hovered) 
				click = true;
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (click && hovered)
				OnClicked (EventArgs.Empty);
			click = false;
			return base.OnButtonReleaseEvent (evnt);
		}

		public event EventHandler Clicked;

		protected virtual void OnClicked (EventArgs e)
		{
			var handler = Clicked;
			if (handler != null)
				handler (this, e);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var cr = CairoHelper.Create (evnt.Window)) {
				DrawCloseButton (cr, new Gdk.Point (Allocation.X + Allocation.Width / 2, Allocation.Y + Allocation.Height / 2), hovered, 1.0, 0);
			}
			return base.OnExposeEvent (evnt);
		}

		static void DrawCloseButton (Cairo.Context context, Gdk.Point center, bool hovered, double opacity, double animationProgress)
		{
			if (hovered) {
				double radius = 6;
				context.Arc (center.X, center.Y, radius, 0, Math.PI * 2);
				context.SetSourceRGBA (.6, .6, .6, opacity);
				context.Fill ();

				context.SetSourceRGBA (0.95, 0.95, 0.95, opacity);
				context.LineWidth = 2;

				context.MoveTo (center.X - 3, center.Y - 3);
				context.LineTo (center.X + 3, center.Y + 3);
				context.MoveTo (center.X - 3, center.Y + 3);
				context.LineTo (center.X + 3, center.Y - 3);
				context.Stroke ();
			} else {
				double lineColor = .63 - .1 * animationProgress;
				double fillColor = .74;

				double heightMod = Math.Max (0, 1.0 - animationProgress * 2);
				context.MoveTo (center.X - 3, center.Y - 3 * heightMod);
				context.LineTo (center.X + 3, center.Y + 3 * heightMod);
				context.MoveTo (center.X - 3, center.Y + 3 * heightMod);
				context.LineTo (center.X + 3, center.Y - 3 * heightMod);

				context.LineWidth = 2;
				context.SetSourceRGBA (lineColor, lineColor, lineColor, opacity);
				context.Stroke ();

				if (animationProgress > 0.5) {
					double partialProg = (animationProgress - 0.5) * 2;
					context.MoveTo (center.X - 3, center.Y);
					context.LineTo (center.X + 3, center.Y);

					context.LineWidth = 2 - partialProg;
					context.SetSourceRGBA (lineColor, lineColor, lineColor, opacity);
					context.Stroke ();


					double radius = partialProg * 3.5;

					// Background
					context.Arc (center.X, center.Y, radius, 0, Math.PI * 2);
					context.SetSourceRGBA (fillColor, fillColor, fillColor, opacity);
					context.Fill ();

					// Inset shadow
					using (var lg = new Cairo.LinearGradient (0, center.Y - 5, 0, center.Y)) {
						context.Arc (center.X, center.Y + 1, radius, 0, Math.PI * 2);
						lg.AddColorStop (0, new Cairo.Color (0, 0, 0, 0.2 * opacity));
						lg.AddColorStop (1, new Cairo.Color (0, 0, 0, 0));
						context.Pattern = lg;
						context.Stroke ();
					}

					// Outline
					context.Arc (center.X, center.Y, radius, 0, Math.PI * 2);
					context.SetSourceRGBA (lineColor, lineColor, lineColor, opacity);
					context.Stroke ();
				}
			}
		}
	}
}

