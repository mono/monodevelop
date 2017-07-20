//
// EventBoxTooltip.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using Gtk;
using System;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide;
using Gdk;

namespace MonoDevelop.Components
{
	public class EventBoxTooltip : IDisposable
	{
		EventBox eventBox;
		string tip;
		TooltipPopoverWindow tooltipWindow;
		bool mouseOver;

		public Atk.Object Accessible {
			get {
				return eventBox.Accessible;
			}
		}

		/// <summary>
		/// The EventBox should have Visible set to false otherwise the tooltip pop window
		/// will have the wrong location.
		/// </summary>

		ImageView image;
		Pixbuf normalPixbuf;
		Pixbuf activePixbuf;

		public EventBoxTooltip (EventBox eventBox)
		{
			this.eventBox = eventBox;
			eventBox.CanFocus = true;

			eventBox.EnterNotifyEvent += HandleEnterNotifyEvent;
			eventBox.LeaveNotifyEvent += HandleLeaveNotifyEvent;
			eventBox.FocusInEvent += HandleFocusInEvent;
			eventBox.FocusOutEvent += HandleFocusOutEvent;

			image = eventBox.Child as ImageView;

			if (image != null) {
				normalPixbuf = image.Image.ToPixbuf ();
			}
			normalPixbuf = image.Image.ToPixbuf ();
			activePixbuf = normalPixbuf.ColorShiftPixbuf ();

			eventBox.FocusGrabbed += (sender, e) => {
				if (image != null)
					image.Image = activePixbuf.ToXwtImage ();
			};

			eventBox.Focused += (o, args) => {
				if (image != null)
					image.Image = normalPixbuf.ToXwtImage ();
			};

			Position = PopupPosition.TopLeft;

			// Accessibility: Disguise this eventbox as a label
			eventBox.Accessible.SetRole (AtkCocoa.Roles.AXStaticText);
			eventBox.CanFocus = true;
		}

		[GLib.ConnectBefore]
		void HandleLeaveNotifyEvent (object sender, EventArgs e)
		{
			mouseOver = false;
			HideTooltip ();
		}

		[GLib.ConnectBefore]
		void HandleEnterNotifyEvent (object sender, EventArgs e)
		{
			mouseOver = true;
			ShowTooltip ();
		}

		[GLib.ConnectBefore]
		void HandleFocusOutEvent (object sender, EventArgs e)
		{
			mouseOver = false;
			HideTooltip ();
		}

		[GLib.ConnectBefore]
		void HandleFocusInEvent (object sender, EventArgs e)
		{
			mouseOver = true;
			ShowTooltip ();
		}

		void UpdateAccessibility ()
		{
			eventBox.Accessible.SetLabel (tip);
		}

		bool ShowTooltip ()
		{
			if (!string.IsNullOrEmpty (tip)) {
				HideTooltip ();
				tooltipWindow = TooltipPopoverWindow.Create ();
				tooltipWindow.ShowArrow = true;
				tooltipWindow.Text = tip;
				tooltipWindow.Severity = Severity;
				var rect = new Gdk.Rectangle (0, 0, eventBox.Allocation.Width, eventBox.Allocation.Height + 5);
				tooltipWindow.ShowPopup (eventBox, rect, Position);
			}
			return false;
		}

		void HideTooltip ()
		{
			if (tooltipWindow != null) {
				tooltipWindow.Destroy ();
				tooltipWindow = null;
			}
		}

		public void Dispose ()
		{
			HideTooltip ();
			eventBox.EnterNotifyEvent -= HandleEnterNotifyEvent;
			eventBox.LeaveNotifyEvent -= HandleLeaveNotifyEvent;
			eventBox.FocusInEvent -= HandleFocusInEvent;
			eventBox.FocusOutEvent -= HandleFocusOutEvent;
		}

		public string ToolTip {
			get { return tip; }
			set {
				tip = value;
				if (tooltipWindow != null) {
					if (!string.IsNullOrEmpty (tip))
						tooltipWindow.Text = value;
					else
						HideTooltip ();
				} else if (!string.IsNullOrEmpty (tip) && mouseOver)
					ShowTooltip ();
				UpdateAccessibility ();
			}
		}

		public TaskSeverity? Severity { get; set; }
		public PopupPosition Position { get; set; }
	}
#region Extension method for Pixbuf
	internal static class PixbufExtension
	{
		public static unsafe Pixbuf ColorShiftPixbuf (this Pixbuf src, byte shift = 120)
		{
			var dest = new Gdk.Pixbuf (src.Colorspace, src.HasAlpha, src.BitsPerSample, src.Width, src.Height);

			byte* src_pixels_orig = (byte*)src.Pixels;
			byte* dest_pixels_orig = (byte*)dest.Pixels;

			for (int i = 0; i < src.Height; i++) {
				byte* src_pixels = src_pixels_orig + i * src.Rowstride;
				byte* dest_pixels = dest_pixels_orig + i * dest.Rowstride;

				for (int j = 0; j < src.Width; j++) {
					*(dest_pixels++) = PixelClamp (*(src_pixels++) + shift);
					*(dest_pixels++) = PixelClamp (*(src_pixels++) + shift);
					*(dest_pixels++) = PixelClamp (*(src_pixels++) + shift);

					if (src.HasAlpha) {
						*(dest_pixels++) = *(src_pixels++);
					}
				}
			}
			return dest;
		}

		static byte PixelClamp (int val)
		{
			return (byte)System.Math.Max (0, System.Math.Min (255, val));
		}
	}
#endregion
}

