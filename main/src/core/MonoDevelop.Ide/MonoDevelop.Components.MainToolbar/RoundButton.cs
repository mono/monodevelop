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

		Xwt.Drawing.Image btnNormal, btnInactive, btnHover, btnPressed;

		Xwt.Drawing.Image iconRunNormal, iconRunDisabled;
		Xwt.Drawing.Image iconStopNormal, iconStopDisabled;
		Xwt.Drawing.Image iconBuildNormal, iconBuildDisabled;

		public RoundButton ()
		{
			this.AppPaintable = true;
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.LeaveNotifyMask | EventMask.PointerMotionMask;
			VisibleWindow = false;
			SetSizeRequest (height, height);

			btnNormal = Xwt.Drawing.Image.FromResource (GetType (), "btn-execute-normal-32.png");
			btnInactive = Xwt.Drawing.Image.FromResource (GetType (), "btn-execute-disabled-32.png");
			btnPressed = Xwt.Drawing.Image.FromResource (GetType (), "btn-execute-pressed-32.png");
			btnHover = Xwt.Drawing.Image.FromResource (GetType (), "btn-execute-hover-32.png");

			iconRunNormal = Xwt.Drawing.Image.FromResource (GetType (), "ico-execute-normal-32.png");
			iconRunDisabled = Xwt.Drawing.Image.FromResource (GetType (), "ico-execute-disabled-32.png");

			iconStopNormal = Xwt.Drawing.Image.FromResource (GetType (), "ico-stop-normal-32.png");
			iconStopDisabled = Xwt.Drawing.Image.FromResource (GetType (), "ico-stop-disabled-32.png");

			iconBuildNormal = Xwt.Drawing.Image.FromResource (GetType (), "ico-build-normal-32.png");
			iconBuildDisabled = Xwt.Drawing.Image.FromResource (GetType (), "ico-build-disabled-32.png");
		}

		StateFlags hoverState = StateFlags.Prelight;

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{

			this.SetStateFlags( IsInside (evnt.X, evnt.Y) ? hoverState : StateFlags.Normal, true);;
			return base.OnMotionNotifyEvent (evnt);
		}


		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			this.SetStateFlags (StateFlags.Normal, true);
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button == 1 && IsInside (evnt.X, evnt.Y)) {
				hoverState = StateFlags.Selected;
				this.SetStateFlags (hoverState, true);
			}
			return true;
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (State == StateType.Selected)
				OnClicked (EventArgs.Empty);
			this.SetStateFlags( IsInside (evnt.X, evnt.Y) ? StateFlags.Prelight : StateFlags.Normal, true);;
			hoverState = StateFlags.Prelight; 
			return true;
		}

		bool IsInside (double x, double y)
		{
			var xr = x - Allocation.Width / 2;
			var yr = y - Allocation.Height / 2;
			return Math.Sqrt (xr * xr + yr * yr) <= height / 2;
		}

		protected override void OnGetPreferredHeight (out int min_height, out int natural_height)
		{
			min_height = (int) btnNormal.Size.Height + 2;
			base.OnGetPreferredHeight (out min_height, out natural_height);
		}

		protected override void OnGetPreferredWidth (out int min_width, out int natural_width)
		{
			min_width = (int) btnNormal.Size.Width;
			base.OnGetPreferredWidth (out min_width, out natural_width);
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

//		protected override bool OnExposeEvent (EventExpose evnt)
//		{
//			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
//				DrawBackground (context, Allocation, 15, State);
//				var icon = GetIcon();
//				context.DrawImage (this, icon, Allocation.X + Math.Max (0, (Allocation.Width - icon.Width) / 2), Allocation.Y + Math.Max (0, (Allocation.Height - icon.Height) / 2));
//			}
//			return base.OnExposeEvent (evnt);
//		}

		void DrawBackground (Cairo.Context context, Gdk.Rectangle region, int radius, StateType state)
		{
			int x = region.X + region.Width / 2;
			int y = region.Y + region.Height / 2;

			var img = btnNormal;
			switch (state) {
			case StateType.Insensitive:
				img = btnInactive; break;
			case StateType.Prelight:
				img = btnHover; break;
			case StateType.Selected:
				img = btnPressed; break;
			}

			x -= (int) img.Width / 2;
			y -= (int) img.Height / 2;

			context.DrawImage (this, img, x, y);
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

			if (btnInactive != null) {
				btnInactive.Dispose ();
				btnInactive = null;
			}

			if (btnPressed != null) {
				btnPressed.Dispose ();
				btnPressed = null;
			}

			if (btnHover != null) {
				btnHover.Dispose ();
				btnHover = null;
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

