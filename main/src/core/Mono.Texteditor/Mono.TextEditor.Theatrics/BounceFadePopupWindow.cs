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
		protected TextEditor editor;
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
			ExpandWidth = 12;
			ExpandHeight = 2;
			BounceEasing = Easing.Sine;
		}
		
		/// <summary>Duration of the animation, in milliseconds.</summary>
		public uint Duration { get; set; }
		
		/// <summary>The number of pixels by which the window's width will expand</summary>
		public uint ExpandWidth { get; set; }
		
		/// <summary>The number of pixels by which the window's height will expand</summary>
		public uint ExpandHeight { get; set; }

		/// <summary>The easing used for the bounce part of the animation.</summary>
		public Easing BounceEasing { get; set; }
		
		public void Popup ()
		{
			if (!IsComposited)
				throw new InvalidOperationException ("Only works with composited screen. Check Widget.IsComposited.");
			
			var rgbaColormap = Screen.RgbaColormap;
			if (rgbaColormap == null)
				return;
			Colormap = rgbaColormap;
			editor.GdkWindow.GetOrigin (out x, out y);
			x = x + bounds.X - (int)(ExpandWidth / 2);
			y = y + bounds.Y - (int)(ExpandHeight / 2);
			Move (x, y);
			Resize (bounds.Width + (int)ExpandWidth, bounds.Height + (int)ExpandHeight);
			
			stage.ActorStep += OnAnimationActorStep;
			stage.Iteration += OnAnimationIteration;
			
			stage.UpdateFrequency = 10;
			stage.Add (this, Duration);
			editor.VAdjustment.ValueChanged += HandleEditorVAdjustmentValueChanged;
			editor.HAdjustment.ValueChanged += HandleEditorHAdjustmentValueChanged;
			vValue = editor.VAdjustment.Value;
			hValue = editor.HAdjustment.Value;
			
			Show ();
		}
		int x, y;
		double vValue, hValue;
		
		void HandleEditorVAdjustmentValueChanged (object sender, EventArgs e)
		{
			y += (int)(vValue - editor.VAdjustment.Value);
			Move (x, y);
			vValue = editor.VAdjustment.Value;
		}
		
		void HandleEditorHAdjustmentValueChanged (object sender, EventArgs e)
		{
			x += (int)(hValue - editor.HAdjustment.Value);
			Move (x, y);
			hValue = editor.HAdjustment.Value;
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
			
			// for the first half, use an easing
			if (actor.Percent < 0.5) {
				scale = Choreographer.Compose (actor.Percent * 2, BounceEasing);
				opacity = 1.0;
			}
			//for the second half, vary opacity linearly from 1 to 0.
			else {
				scale = scale = Choreographer.Compose (1.0, BounceEasing);
				opacity = 2.0 - actor.Percent * 2;
			}
			return true;
		}
		
		protected override void OnDestroyed ()
		{
			editor.VAdjustment.ValueChanged -= HandleEditorVAdjustmentValueChanged;
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
				
				int i = (int)(ExpandWidth * scale);
				int j = (int)(ExpandHeight * scale);
				int winw = Allocation.Width, winh = Allocation.Height;
				int scaledw = winw - (int)(ExpandWidth - i);
				int scaledh = winh - (int)(ExpandHeight - j);
				
				using (var scaled = textImage.ScaleSimple (scaledw, scaledh, Gdk.InterpType.Bilinear)) {
					if (scaled != null) {
						SetPixbufChannel (scaled, 4, (byte)(opacity*255));
						using (var gc = new Gdk.GC (evnt.Window)) {
							scaled.RenderToDrawable (evnt.Window, gc, 0, 0, (winw - scaledw) / 2, (winh - scaledh) / 2, 
							                         scaledw, scaledh, Gdk.RgbDither.None, 0, 0);
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
	
	public abstract class CachedBounceFadePopupWindow : Gtk.Window
	{
		Rectangle bounds;
		protected TextEditor editor;
		Stage<CachedBounceFadePopupWindow> stage = new Stage<CachedBounceFadePopupWindow> ();
		Gdk.Pixbuf textImage = null;
		
		double scale = 0.0;
		double opacity = 1.0;
		
		public CachedBounceFadePopupWindow (TextEditor editor) : base (Gtk.WindowType.Popup)
		{
			this.Decorated = false;
			this.BorderWidth = 0;
			this.HasFrame = true;
			this.editor = editor;
			ExpandWidth = 12;
			ExpandHeight = 2;
			BounceEasing = Easing.Sine;
			Duration = 500;
			
			if (!IsComposited)
				throw new InvalidOperationException ("Only works with composited screen. Check Widget.IsComposited.");
			
			var rgbaColormap = Screen.RgbaColormap;
			if (rgbaColormap != null)
				Colormap = rgbaColormap;
			
			stage.ActorStep += OnAnimationActorStep;
			stage.Iteration += OnAnimationIteration;
			
			stage.UpdateFrequency = 10;
			
		}

		protected void Start (Rectangle bounds)
		{
			this.bounds = bounds;
			Show ();
			Popup ();
		}
		
		/// <summary>Duration of the animation, in milliseconds.</summary>
		public uint Duration { get; set; }
		
		/// <summary>The number of pixels by which the window's width will expand</summary>
		public uint ExpandWidth { get; set; }
		
		/// <summary>The number of pixels by which the window's height will expand</summary>
		public uint ExpandHeight { get; set; }

		/// <summary>The easing used for the bounce part of the animation.</summary>
		public Easing BounceEasing { get; set; }
		
		public void Popup ()
		{
			editor.GdkWindow.GetOrigin (out x, out y);
			x = x + bounds.X - (int)(ExpandWidth / 2);
			y = y + bounds.Y - (int)(ExpandHeight / 2);
			Move (x, y);
			Resize (bounds.Width + (int)ExpandWidth, bounds.Height + (int)ExpandHeight);
			
			editor.VAdjustment.ValueChanged += HandleEditorVAdjustmentValueChanged;
			editor.HAdjustment.ValueChanged += HandleEditorHAdjustmentValueChanged;
			vValue = editor.VAdjustment.Value;
			hValue = editor.HAdjustment.Value;
			
			stage.AddOrReset (this, Duration);
			stage.Play ();
			
		}
		
		protected override void OnHidden ()
		{
			base.OnHidden ();
			editor.VAdjustment.ValueChanged -= HandleEditorVAdjustmentValueChanged;
			editor.HAdjustment.ValueChanged -= HandleEditorHAdjustmentValueChanged;
		}
		
		int x, y;
		double vValue, hValue;
		
		void HandleEditorVAdjustmentValueChanged (object sender, EventArgs e)
		{
			y += (int)(vValue - editor.VAdjustment.Value);
			Move (x, y);
			vValue = editor.VAdjustment.Value;
		}
		
		void HandleEditorHAdjustmentValueChanged (object sender, EventArgs e)
		{
			x += (int)(hValue - editor.HAdjustment.Value);
			Move (x, y);
			hValue = editor.HAdjustment.Value;
		}
		
		void OnAnimationIteration (object sender, EventArgs args)
		{
			QueueDraw ();
		}
		
		bool OnAnimationActorStep (Actor<CachedBounceFadePopupWindow> actor)
		{
			if (actor.Expired) {
				StopPlaying ();
				Hide ();
				return false;
			}
			
			// for the first half, use an easing
			if (actor.Percent < 0.5) {
				scale = Choreographer.Compose (actor.Percent * 2, BounceEasing);
				opacity = 1.0;
			}
			//for the second half, vary opacity linearly from 1 to 0.
			else {
				scale = scale = Choreographer.Compose (1.0, BounceEasing);
				opacity = 2.0 - actor.Percent * 2;
			}
			return true;
		}
		
		protected override void OnDestroyed ()
		{
			editor.VAdjustment.ValueChanged -= HandleEditorVAdjustmentValueChanged;
			base.OnDestroyed ();
			StopPlaying ();
		}
		
		internal void StopPlaying ()
		{
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
				
				int i = (int)(ExpandWidth * scale);
				int j = (int)(ExpandHeight * scale);
				int winw = Allocation.Width, winh = Allocation.Height;
				int scaledw = winw - (int)(ExpandWidth - i);
				int scaledh = winh - (int)(ExpandHeight - j);
				
				using (var scaled = textImage.ScaleSimple (scaledw, scaledh, Gdk.InterpType.Bilinear)) {
					if (scaled != null) {
						SetPixbufChannel (scaled, 4, (byte)(opacity*255));
						using (var gc = new Gdk.GC (evnt.Window)) {
							scaled.RenderToDrawable (evnt.Window, gc, 0, 0, (winw - scaledw) / 2, (winh - scaledh) / 2, 
							                         scaledw, scaledh, Gdk.RgbDither.None, 0, 0);
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

