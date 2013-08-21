//
// ProgressControl.cs
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
using Cairo;
using System.Collections.Generic;

namespace MonoDevelop.Components
{
	/// <summary>
	/// A mac os x like circle progress control.
	/// </summary>
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ProgressControl : Gtk.DrawingArea
	{
		int tick;
		List<Petal> blossom;
		
		struct Petal
		{
			public readonly PointD Start;
			public readonly PointD End;
			
			public Petal (PointD start, PointD end)
			{
				Start = start;
				End = end;
			}
			
			const int petalCount = 12;
			
			internal static List<Petal> CalculateBlossom (int width, int height)
			{
				var result = new List<Petal> ();
				var outerRadius = Math.Min (width, height) / 2;
				var innerRadius = outerRadius / 2.2;
				var centerX = width / 2;
				var centerY = height / 2;
				var angleBetweenPetals = 2 * Math.PI / petalCount;
				for (double angle = 0; angle < 2 * Math.PI; angle += angleBetweenPetals) {
					var aCos = Math.Cos (angle);
					var aSin = Math.Sin (angle);
					result.Add (new Petal (new PointD (centerX + innerRadius * aCos, centerY + innerRadius * aSin),
					                       new PointD (centerX + outerRadius * aCos, centerY + outerRadius * aSin)));
				}
				return result;
			}
		}
		
		public ProgressControl ()
		{
			int width = 24;
			int height = 24;
			SetSizeRequest (width, height);
		}
		
		void Pulse ()
		{
			tick++;
			QueueDraw ();
		}
		
		uint autoPulseTimer;
		
		/// <summary>
		/// Starts the auto pulse. This is not done automatically.
		/// </summary>
		public void StartAutoPulse ()
		{
			if (autoPulseTimer != 0)
				return;
			autoPulseTimer = GLib.Timeout.Add (82, delegate {
				Pulse ();
				return true;
			});
		}
		
		/// <summary>
		/// Stops the auto pulse. The pulse timer is automatically stopped on destroy.
		/// </summary>
		public void StopAutoPulse ()
		{
			if (autoPulseTimer == 0)
				return;
			GLib.Source.Remove (autoPulseTimer);
			autoPulseTimer = 0;
		}
		
		protected override void OnDestroyed ()
		{
			StopAutoPulse ();
			base.OnDestroyed ();
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			blossom = Petal.CalculateBlossom (allocation.Width, allocation.Height);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				int alpha = blossom.Count + tick;
				for (int i = 0; i < blossom.Count; i++) {
					var petal = blossom[i];
					context.SetSourceRGBA (0, 0, 0, 0.1 + (alpha % blossom.Count) / (double)blossom.Count);
					context.MoveTo (petal.Start);
					context.LineTo (petal.End);
					context.Stroke ();
					alpha --;
				}
			}
			return base.OnExposeEvent (evnt);
		}
	}
}