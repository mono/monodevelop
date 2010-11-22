// 
// TimeLineWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.ComponentModel;
using Gtk;
using Cairo;
using Mono.TextEditor;

namespace MonoDevelop.Profiler
{
	[ToolboxItem (true)]
	public class TimeLineWidget : DrawingArea
	{
		ProfileDialog dialog;
		int maxEvents = 230;
		int slider = 0;
		
		public TimeLineWidget (ProfileDialog dialog)
		{
			this.dialog = dialog;
			Events |= Gdk.EventMask.ButtonMotionMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask;
		}
		
		public void Update ()
		{
			maxEvents = (int)(Math.Round (dialog.visitor.Events.Max () / 100.0) * 100 + 30);
			QueueDraw ();
		}
		
		double pressStart, pressEnd;
		bool pressed;
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1)
				pressed = true;
			if (evnt.Y > Allocation.Height - scrollBarHeight) {
				MoveSlider (evnt.X);
			} else {
				if (evnt.Button == 1) {
					pressStart = pressEnd = slider + Math.Max (boxWidth, evnt.X) / 2;
				} else {
					pressStart = pressEnd = 0;
					dialog.SetTime (0, 0);
				}
			}
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				var min = 100000 * (Math.Min (pressStart, pressEnd) - boxWidth);
				var max = 100000 * (Math.Max (pressStart, pressEnd) - boxWidth);
				dialog.SetTime ((ulong)min, (ulong)max);
				pressed = false;
			}
			return base.OnButtonReleaseEvent (evnt);
		}
		
		void MoveSlider (double x)
		{
			if (x >= boxWidth) {
				double length = Allocation.Width - boxWidth;
				double displayLength = length / 2;
			
				slider = Math.Max (0, (int)(dialog.visitor.Events.Count * (x - boxWidth) / length - displayLength / 2));
				QueueDraw ();
			}
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			if (pressed) {
				if (evnt.Y > Allocation.Height - scrollBarHeight) {
					MoveSlider (evnt.X);
				} else {
					pressEnd = slider + Math.Max (boxWidth, evnt.X) / 2;
					QueueDraw ();
				}
			}
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
		}
		
		const double scrollBarHeight = 22;
		const double timeHeight = 28;
		const double eventHeight = 20;
		const double boxWidth = 60;
		const double eventMetricLength = 4;
		
		static readonly Color timeGradientStartColor = new Color (0.85, 0.85, 0.85);
		static readonly Color timeGradientEndColor = new Color (0.91, 0.91, 0.91);
		static readonly Color lineColor = new Color (0.6, 0.6, 0.6);
		
		void DrawBackground (Cairo.Context gr)
		{
			gr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
			gr.Color = new Color (1, 1, 1);
			gr.Fill ();
			
			gr.Rectangle (0, 0, Allocation.Width, timeHeight);
			var gradient = new LinearGradient (0, 0, 0, timeHeight);
			gradient.AddColorStop (0, timeGradientStartColor);
			gradient.AddColorStop (1, timeGradientEndColor);
			gr.Pattern = gradient;
			gr.Fill ();
			
			gr.LineWidth = 1;
			gr.Color = lineColor;
			gr.MoveTo (0, timeHeight + 0.5);
			gr.LineTo (Allocation.Width, timeHeight + 0.5);
			gr.Stroke ();
			
			gr.MoveTo (0, timeHeight + eventHeight + 0.5);
			gr.LineTo (Allocation.Width, timeHeight + eventHeight + 0.5);
			gr.Stroke ();
			
			gr.MoveTo (boxWidth + 0.5, 0);
			gr.LineTo (boxWidth + 0.5, Allocation.Height);
			gr.Stroke ();
		}
		
		void DrawScrollBar (Context gr)
		{
			gr.LineWidth = 1;
			
			gr.Rectangle (boxWidth, Allocation.Height - scrollBarHeight, Allocation.Width - boxWidth, scrollBarHeight);
			gr.Color = new Color (0.7, 0.7, 0.7);
			gr.Fill ();
			
			double length = Allocation.Width - boxWidth;
			double displayLength = length / 2;
			double f = (double)displayLength / dialog.visitor.Events.Count;
			double s = slider * 2 * f;
			double w = length * f;
			
			gr.Rectangle (boxWidth + s + 0.5, Allocation.Height - scrollBarHeight + 0.5, w, scrollBarHeight);
			gr.Color = new Color (1, 1, 1);
			gr.FillPreserve ();
			gr.Color = new Color (0, 0, 0);
			gr.Stroke ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var gr = Gdk.CairoHelper.Create (evnt.Window)) {
				
				DrawBackground (gr);
				
				var layout = gr.CreateLayout ();
				layout.FontDescription = Pango.FontDescription.FromString ("Tahoma 8");
				layout.SetText ("Time");
				int w, h;
				layout.GetPixelSize (out w, out h);
				gr.MoveTo ((boxWidth - w) / 2, (timeHeight - h) / 2);
				gr.ShowLayout (layout);
				
				layout.SetText ("Events");
				layout.GetPixelSize (out w, out h);
				gr.MoveTo ((boxWidth - w) / 2, timeHeight + (eventHeight - h) / 2);
				gr.ShowLayout (layout);
				
				for (int i = 100; i < maxEvents; i += 100) {
					layout.SetText (i.ToString ());
					layout.GetPixelSize (out w, out h);
					double y = Allocation.Height - i * (Allocation.Height - timeHeight - eventHeight) / maxEvents;
					gr.MoveTo (boxWidth - w - eventMetricLength - 2, y - h / 2);
					gr.ShowLayout (layout);
					
					gr.MoveTo (boxWidth - eventMetricLength, y);
					gr.LineTo (boxWidth, y);
					gr.Stroke ();
				}
				var eventMetricHeight = Allocation.Height - timeHeight - eventHeight - scrollBarHeight;
				gr.MoveTo (boxWidth + 1, Allocation.Height - scrollBarHeight);
				gr.Color = new Color (1, 0, 0);
				double x = boxWidth + 1;
				for (int i = slider; i < dialog.visitor.Events.Count && x < Allocation.Width; i++) {
					var e = dialog.visitor.Events [i];
					gr.LineTo (x, Allocation.Height - scrollBarHeight - (eventMetricHeight * e / (double)maxEvents));
					x += 2;
				}
				gr.Stroke ();
				
				gr.Color = lineColor;
				gr.MoveTo (boxWidth + 1 + dialog.visitor.Events.Count * 2, 0);
				gr.LineTo (boxWidth + 1 + dialog.visitor.Events.Count * 2, 4);
				gr.Stroke ();
				
				layout.SetText (string.Format ("{0:0.0}ms", dialog.totalTime / 1000000));
				layout.GetPixelSize (out w, out h);
				gr.MoveTo (boxWidth + 1 + dialog.visitor.Events.Count * 2 - w / 2, 5);
				gr.ShowLayout (layout);
				
				layout.Dispose ();
				x = (pressStart - slider) * 2;
				double delta = 0;
				if (x < boxWidth)
					delta = boxWidth - x;
				double width = pressEnd * 2 - pressStart * 2 - delta;
				if (width > 0) {
					gr.Rectangle (x + delta, 0, width, Allocation.Height - scrollBarHeight);
					gr.Color = new Color (0, 1, 1, 0.2);
					gr.Fill ();
				}
				
				gr.Color = new Color (0, 1, 1, 0.3);
				if (x > boxWidth) {
					gr.MoveTo (x, 0);
					gr.LineTo (x, Allocation.Height);
					gr.Stroke ();
				}
				
				x = (pressEnd - slider) * 2;
				if (x > boxWidth) {
					gr.MoveTo (x, 0);
					gr.LineTo (x, Allocation.Height);
					gr.Stroke ();
				}
				
				DrawScrollBar (gr);
			}

			return base.OnExposeEvent (evnt);
		}
	}
}