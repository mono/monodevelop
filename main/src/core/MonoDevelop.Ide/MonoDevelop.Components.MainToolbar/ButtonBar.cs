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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.Components.MainToolbar
{
	class ButtonBar : EventBox, ICommandBar
	{
		List<ButtonBarButton> buttons = new List<ButtonBarButton> ();
		ButtonBarButton[] visibleButtons;

		LazyImage[] btnNormal;
		LazyImage[] btnPressed;

		int pushedButton = -1;

		public ButtonBar ()
		{
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable;
			VisibleWindow = false;
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask;

			btnNormal = new LazyImage[] {
				new LazyImage ("btDebugBase-LeftCap-Normal.png"),
				new LazyImage ("btDebugBase-MidCap-Normal.png"),
				new LazyImage ("btDebugBase-RightCap-Normal.png")
			};

			btnPressed = new LazyImage[] {
				new LazyImage ("btDebugBase-LeftCap-Pressed.png"),
				new LazyImage ("btDebugBase-MidCap-Pressed.png"),
				new LazyImage ("btDebugBase-RightCap-Pressed.png")
			};
		}

		ButtonBarButton[] VisibleButtons {
			get {
				if (visibleButtons == null)
					visibleButtons = buttons.Where (b => b.Visible).ToArray ();
				return visibleButtons;
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
			if (evnt.Button == 1) {
				pushedButton = (int)(evnt.X / btnNormal[0].Img.Width);
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

		public void Clear ()
		{
			buttons.Clear ();
			visibleButtons = null;
			pushedButton = -1;
			QueueResize ();
		}

		public void Add (string commandId)
		{
			ButtonBarButton b = new ButtonBarButton (commandId);
			var ci = IdeApp.CommandService.GetCommandInfo (commandId);
			if (ci != null)
				UpdateButton (b, ci);
			buttons.Add (b);
			visibleButtons = null;
			QueueResize ();
		}

		public void AddSeparator ()
		{
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = btnNormal[0].Img.Width * VisibleButtons.Length;
			requisition.Height = btnNormal[0].Img.Height;
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				double x = Allocation.X, y = Allocation.Y;
				for (int i = 0; i < VisibleButtons.Length; i++) {
					ButtonBarButton button = VisibleButtons [i];
					LazyImage[] images = State == StateType.Selected && pushedButton == i ? btnPressed : btnNormal;
					ImageSurface img = images [i == 0 ? 0 : i == visibleButtons.Length - 1 ? 2 : 1];
					img.Show (context, x, y);

					var icon = ImageService.GetPixbuf (button.Image, IconSize.Menu);
					var iconCopy = icon;
					if (!Sensitive || !button.Enabled)
						iconCopy = ImageService.MakeTransparent (icon, 0.4);
					Gdk.CairoHelper.SetSourcePixbuf (context, iconCopy, x + (img.Width - icon.Width) / 2, y + (img.Height - icon.Height) / 2);
					context.Paint ();
					if (iconCopy != icon)
						iconCopy.Dispose ();
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

		#region ICommandBar implementation
		void ICommandBar.Update (object activeTarget)
		{
			foreach (var b in buttons) {
				var ci = IdeApp.CommandService.GetCommandInfo (b.CommandId, new CommandTargetRoute (activeTarget));
				UpdateButton (b, ci);
			}
		}

		void UpdateButton (ButtonBarButton b, CommandInfo ci)
		{
			if (ci.Icon != b.Image) {
				b.Image = ci.Icon;
				QueueDraw ();
			}
			if (ci.Visible != b.Visible) {
				b.Visible = ci.Visible;
				QueueResize ();
			}
			if (ci.Enabled != b.Enabled) {
				b.Enabled = ci.Enabled;
				QueueDraw ();
			}
		}

		void ICommandBar.SetEnabled (bool enabled)
		{
			Sensitive = enabled;
		}
		#endregion
	}

	class ButtonBarButton
	{
		public ButtonBarButton (string commandId)
		{
			this.CommandId = commandId;
		}

		public IconId Image { get; set; }

		public string CommandId { get; set; }

		internal bool Enabled { get; set; }

		internal bool Visible { get; set; }
	}
}

