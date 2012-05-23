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
using System.Linq;

namespace MonoDevelop.Compontents.MainToolbar
{
	public class ButtonBar : EventBox
	{
		List<LazyImage> buttons = new List<LazyImage> ();

		LazyImage btnLeftNormal, btnLeftPressed;
		LazyImage btnMidNormal, btnMidPressed;
		LazyImage btnRightNormal, btnRightPressed;

		int pushedButton = -1;

		public ButtonBar ()
		{
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			VisibleWindow = false;
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask;

			btnLeftNormal = new LazyImage ("btDebugBase-LeftCap-Normal.png");
			btnLeftPressed = new LazyImage ("btDebugBase-LeftCap-Pressed.png");

			btnMidNormal = new LazyImage ("btDebugBase-MidCap-Normal.png");
			btnMidPressed = new LazyImage ("btDebugBase-MidCap-Pressed.png");

			btnRightNormal = new LazyImage ("btDebugBase-RightCap-Normal.png");
			btnRightPressed = new LazyImage ("btDebugBase-RightCap-Pressed.png");
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
			if (evnt.Button == 1) {
				pushedButton = (int)(evnt.X / btnLeftNormal.Img.Width);
				State = StateType.Selected;
			}
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (State == StateType.Selected)
				OnClicked (new ClickEventArgs (pushedButton));
			State = StateType.Prelight;
			leaveState = StateType.Normal;
			pushedButton = -1;
			return base.OnButtonReleaseEvent (evnt);
		}

		public void Add (LazyImage pixbuf)
		{
			buttons.Add (pixbuf);
			SetSizeRequest (btnLeftNormal.Img.Width * buttons.Count, btnLeftNormal.Img.Height);
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				double x = Allocation.X, y = Allocation.Y;
				for (int i = 0; i < buttons.Count; i++) {
					ImageSurface img;
					if (i == 0) {
						img = State == StateType.Selected && pushedButton == i ? btnLeftPressed : btnLeftNormal;
					} else if (i == buttons.Count - 1) {
						img = State == StateType.Selected && pushedButton == i ? btnRightPressed : btnRightNormal;
					} else {
						img = State == StateType.Selected && pushedButton == i ? btnMidPressed : btnMidNormal;
					}
					img.Show (context, x, y);

					buttons[i].Img.Show (
						context,
						x + (img.Width - buttons[i].Img.Width) / 2,
						y + (img.Height - buttons[i].Img.Height) / 2
					);

					x += img.Width;
				}
			}
			return base.OnExposeEvent (evnt);
		}

		public sealed class ClickEventArgs : EventArgs
		{
			public int Button { get; private set; }

			public ClickEventArgs (int button)
			{
				this.Button = button;
			}
		}

		public event EventHandler<ClickEventArgs> Clicked;

		protected virtual void OnClicked (ClickEventArgs e)
		{
			var handler = this.Clicked;
			if (handler != null)
				handler (this, e);
		}

	}
}

