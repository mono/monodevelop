// 
// BounceFadePopupWindow.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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

namespace Mono.TextEditor.Theatrics
{
	/// <summary>
	/// Tooltip that "bounces", then fades away.
	/// </summary>
	public abstract class BounceFadePopupWindow : Gtk.Window
	{
		Rectangle bounds;
		TextEditor editor;
		Stage<BounceFadePopupWindow> stage = new Stage<BounceFadePopupWindow> ();
		Gdk.Pixbuf textImage = null;
		
		double scale = 0.0;
		double opacity = 1.0;
		
		public BounceFadePopupWindow (TextEditor editor, Rectangle bounds) : base (Gtk.WindowType.Popup)
		{
			this.Decorated = false;
			this.BorderWidth = 0;
			this.HasFrame = true;
			this.editor = editor;
			this.bounds = bounds;
			this.Duration = 500;
			ExpandWidth = 8;
			ExpandHeight = 2;
		}
		
		/// <summary>Duration of the animation, in milliseconds.</summary>
		public uint Duration { get; set; }
		
		/// <summary>The number of pixels by which the window's width will expand</summary>
		public uint ExpandWidth { get; set; }
		
		/// <summary>The number of pixels by which the window's height will expand</summary>
		public uint ExpandHeight { get; set; }

		public void Popup ()
		{
			if (!IsComposited)
				throw new InvalidOperationException ("Only works with composited screen. Check Widget.IsComposited.");
			
			var rgbaColormap = Screen.RgbaColormap;
			if (rgbaColormap == null)
				return;
			Colormap = rgbaColormap;
			
			int x, y;
			editor.GdkWindow.GetOrigin (out x, out y);
			Move (x + bounds.X - (int)ExpandWidth, y + bounds.Y - (int)ExpandHeight);
			Resize (bounds.Width + (int)ExpandWidth * 2, bounds.Height + (int)ExpandHeight * 2);
			
			stage.ActorStep += OnAnimationActorStep;
			stage.Iteration += OnAnimationIteration;
			
			stage.UpdateFrequency = 10;
			stage.Add (this, Duration);
			
			Show ();
		}
		
		
		void OnAnimationIteration (object sender, EventArgs args)
		{
			QueueDraw ();
		}
		
		bool OnAnimationActorStep (Actor<BounceFadePopupWindow> actor)
		{
			if (actor.Expired) {
				Destroy ();
				return false;
			}
			
			// for the first half, vary scale in a simple sin curve from 0 to Pi
			if (actor.Percent < 0.5) {
				scale = System.Math.Sin (actor.Percent * 2 * System.Math.PI);
			}
			//for the second half, vary opacity linearly from 1 to 0.
			else {
				scale = 0;
				opacity = 2.0 - actor.Percent * 2;
			}
			return true;
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			stage.Playing = false;
			
			if (textImage != null) {
				textImage.Dispose ();
				textImage = null;
			}
		}
		
		protected abstract Pixbuf RenderInitialPixbuf (Gdk.Window parentwindow, Rectangle bounds);
		
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			try {
				using (var g = Gdk.CairoHelper.Create (evnt.Window)) {
					g.SetSourceRGBA (1, 1, 1, 0);
					g.Operator = Cairo.Operator.Source;
					g.Paint ();
				}
				
				if (textImage == null) {
					var img = RenderInitialPixbuf (evnt.Window, bounds);
					if (!img.HasAlpha) {
						textImage = img.AddAlpha (false, 0, 0, 0);
						img.Dispose ();
					} else {
						textImage = img;
					}
				}
				
				int i = (int)(12.0 * scale);
				int j = (int)(2.0 * scale);
				using (var scaled = textImage.ScaleSimple (Allocation.Width - (12 - i), Allocation.Height - (2 - j), Gdk.InterpType.Bilinear)) {
					if (scaled != null) {
						SetPixbufChannel (scaled, 4, (byte)(opacity*255));
						using (var gc = new Gdk.GC (evnt.Window)) {
							scaled.RenderToDrawable (evnt.Window, gc, 0, 0, (Allocation.Width - scaled.Width) / 2, (Allocation.Height - scaled.Height) / 2, scaled.Width, scaled.Height, Gdk.RgbDither.None, 0, 0);
						}
					}
				}
			} catch (Exception e) {
				Console.WriteLine ("Exception in animation:" + e);
			}
			return false;
		}
		
		/// <summary>
		/// Utility method for setting a single channel in a pixbuf.
		/// </summary>
		/// <param name="channel">Channel indexx, 1-based.</param>
		/// <param name="value">Value for all pixels in that channel.</param>
		unsafe void SetPixbufChannel (Gdk.Pixbuf p, int channel, byte value)
		{
			int nChannels = p.NChannels;
			
			if (channel > nChannels)
				throw new ArgumentException ("channel");
			
			byte *start = (byte*) p.Pixels;
			int rowStride = p.Rowstride;
			int width = p.Width;
			byte *lastRow = start + p.Rowstride * (p.Height - 1);
			for (byte *row = start; row <= lastRow; row += rowStride) {
				byte *colEnd = row + width * nChannels;
				for (byte *col = row + channel - 1; col < colEnd; col += nChannels)
					*col = value;
			}
		}
	}
}

