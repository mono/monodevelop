// 
// MessageBubbleHighlightPopupWindow.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using Mono.TextEditor;
using Mono.TextEditor.Theatrics;

namespace MonoDevelop.SourceEditor
{
	public class MessageBubbleHighlightPopupWindow : Gtk.Window
	{
		MessageBubbleTextMarker marker;
		SourceEditorView view;
		
		public MessageBubbleHighlightPopupWindow (SourceEditorView view, MessageBubbleTextMarker marker)  : base (WindowType.Popup)
		{
			this.view = view;
			this.marker = marker;
			this.Decorated = false;
			this.BorderWidth = 0;
			this.HasFrame = true;
			this.TransientFor = IdeApp.Workbench.RootWindow;
		}
		
		Stage<MessageBubbleHighlightPopupWindow> stage = new Stage<MessageBubbleHighlightPopupWindow> ();

		public void Popup ()
		{
			stage.ActorStep += OnAnimationActorStep;
			stage.Iteration += OnAnimationIteration;
			stage.UpdateFrequency = 10;
			stage.Add (this, 200);
		}
		double Percent = 0.0;
		
		void OnAnimationIteration (object sender, EventArgs args)
		{
			Gdk.Rectangle currentBounds = marker.ErrorTextBounds;
			int i = (int) (24.0 * Percent);
			int j = (int) (4.0 * Percent);
			int x, y;
			view.SourceEditorWidget.TextEditor.GdkWindow.GetOrigin (out x, out y);
			Move (x + currentBounds.X - i, y + currentBounds.Y - j);
			Resize (currentBounds.Width + 2 * i, currentBounds.Height + 2 * j);
			if (first) {
				Show ();
				first = false;
			}
		}
		bool isComing = true;
		bool first = true;
		bool OnAnimationActorStep (Actor<MessageBubbleHighlightPopupWindow> actor)
		{
			if (isComing) {
				Percent = actor.Percent;
				if (actor.Expired) {
					isComing = false;
					actor.Reset ();
					return true;
				}
			} else {
				Percent = 1.0 - actor.Percent;
				if (actor.Expired) {
					Destroy ();
					return false;
				}
			}
			return true;
		}

		
		Gdk.Pixbuf textImage = null;
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			stage.Playing = false;
			
			if (textImage != null) {
				textImage.Dispose ();
				textImage = null;
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Gdk.Rectangle currentBounds = marker.ErrorTextBounds;
			if (textImage == null) {
				using (Gdk.Pixmap pixmap = new Gdk.Pixmap (evnt.Window, currentBounds.Width, currentBounds.Height)) {
					using (var bgGc = new Gdk.GC(pixmap)) {
						bgGc.RgbFgColor = CairoExtensions.CairoColorToGdkColor (marker.Errors[0].IsError ? marker.errorLightBg : marker.warningLightBg);
						pixmap.DrawRectangle (bgGc, true, 0, 0, currentBounds.Width, currentBounds.Height);
						
						pixmap.DrawPixbuf (marker.gc, 
						                marker.Errors.Any (e => e.IsError) ? marker.errorPixbuf : marker.warningPixbuf, 
						                0, 0, 0, 0,
						                marker.errorPixbuf.Width, marker.errorPixbuf.Height, 
						                Gdk.RgbDither.None, 0, 0);

						pixmap.DrawLayout (marker.gc, marker.errorPixbuf.Width + 2, (currentBounds.Height - marker.Layouts[0].Height) / 2, marker.Layouts[0].Layout);
					}
					textImage = Gdk.Pixbuf.FromDrawable (pixmap, Gdk.Colormap.System, 0, 0, 0, 0, currentBounds.Width, currentBounds.Height);
				}
			}
			try {
				using (var scaled = textImage.ScaleSimple (Allocation.Width, Allocation.Height, Gdk.InterpType.Bilinear)) {
					if (scaled != null) {
						using (var gc = new Gdk.GC (evnt.Window)) {
							scaled.RenderToDrawable (evnt.Window, gc, 0, 0, 0, 0, Allocation.Width, Allocation.Height, Gdk.RgbDither.None, 0, 0);
						}
					}
				}
			} catch (Exception e) {
				Console.WriteLine ("got exception in search result animation:" + e);
			}
			return false;
		}
	}
}

