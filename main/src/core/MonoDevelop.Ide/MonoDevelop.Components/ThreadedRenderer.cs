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
	public class ThreadedRenderer : IDisposable
	{
		Gtk.Widget owner;
		SurfaceWrapper surface;
		ManualResetEventSlim runningSignal;

		static ThreadedRenderer ()
		{
			// Initialize enough threads for all renderers at once so they dont all stutter at the start
			ThreadPool.SetMinThreads (20, 20);
		}

		public ThreadedRenderer (Gtk.Widget owner)
		{
			this.owner = owner;
			runningSignal = new ManualResetEventSlim (true);
		}

		public void Dispose ()
		{
			if (surface != null)
				surface.Dispose ();
		}

		double Scale {
			get;
			set;
		}

		int TargetWidth {
			get { return (int)(owner.Allocation.Width * Scale); }
		}

		int TargetHeight {
			get { return (int)(owner.Allocation.Height * Scale); }
		}

		void UpdateScale ()
		{
			if (MonoDevelop.Core.Platform.IsMac) {
				using (var similar = Gdk.CairoHelper.Create (owner.GdkWindow)) {
					Scale = QuartzSurface.GetRetinaScale (similar);
				}
			} else {
				Scale = 1;
			}
		}

		public void QueueThreadedDraw (Action<Cairo.Context> drawCallback)
		{
			if (!owner.IsRealized)
				return;

			runningSignal.Wait ();

			UpdateScale ();

			if (surface == null || surface.Height != TargetHeight || surface.Width != TargetWidth) {
				using (var similar = Gdk.CairoHelper.Create (owner.GdkWindow)) {
					if (surface != null)
						surface.Dispose ();
					surface = new SurfaceWrapper (similar, TargetWidth, TargetHeight);
				}
			}
			runningSignal.Reset ();
			this.OnDraw (drawCallback);
			//ThreadPool.QueueUserWorkItem (new WaitCallback (this.OnDraw), drawCallback);
			owner.QueueDraw ();
		}

		public bool Show (Cairo.Context context)
		{
			UpdateScale ();
			if (surface == null || surface.Width != TargetWidth || surface.Height != TargetHeight)
				return false;

			runningSignal.Wait ();
			context.Scale (1 / Scale, 1 / Scale);
			surface.Surface.Show (context, owner.Allocation.X * Scale, owner.Allocation.Y * Scale);
			context.Scale (Scale, Scale);
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

				context.Scale (Scale, Scale);
				callback (context);
			}
			runningSignal.Set ();
		}
	}
}

