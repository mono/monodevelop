// 
// RoundButton.cs
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
using MonoDevelop.Components;
using Cairo;
using MonoDevelop.Ide;
using System.Reflection;
using Mono.TextEditor;


namespace MonoDevelop.Components.MainToolbar
{
	class RoundButton : Gtk.EventBox
	{
		const int height = 32;
/*		Cairo.Color borderColor;

		Cairo.Color fill0Color;
		Cairo.Color fill1Color;
		Cairo.Color fill2Color;
		Cairo.Color fill3Color;*/

		Xwt.Drawing.Image btnNormal/*, btnInactive, btnHover, btnPressed*/;

		Xwt.Drawing.Image iconRunNormal, iconRunDisabled;
		Xwt.Drawing.Image iconStopNormal, iconStopDisabled;
		Xwt.Drawing.Image iconBuildNormal, iconBuildDisabled;

		public RoundButton ()
		{
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.LeaveNotifyMask | EventMask.PointerMotionMask;
			VisibleWindow = false;
			SetSizeRequest (height, height);

			btnNormal = Xwt.Drawing.Image.FromResource (GetType (), "btn-execute-normal-32.png");
//			btnInactive = new LazyImage ("btn-execute-disabled-32.png");
//			btnPressed = new LazyImage ("btn-execute-pressed-32.png");
//			btnHover = new LazyImage ("btn-execute-hover-32.png");

			iconRunNormal = Xwt.Drawing.Image.FromResource (GetType (), "ico-execute-normal-32.png");
			iconRunDisabled = Xwt.Drawing.Image.FromResource (GetType (), "ico-execute-disabled-32.png");

			iconStopNormal = Xwt.Drawing.Image.FromResource (GetType (), "ico-stop-normal-32.png");
			iconStopDisabled = Xwt.Drawing.Image.FromResource (GetType (), "ico-stop-disabled-32.png");

			iconBuildNormal = Xwt.Drawing.Image.FromResource (GetType (), "ico-build-normal-32.png");
			iconBuildDisabled = Xwt.Drawing.Image.FromResource (GetType (), "ico-build-disabled-32.png");
		}

		StateType hoverState = StateType.Normal;

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{

			State = IsInside (evnt.X, evnt.Y) ? hoverState : StateType.Normal;;
			return base.OnMotionNotifyEvent (evnt);
		}


		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			State = StateType.Normal;
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button == 1 && IsInside (evnt.X, evnt.Y)) {
				hoverState = State = StateType.Selected;
			}
			return true;
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (State == StateType.Selected)
				OnClicked (EventArgs.Empty);
			State = IsInside (evnt.X, evnt.Y) ? StateType.Prelight : StateType.Normal;;
			hoverState = StateType.Prelight; 
			return true;
		}

		bool IsInside (double x, double y)
		{
			var xr = x - Allocation.Width / 2;
			var yr = y - Allocation.Height / 2;
			return Math.Sqrt (xr * xr + yr * yr) <= height / 2;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Width = (int) btnNormal.Size.Width;
			requisition.Height = (int) btnNormal.Size.Height + 2;
			base.OnSizeRequested (ref requisition);
		}

		Xwt.Drawing.Image GetIcon()
		{
			switch (icon) {
			case OperationIcon.Stop:
				return State ==  StateType.Insensitive ? iconStopDisabled : iconStopNormal;
			case OperationIcon.Run:
				return State ==  StateType.Insensitive ? iconRunDisabled : iconRunNormal;
			case OperationIcon.Build:
				return State ==  StateType.Insensitive ? iconBuildDisabled : iconBuildNormal;
			}
			throw new InvalidOperationException ();
		}

		OperationIcon icon;
		public OperationIcon Icon {
			get { return icon; }
			set {
				if (value != icon) {
					icon = value;
					QueueDraw ();
				}
			}
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				DrawBackground (context, Allocation, 15, State);
				var icon = GetIcon();
				context.DrawImage (this, icon, Allocation.X + Math.Max (0, (Allocation.Width - icon.Width) / 2), Allocation.Y + Math.Max (0, (Allocation.Height - icon.Height) / 2));
			}
			return base.OnExposeEvent (evnt);
		}

		void DrawBackground (Cairo.Context context, Gdk.Rectangle region, int radius, StateType state)
		{
			double rad = radius - 0.5;
			int centerX = region.X + region.Width / 2;
			int centerY = region.Y + region.Height / 2;

			context.MoveTo (centerX + rad, centerY);
			context.Arc (centerX, centerY, rad, 0, Math.PI * 2);

			double high;
			double low;
			switch (state) {
			case StateType.Selected:
				high = 0.85;
				low = 1.0;
				break;
			case StateType.Prelight:
				high = 1.0;
				low = 0.9;
				break;
			case StateType.Insensitive:
				high = 0.95;
				low = 0.83;
				break;
			default:
				high = 1.0;
				low = 0.85;
				break;
			}
			using (var lg = new LinearGradient (0, centerY - rad, 0, centerY +rad)) {
				lg.AddColorStop (0, new Cairo.Color (high, high, high));
				lg.AddColorStop (1, new Cairo.Color (low, low, low));
				context.SetSource (lg);
				context.FillPreserve ();
			}

			context.SetSourceRGBA (0, 0, 0, 0.4);
			context.LineWidth = 1;
			context.Stroke ();
		}

		public event EventHandler Clicked;

		protected virtual void OnClicked (EventArgs e)
		{
			EventHandler handler = this.Clicked;
			if (handler != null)
				handler (this, e);
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();

			if (btnNormal != null) {
				btnNormal.Dispose ();
				btnNormal = null;
			}

			if (iconRunNormal != null) {
				iconRunNormal.Dispose ();
				iconRunNormal = null;
			}

			if (iconRunDisabled != null) {
				iconRunDisabled.Dispose ();
				iconRunDisabled = null;
			}

			if (iconStopNormal != null) {
				iconStopNormal.Dispose ();
				iconStopNormal = null;
			}

			if (iconStopDisabled != null) {
				iconStopDisabled.Dispose ();
				iconStopDisabled = null;
			}

			if (iconBuildNormal != null) {
				iconBuildNormal.Dispose ();
				iconBuildNormal = null;
			}

			if (iconBuildDisabled != null) {
				iconBuildDisabled.Dispose ();
				iconBuildDisabled = null;
			}
		}
	}
}

