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


namespace MonoDevelop.Compontents.MainToolbar
{
	public class LazyImage
	{
		string resourceName;

		ImageSurface img;
		public ImageSurface Img {
			get {
				if (img == null)
					img = CairoExtensions.LoadImage (Assembly.GetCallingAssembly (), resourceName);
				return img;
			}
		}

		public LazyImage (string resourceName)
		{
			this.resourceName = resourceName;
		}

		public static implicit operator ImageSurface(LazyImage lazy)
		{
			return lazy.Img;
		}

	}

	public class RoundButton : Gtk.EventBox
	{
		const int height = 32;
/*		Cairo.Color borderColor;

		Cairo.Color fill0Color;
		Cairo.Color fill1Color;
		Cairo.Color fill2Color;
		Cairo.Color fill3Color;*/

		LazyImage btnNormal, btnPressed, btnInactive, btnHover;

		LazyImage iconRunNormal, iconRunDisabled;
		LazyImage iconStopNormal, iconStopDisabled;

		public RoundButton ()
		{
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.LeaveNotifyMask | EventMask.EnterNotifyMask;
			SetSizeRequest (height, height);

			btnNormal = new LazyImage ("btExecuteBase-Normal.png");
			btnInactive = new LazyImage ("btExecuteBase-Disabled.png");
			btnPressed = new LazyImage ("btExecuteBase-Pressed.png");
			btnHover = new LazyImage ("btExecuteBase-Hover.png");

			iconRunNormal = new LazyImage ("icoExecute-Normal.png");
			iconRunDisabled = new LazyImage ("icoExecute-Disabled.png");

			iconStopNormal = new LazyImage ("icoStop-Normal.png");
			iconStopDisabled = new LazyImage ("icoStop-Disabled.png");
		}

		void SetShape ()
		{
			var black = new Gdk.Color (0, 0, 0);
			black.Pixel = 1;

			var white = new Gdk.Color (255, 255, 255);
			white.Pixel = 0;

			using (var pm = new Pixmap (this.GdkWindow, height, height, 1)) {
				using (var gc = new Gdk.GC (pm)) {
					gc.Background = white;
					gc.Foreground = white;
					pm.DrawRectangle (gc, true, 0, 0, height, height);
		
					gc.Foreground = black;
					gc.Background = black;
					pm.DrawArc (gc, true, 0, 0, height, height, 0, 360 * 64);
		
					ShapeCombineMask (pm, 0, 0);
				}
			}
		}
		StateType leaveState = StateType.Normal;

		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			State = leaveState;
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			leaveState = State;
			State = StateType.Normal;
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button == 1)
				State = StateType.Selected;
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (State == StateType.Selected)
				OnClicked (EventArgs.Empty);
			State = StateType.Prelight;
			leaveState = StateType.Normal;
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			SetShape ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition.Width = btnNormal.Img.Width;
			requisition.Height = btnNormal.Img.Height + 2;
			base.OnSizeRequested (ref requisition);
		}


		ImageSurface GetBackground()
		{
			switch (State) {
				case StateType.Selected:
					return btnPressed;
				case StateType.Prelight:
					return btnHover;
				case StateType.Insensitive:
					return btnInactive;
				default:
					return btnNormal;
				}
		}

		ImageSurface GetIcon()
		{
			return iconRunNormal;
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				GetBackground ().Show (context, 0, 0);
				var icon = GetIcon();
				icon.Show (context, (icon.Width - Allocation.Width) / 2, (icon.Height - Allocation.Height) / 2);

/*				context.Arc (height / 2 + 0.5, height / 2 + 0.5, height / 2 - 1, 0, 2 * System.Math.PI);
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

				evnt.Window.DrawPixbuf (Style.WhiteGC, 
				                        pixbuf, 
				                        0, 0, 
				                        (Allocation.Width - pixbuf.Width) / 2, 
				                        (Allocation.Height - pixbuf.Height) / 2, 
				                        pixbuf.Width, pixbuf.Height,
				                        RgbDither.None, 0, 0);*/
			}
			return base.OnExposeEvent (evnt);
		}

		public event EventHandler Clicked;

		protected virtual void OnClicked (EventArgs e)
		{
			EventHandler handler = this.Clicked;
			if (handler != null)
				handler (this, e);
		}
	}
}

