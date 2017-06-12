//
// ImageButton.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
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
using Gdk;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide;

namespace MonoDevelop.Components
{
	public class ImageButton: Gtk.EventBox
	{
		Xwt.Drawing.Image image;
		Xwt.Drawing.Image inactiveImage;
		ImageView imageWidget;
		bool hasInactiveImage;
		bool hover;
		bool pressed;

		public ImageButton ()
		{
			var actionHandler = new ActionDelegate (this);
			actionHandler.PerformPress += HandlePress;

			Accessible.Role = Atk.Role.PushButton;

			Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask | Gdk.EventMask.ButtonReleaseMask;
			VisibleWindow = false;
			imageWidget = new ImageView ();
			imageWidget.Accessible.SetShouldIgnore (true);
			imageWidget.Show ();
			Add (imageWidget);
		}

		public Xwt.Drawing.Image Image {
			get { return image; }
			set {
				image = value;
				Xwt.Drawing.Image oldInactive = null;
				if (!hasInactiveImage) {
					oldInactive = inactiveImage;
					inactiveImage = image != null ? image.WithAlpha (0.5) : null;
				}
				LoadImage ();
				if (oldInactive != null)
					oldInactive.Dispose ();
			}
		}

		public Xwt.Drawing.Image InactiveImage {
			get { return hasInactiveImage ? inactiveImage : null; }
			set {
				if (!hasInactiveImage && inactiveImage != null)
					inactiveImage.Dispose ();
				hasInactiveImage = true;
				inactiveImage = value;
				LoadImage ();
			}
		}

		protected override void OnDestroyed ()
		{
			if (!hasInactiveImage && inactiveImage != null)
				inactiveImage.Dispose ();
			base.OnDestroyed ();
		}

		void LoadImage ()
		{
			if (image != null) {
				if (hover)
					imageWidget.Image = image;
				else
					imageWidget.Image = inactiveImage;
			} else {
				imageWidget.Image = null;
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (HasFocus && image != null) {
				Gtk.Style.PaintFocus (Style, GdkWindow, State, Allocation, this, "button",
				                      Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
			}

			return base.OnExposeEvent (evnt);
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			hover = true;
			LoadImage ();
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			hover = false;
			LoadImage ();
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			pressed = image != null;
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (pressed && evnt.Button == 1 && new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height).Contains ((int)evnt.X, (int)evnt.Y)) {
				hover = false;
				LoadImage ();
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
				return true;
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			pressed = image != null;
			return base.OnKeyPressEvent (evnt);
		}

		protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
		{
			if (pressed && evnt.Key == Gdk.Key.space) {
				LoadImage ();
				Clicked?.Invoke (this, EventArgs.Empty);
				return true;
			}

			return base.OnKeyReleaseEvent (evnt);
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			hover = true;
			LoadImage ();
			QueueDraw ();
			return base.OnFocusInEvent (evnt);
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			hover = false;
			LoadImage ();
			QueueDraw ();
			return base.OnFocusOutEvent (evnt);
		}

		void HandlePress (object o, EventArgs args)
		{
			Clicked?.Invoke (this, EventArgs.Empty);
		}

		public event EventHandler Clicked;
	}
}

