//
// MDSpinner.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.Components
{
	public class MDSpinner : Widget
	{
		Xwt.ImageView imageView;
		AnimatedIcon spinner;
		IDisposable animation;
		Image idleImage;

		public Image IdleImage {
			get {
				return idleImage;
			}
			set {
				idleImage = value;
				if (!animate)
					imageView.Image = value ?? spinner.FirstFrame;
			}
		}

		public MDSpinner () : this (Gtk.IconSize.Menu, null)
		{
		}

		public MDSpinner (Image idleImage) : this (Gtk.IconSize.Menu, idleImage)
		{
		}

		public MDSpinner (Gtk.IconSize size, Image idleImage = null)
		{
			Content = imageView = new Xwt.ImageView ();
			if (size == Gtk.IconSize.Menu)
				spinner = ImageService.GetAnimatedIcon ("md-spinner-16", size);
			else
				spinner = ImageService.GetAnimatedIcon ("md-spinner-18", size);
			IdleImage = idleImage;
		}

		bool animate;
		public bool Animate {
			get {
				return animate;
			}
			set {
				if (animate != value) {
					animate = value;
					if (animation != null) {
						animation.Dispose ();
						animation = null;
					}
					if (value)
						animation = spinner.StartAnimation ((obj) => imageView.Image = obj);
					else
						imageView.Image = IdleImage ?? spinner.FirstFrame;
				}
			}
		}
	}
}
