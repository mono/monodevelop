//
// TooltipPopoverWindow.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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

namespace MonoDevelop.Components
{
	public class TooltipPopoverWindow: PopoverWindow
	{
		Gtk.Label label;

		public TooltipPopoverWindow ()
		{
			Theme.SetFlatColor (new Cairo.Color (1d, 243d / 255d, 207d / 255d, 0.9d));
			Theme.BorderColor = new Cairo.Color (128d/255d, 122d / 255d, 104d / 255d);
			ShowArrow = true;
		}

		public string Text {
			get {
				return label != null ? label.Text : string.Empty;
			}
			set {
				AddLabel ();
				label.Text = value;
				AdjustSize ();
			}
		}

		public string Markup {
			get {
				return label != null ? label.Text : string.Empty;
			}
			set {
				AddLabel ();
				label.Markup = value;
				AdjustSize ();
			}
		}

		void AddLabel ()
		{
			if (label == null) {
				Gtk.Alignment al = new Gtk.Alignment (0.5f, 0.5f, 1f, 1f);
				al.SetPadding (6, 6, 6, 6);
				label = new Gtk.Label ();
				al.Add (label);
				ContentBox.Add (al);
				al.ShowAll ();
			}
		}

		void AdjustSize ()
		{
			if (label.SizeRequest ().Width > 300) {
				label.Wrap = true;
				label.WidthRequest = 300;
			} else {
				label.Wrap = false;
				label.WidthRequest = -1;
			}
			RepositionWindow ();
		}
	}
}

