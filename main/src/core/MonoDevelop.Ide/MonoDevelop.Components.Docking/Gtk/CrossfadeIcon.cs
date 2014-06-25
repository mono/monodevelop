//
// CrossfadeIcon.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using Xwt.Motion;
using Animations = Xwt.Motion.AnimationExtensions;

namespace MonoDevelop.Components.Docking
{	
	class CrossfadeIcon: Gtk.Image, IAnimatable
	{
		// This class should be subclassed from Gtk.Misc, but there is no reasonable way to do that due to there being no bindings to gtk_widget_set_has_window

		Xwt.Drawing.Image primary, secondary;

		double secondaryOpacity;

		public CrossfadeIcon (Xwt.Drawing.Image primary, Xwt.Drawing.Image secondary)
		{
			if (primary == null)
				throw new ArgumentNullException ("primary");
			if (secondary == null)
				throw new ArgumentNullException ("secondary");

			this.primary = primary;
			this.secondary = secondary;
		}

		void IAnimatable.BatchBegin () { }
		void IAnimatable.BatchCommit () { QueueDraw (); }

		public void ShowPrimary ()
		{
			AnimateCrossfade (false);
		}

		public void ShowSecondary ()
		{
			AnimateCrossfade (true);
		}

		void AnimateCrossfade (bool toSecondary)
		{
			this.Animate ("CrossfadeIconSwap",
			              x => secondaryOpacity = x,
			              secondaryOpacity,
			              toSecondary ? 1.0f : 0.0f);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);

			requisition.Width = (int) primary.Width;
			requisition.Height = (int) primary.Height;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (Cairo.Context context = Gdk.CairoHelper.Create (evnt.Window)) {
				if (secondaryOpacity < 1.0f)
					RenderIcon (context, primary, 1.0f - (float)Math.Pow (secondaryOpacity, 3.0f));

				if (secondaryOpacity > 0.0f)
					RenderIcon (context, secondary, secondaryOpacity);
			}

			return false;
		}

		void RenderIcon (Cairo.Context context, Xwt.Drawing.Image surface, double opacity)
		{
			context.DrawImage (this, surface.WithAlpha (opacity),
			                          Allocation.X + (Allocation.Width - surface.Width) / 2,
			                          Allocation.Y + (Allocation.Height - surface.Height) / 2);
		}
	}
	
}
