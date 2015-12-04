//
// ButtonBar.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using AppKit;
using Foundation;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.MacIntegration
{
	[Register]
	class ButtonBar : NSSegmentedControl
	{
		readonly Dictionary<IButtonBarButton, int> indexMap = new Dictionary<IButtonBarButton, int> ();
		readonly IReadOnlyList<IButtonBarButton> buttons;

		public ButtonBar (IEnumerable<IButtonBarButton> buttons)
		{
			this.buttons = buttons.ToList ();

			foreach (var button in buttons) {
				var _button = button;
				button.ImageChanged += (o, e) => {
					if (!indexMap.ContainsKey (_button))
						return;
					SetImage (ImageService.GetIcon (_button.Image, Gtk.IconSize.Menu).ToNSImage (), indexMap [_button]);
					SetNeedsDisplay ();
				};
				button.EnabledChanged += (o, e) => {
					if (!indexMap.ContainsKey (_button))
						return;
					SetEnabled (_button.Enabled, indexMap [_button]);
					SetNeedsDisplay ();
				};
				button.VisibleChanged += (o, e) => RebuildSegments ();
				button.TooltipChanged += (o, e) => {
					if (!indexMap.ContainsKey (_button))
						return;
					Cell.SetToolTip (_button.Tooltip, indexMap [_button]);
				};
			}
			Activated += (sender, e) => indexMap.First (b => b.Value == SelectedSegment).Key.NotifyPushed ();

			RebuildSegments ();
			SegmentStyle = NSSegmentStyle.TexturedRounded;
			Cell.TrackingMode = NSSegmentSwitchTracking.Momentary;
		}

		public override nint SegmentCount {
			get { return base.SegmentCount; }
			set {
				base.SegmentCount = value;
				if (updating)
					return;

				if (ResizeRequested != null)
					ResizeRequested (this, null);
			}
		}

		bool updating;
		void RebuildSegments ()
		{
			updating = true;
			SegmentCount = buttons.Count;

			int j = 0;
			foreach (var button in buttons) {
				if (!button.Visible) {
					indexMap.Remove (button);
					continue;
				}
				if (!indexMap.ContainsKey (button) || indexMap [button] != j)
					UpdateButton (button, indexMap [button] = j);
				++j;
			}

			updating = false;
			SegmentCount = j;
		}

		void UpdateButton (IButtonBarButton button, int idx)
		{
			var img = ImageService.GetIcon (button.Image, Gtk.IconSize.Menu);
			if (img.ToNSImage () != GetImage (idx)) {
				SetImage (ImageService.GetIcon (button.Image, Gtk.IconSize.Menu).ToNSImage (), idx);
				SetNeedsDisplay ();
			}
			if (button.Enabled != IsEnabled (idx)) {
				SetEnabled (button.Enabled, idx);
				SetNeedsDisplay ();
			}
			if (button.Tooltip != Cell.GetToolTip (idx))
				Cell.SetToolTip (button.Tooltip, idx);
		}

		public event EventHandler ResizeRequested;
	}
}
