//
// ThreadedRenderer.cs
//
// Author:
//       Jason Smith <jason@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using System.Threading;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.Ide
{
	public class ThreadedRenderer
	{
		Gtk.Widget owner;
		SurfaceWrapper surface;
		ManualResetEvent runningSignal;

		public ThreadedRenderer (Gtk.Widget owner)
		{
			this.owner = owner;
			runningSignal = new ManualResetEvent (true);
		}

		public void QueueThreadedDraw (Action<Cairo.Context> drawCallback)
		{
			if (!owner.IsRealized)
				return;

			// join last draw if still running to avoid having multiple draws running at once
			runningSignal.WaitOne ();

			if (surface == null || surface.Height != owner.Allocation.Height || surface.Width != owner.Allocation.Width) {
				using (var similar = Gdk.CairoHelper.Create (owner.GdkWindow)) {
					surface = new SurfaceWrapper (similar, owner.Allocation.Width, owner.Allocation.Height);
				}
			}

			runningSignal.Reset ();
			ThreadPool.QueueUserWorkItem (new WaitCallback (this.OnDraw), drawCallback);
			owner.QueueDraw ();
		}

		public bool Show (Cairo.Context context)
		{
			if (surface == null || surface.Width != owner.Allocation.Width || surface.Height != owner.Allocation.Height)
				return false;

			runningSignal.WaitOne ();
			surface.Surface.Show (context, owner.Allocation.X, owner.Allocation.Y);
			return true;
		}

		void OnDraw (object data)
		{
			Action<Cairo.Context> callback = (Action<Cairo.Context>) data;

			using (var context = new Cairo.Context (surface.Surface)) {
				context.Operator = Cairo.Operator.Source;
				context.Color = new Cairo.Color (0, 0, 0, 0);
				context.Paint ();
				context.Operator = Cairo.Operator.Over;

				callback (context);
			}
			runningSignal.Set ();
		}
	}
}

