// 
// TimeLineView.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using Gtk;
using Gdk;
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Components;
using System.Linq;
using Mono.TextEditor;

namespace Mono.Instrumentation.Monitor
{
	[System.ComponentModel.ToolboxItem(true)]
	class TimeLineView: EventBox
	{
		Counter mainCounter;
		CounterValue mainValue;
		List<ChartSerieInfo> extraCounters = new List<ChartSerieInfo> ();
		List<CounterValueInfo> data;
		List<HotSpot> hostSpots = new List<HotSpot> ();
		int lastHeight;
		int lastWidth;
		int baseTime;
		CounterValueInfo focusedValue;
		CounterValueInfo overValue;
		
		Pango.Layout layout;
		
		double scale = 300; // Pixels per second
		
		const int padding = 3;
		const int LineSpacing = 1;
		const int MarkerWidth = 20;
		const int LineEndWidth = 20;
		const int ExpanderSize = 8;
		const int ChildIndent = 20;
		const int SelectedValuePadding = 3;
		
		class HotSpot
		{
			public Rectangle Rect;
			public System.Action Action;
			public System.Action OnMouseOver;
			public System.Action OnMouseLeave;
			public bool IsOver;
		}
		
		class CounterValueInfo
		{
			public DateTime Time;
			public Counter Counter;
			public string Trace;
			public IEnumerable<TimerTrace> TimerTraces;
			public List<CounterValueInfo> ExpandedTimerTraces;
			public bool Expanded;
			public bool CanExpand;
			public TimeSpan Duration;
		}
		
		public TimeLineView ()
		{
			layout = new Pango.Layout (this.PangoContext);
			layout.FontDescription = Pango.FontDescription.FromString ("Tahoma 8");
			Events |= EventMask.PointerMotionMask | EventMask.LeaveNotifyMask;
		}
		
		
		public void Clear ()
		{
			mainCounter = null;
			extraCounters.Clear ();
			data = null;
		}
		
		public void SetMainCounter (Counter c, CounterValue value)
		{
			mainCounter = c;
			mainValue = value;
			data = null;
			Scale = scale;
			QueueDraw ();
		}
		
		public void AddCounter (ChartSerieInfo c)
		{
			extraCounters.Add (c);
			data = null;
			QueueDraw ();
		}
		
		public void ExpandAll ()
		{
		}
		
		public void CollapseAll ()
		{
		}
		
		bool singleThread;
		
		public bool SingleThread {
			get {
				return singleThread; 
			}
			set {
				singleThread = value;
				BuildData ();
				QueueDraw ();
			}
		}
		
		double timeScale;
		public double TimeScale {
			get {
				return timeScale;
			}
			set {
				timeScale = value;
				Scale = (value * 300) / 100;
			}
		}
		
		int zoom;
		public int Zoom {
			get {
				return zoom;
			}
			set {
				zoom = value;
				QueueDraw ();
			}
		}
		
		public double Scale {
			get { return this.scale; }
			set {
				this.scale = value;
				QueueDraw ();
			}
		}
		
		void BuildData ()
		{
			data = new List<CounterValueInfo> ();
			if (mainCounter == null)
				return;
			
			DateTime lastTime = mainValue.TimeStamp + mainValue.Duration;
			FillTimerTraces (mainValue.GetTimerTraces (), data, mainValue.TimeStamp, lastTime);
			FixValueList (data, lastTime);
			
			foreach (CounterValueInfo val in data) {
				ExpandValue (val);
//				val.Expanded = true;
			}
		}
		
		void FillTimerTraces (IEnumerable<TimerTrace> traces, List<CounterValueInfo> list, DateTime startTime, DateTime endTime)
		{
			if (!traces.Any ()) {
				GetValues (list, startTime, endTime, false, false);
			} else {
				GetValues (list, startTime, traces.First ().Timestamp, false, false);
			}
			
			foreach (TimerTrace tt in traces) {
				CounterValueInfo v = new CounterValueInfo ();
				v.Time = tt.Timestamp;
				v.Trace = tt.Message;
				v.Counter = mainCounter;
				v.CanExpand = true;
				list.Add (v);
			}
		}
		
		void ExpandValue (CounterValueInfo val)
		{
			List<CounterValueInfo> list = new List<CounterValueInfo> ();
			
			if (val.TimerTraces != null) {
				FillTimerTraces (val.TimerTraces, list, val.Time, val.Time + val.Duration);
			} else {
				GetValues (list, val.Time, val.Time + val.Duration, false, false);
			}
			if (list.Count == 0)
				val.CanExpand = false;
			FixValueList (list, val.Time + val.Duration);
			val.ExpandedTimerTraces = list;
		}
		
		void GetValues (List<CounterValueInfo> list, DateTime startTime, DateTime endTime, bool includeStart, bool includeEnd)
		{
			foreach (Counter c in App.Service.GetCounters ()) {
				foreach (CounterValue cval in c.GetValuesBetween (startTime, endTime)) {
					if (singleThread && cval.ThreadId != mainValue.ThreadId)
						continue;
					if (!includeStart && cval.TimeStamp == startTime)
						continue;
					if (!includeEnd && cval.TimeStamp == endTime)
						continue;
					CounterValueInfo v = new CounterValueInfo ();
					v.Time = cval.TimeStamp;
					v.Counter = c;
					v.Trace = "[" + c.Name + ": " + cval.Value + "]";
					if (!string.IsNullOrEmpty (cval.Message))
						v.Trace = cval.Message + " " + v.Trace;
					if (cval.HasTimerTraces) {
						v.TimerTraces = cval.GetTimerTraces ();
						v.Duration = cval.Duration;
						v.CanExpand = true;
					}
					list.Add (v);
				}
			}
		}
		
		void ToggleExpand (CounterValueInfo val)
		{
			val.Expanded = !val.Expanded;
			if (!val.Expanded || val.ExpandedTimerTraces != null) {
				QueueDraw ();
				return;
			}

			ExpandValue (val);
			QueueDraw ();
		}
		
		void FixValueList (List<CounterValueInfo> list, DateTime endTime)
		{
			if (list.Count == 0)
				return;
			list.Sort (delegate (CounterValueInfo v1, CounterValueInfo v2) {
				return v1.Time.CompareTo (v2.Time);
			});
			
			for (int n=0; n<list.Count - 1; n++) {
				CounterValueInfo val = list [n];
				if (val.TimerTraces != null) {
					int i = n + 1;
					DateTime localEndTime = val.Time + val.Duration;
					while (i < list.Count && list [i].Time < localEndTime) {
						i++;
					}
					list.RemoveRange (n + 1, i - n - 1);
					val.CanExpand = (i > n + 1) || (val.TimerTraces.Any ());
				} else
					val.Duration = list [n + 1].Time - val.Time;
			}
			list [list.Count - 1].Duration = endTime - list [list.Count - 1].Time;
		}
		
		HotSpot AddHotSpot (double x, double y, double w, double h)
		{
			HotSpot hp = new HotSpot ();
			hp.Rect = new Rectangle ((int)x, (int)y, (int)w, (int)h);
			hostSpots.Add (hp);
			return hp;
		}
		
		uint timeAnim = 0;
		int destBaseTime;
		
		void SetBaseTime (int dy)
		{
			if (timeAnim != 0)
				GLib.Source.Remove (timeAnim);

			destBaseTime = dy;
			
			timeAnim = GLib.Timeout.Add (40, delegate {
				baseTime = baseTime + (destBaseTime - baseTime) / 2;
				QueueDraw ();

				bool cont = baseTime != destBaseTime;
				if (!cont)
					timeAnim = 0;
				return cont;
			});
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			foreach (HotSpot hp in hostSpots) {
				if (hp.Rect.Contains ((int)evnt.X, (int)evnt.Y) && hp.OnMouseOver != null) {
					if (!hp.IsOver && hp.OnMouseOver != null) {
						hp.IsOver = true;
						hp.OnMouseOver ();
					}
				} else if (hp.IsOver) {
					if (hp.IsOver && hp.OnMouseLeave != null) {
						hp.IsOver = false;
						hp.OnMouseLeave ();
					}
				}
			}
			return base.OnMotionNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			foreach (HotSpot hp in hostSpots) {
				if (hp.IsOver && hp.OnMouseLeave != null) {
					hp.IsOver = false;
					hp.OnMouseLeave ();
					break;
				}
			}
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			foreach (HotSpot hp in hostSpots) {
				if (hp.Rect.Contains ((int)evnt.X, (int)evnt.Y) && hp.Action != null) {
					hp.Action ();
					break;
				}
			}
			return base.OnButtonPressEvent (evnt);
		}

		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (data == null)
				BuildData ();
			
			hostSpots.Clear ();
			int ytop = padding;
			int markerX = 3;
			int lx = markerX + MarkerWidth + 1;
			int tx = 250;
			int ty = ytop;
			int maxx = lx;
			int maxy = 0;
			
			DateTime initialTime = mainValue.TimeStamp;
			
			Cairo.Context ctx = CairoHelper.Create (GdkWindow);

			using (Gdk.GC gc = new Gdk.GC (GdkWindow)) {
				gc.RgbFgColor = Style.White;
				GdkWindow.DrawRectangle (gc, true, 0, 0, Allocation.Width, Allocation.Height);

				// Draw full time marker

				ctx.NewPath ();
				ctx.Rectangle (markerX, ytop + baseTime + 0.5, MarkerWidth / 2, ((mainValue.Duration.TotalMilliseconds * scale) / 1000));
				HslColor hsl = Style.Foreground (Gtk.StateType.Normal);
				hsl.L = 0.8;
				ctx.SetSourceColor (hsl);
				ctx.Fill ();

				// Draw values

				foreach (CounterValueInfo val in data) {
					DrawValue (ctx, gc, initialTime, ytop, lx, tx, ref ty, ref maxx, ref maxy, 0, val);
				}

				if (ty > maxy)
					maxy = ty;

				int totalms = (int)mainValue.Duration.TotalMilliseconds;
				int marks = (totalms / 1000) + 1;

				ctx.LineWidth = 1;
				gc.RgbFgColor = Style.Foreground (Gtk.StateType.Normal);

				for (int n = 0; n <= marks; n++) {
					ctx.NewPath ();
					int y = ytop + (int)(n * scale) + baseTime;
					ctx.MoveTo (markerX, y + 0.5);
					ctx.LineTo (markerX + MarkerWidth, y + 0.5);
					ctx.SetSourceColor (Style.Foreground (Gtk.StateType.Normal).ToCairoColor ());
					ctx.Stroke ();

					y += 2;
					layout.SetText (n + "s");
					GdkWindow.DrawLayout (gc, markerX + 1, y + 2, layout);

					int tw, th;
					layout.GetPixelSize (out tw, out th);
					y += th;

					if (y > maxy)
					maxy = y;
			}
			}
			
			((IDisposable)ctx).Dispose ();
			
			maxy += padding;
			maxx += padding;
			
			if (lastHeight != maxy || lastWidth != maxx) {
				lastWidth = maxx;
				lastHeight = maxy;
				SetSizeRequest (maxx, maxy);
			}

			return true;
		}
		
		void DrawValue (Cairo.Context ctx, Gdk.GC gc, DateTime initialTime, int ytop, int lx, int tx, ref int ty, ref int maxx, ref int maxy, int indent, CounterValueInfo val)
		{
			Gdk.Color color;
			if (val.Counter != null)
				color = val.Counter.GetColor ();
			else
				color = Style.Black;
			
			// Draw text
			gc.RgbFgColor = color;
			
			double ms = (val.Time - initialTime).TotalMilliseconds;
			
			string txt = (ms / 1000).ToString ("0.00000") + ": " + (val.Duration.TotalMilliseconds / 1000).ToString ("0.00000") + " " + val.Trace;
			layout.SetText (txt);
			GdkWindow.DrawLayout (gc, tx + indent, ty, layout);
			int tw, th;
			layout.GetPixelSize (out tw, out th);
			if (tx + tw + indent > maxx)
				maxx = tx + tw + indent;
			
			HotSpot hp = AddHotSpot (tx + indent, ty, tw, th);
			int tempTy = ty;
			hp.Action = delegate {
				int ytm = ytop + (int) ((ms * scale) / 1000);
				SetBaseTime ((int) (tempTy + (th / 2) + 0.5) - ytm);
			};
			hp.OnMouseOver += delegate {
				overValue = val;
				QueueDraw ();
			};
			hp.Action += delegate {
				focusedValue = val;
				QueueDraw ();
			};
			
			// Draw time marker
			int ytime = ytop + (int) ((ms * scale) / 1000) + baseTime;
			
			if (val == focusedValue || val == overValue) {
				ctx.NewPath ();
				double dx = val == focusedValue ? 0 : 2;
				ctx.Rectangle (lx + 0.5 + dx - SelectedValuePadding, ytime + 0.5, LineEndWidth - dx*2 + SelectedValuePadding, ((val.Duration.TotalMilliseconds * scale) / 1000));
				HslColor hsl = color;
				hsl.L = val == focusedValue ? 0.9 : 0.8;
				ctx.SetSourceColor (hsl);
				ctx.Fill ();
			}
			
			ctx.NewPath ();
			ctx.LineWidth = 1;
			ctx.MoveTo (lx + 0.5, ytime + 0.5);
			ctx.LineTo (lx + LineEndWidth + 0.5, ytime + 0.5);
			ctx.LineTo (tx - 3 - LineEndWidth + 0.5, ty + (th / 2) + 0.5);
			ctx.LineTo (tx + indent - 3 + 0.5, ty + (th / 2) + 0.5);
			ctx.SetSourceColor (color.ToCairoColor ());
			ctx.Stroke ();
			
			// Expander
			
			bool incLine = true;
			
			if (val.CanExpand) {
				double ex = tx + indent - 3 - ExpanderSize - 2 + 0.5;
				double ey = ty + (th / 2) - (ExpanderSize/2) + 0.5;
				hp = AddHotSpot (ex, ey, ExpanderSize, ExpanderSize);
				DrawExpander (ctx, ex, ey, val.Expanded, false);
				hp.OnMouseOver = delegate {
					using (Cairo.Context c = CairoHelper.Create (GdkWindow)) {
						DrawExpander (c, ex, ey, val.Expanded, true);
					}
				};
				hp.OnMouseLeave = delegate {
					using (Cairo.Context c = CairoHelper.Create (GdkWindow)) {
						DrawExpander (c, ex, ey, val.Expanded, false);
					}
				};
				hp.Action = delegate {
					ToggleExpand (val);
				};
				
				if (val.Expanded && val.ExpandedTimerTraces.Count > 0) {
					ty += th + LineSpacing;
					foreach (CounterValueInfo cv in val.ExpandedTimerTraces)
						DrawValue (ctx, gc, initialTime, ytop, lx, tx, ref ty, ref maxx, ref maxy, indent + ChildIndent, cv);
					incLine = false;
				}
			}
			if (incLine)
				ty += th + LineSpacing;
			
			if (ytime > maxy)
				maxy = ytime;
		}
		
		void DrawExpander (Cairo.Context ctx, double ex, double ey, bool expanded, bool hilight)
		{
			ctx.NewPath ();
			ctx.LineWidth = 1;
			ctx.Rectangle (ex, ey, ExpanderSize, ExpanderSize);
			if (hilight)
				ctx.SetSourceColor (Style.Background (Gtk.StateType.Normal).ToCairoColor ());
			else
				ctx.SetSourceColor (Style.White.ToCairoColor ());
			ctx.FillPreserve ();
			ctx.SetSourceColor (Style.Foreground (Gtk.StateType.Normal).ToCairoColor ());
			ctx.Stroke ();
			ctx.NewPath ();
			ctx.MoveTo (ex + 2, ey + (ExpanderSize/2));
			ctx.RelLineTo (ExpanderSize - 4, 0);
			if (!expanded) {
				ctx.MoveTo (ex + (ExpanderSize/2), ey + 2);
				ctx.RelLineTo (0, ExpanderSize - 4);
			}
			ctx.Stroke ();
		}

		protected override void OnDestroyed ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
			base.OnDestroyed ();
		}
	}
}

