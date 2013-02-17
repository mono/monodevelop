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
	public class MessageBubbleHighlightPopupWindow : BounceFadePopupWindow
	{
		MessageBubbleTextMarker marker;
//		new Gdk.Rectangle bounds;
		
		public MessageBubbleHighlightPopupWindow (SourceEditorView view, MessageBubbleTextMarker marker)
			: base (view.TextEditor)
		{
			this.marker = marker;
			
			ExpandWidth = 36;
			ExpandHeight = 2;
			BounceEasing = Mono.TextEditor.Theatrics.Easing.Sine;
			Duration = 150;
		}
	
		protected override Gdk.Rectangle CalculateInitialBounds ()
		{
			var bounds = marker.ErrorTextBounds;
			int spaceX = bounds.Width / 2;
			return new Gdk.Rectangle (bounds.X - spaceX,
				(int)(bounds.Y - Editor.LineHeight),
				bounds.Width + spaceX * 2,
				(int)(bounds.Height + Editor.LineHeight * 2));
		}
		
		protected override void OnAnimationCompleted ()
		{
			Destroy ();
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.SetSourceRGBA (1, 1, 1, 0);
				cr.Operator = Cairo.Operator.Source; 
				cr.Paint ();
			}
			
			var bounds = marker.ErrorTextBounds;
			using (var cr = Gdk.CairoHelper.Create (evnt.Window)) {
				cr.Translate (width / 2, height / 2);
				cr.Scale (1 + scale / 8, 1 + scale / 8);
				int x = -(bounds.Width) / 2;
				int y = -bounds.Height / 2;
				
				if (marker.FitsInSameLine) {
					cr.MoveTo (x + Editor.LineHeight / 2, y);
					cr.LineTo (x, 0);
					cr.LineTo (x + Editor.LineHeight / 2, bounds.Height / 2);
				} else {
					cr.MoveTo (x, -bounds.Height / 2);
					cr.LineTo (x, bounds.Height / 2);
				}
				cr.LineTo (x + bounds.Width, bounds.Height / 2);
				cr.LineTo (x + bounds.Width, y);
				cr.ClosePath ();
				
				Mono.TextEditor.HslColor hsl = marker.colorMatrix [0, 0, 0, 0, 0];
				double delta = 1 + 2 - scale;
				hsl.S += delta;
				var color = (Cairo.Color)hsl;
				color.A = opacity;
				cr.Color = color;
				cr.FillPreserve ();
				color = marker.colorMatrix [0, 0, 2, 0, 0];
				color.A = opacity;
				cr.Color = color;
				cr.Stroke ();
				int errorCounterWidth = 0;
				
				marker.EnsureLayoutCreated (base.Editor);
				
				if (marker.Errors.Count > 1) {
					double rY = y + Editor.LineHeight / 6;
					int ew, eh;
					marker.errorCountLayout.GetPixelSize (out ew, out eh);
					errorCounterWidth = ew + 10;
					int rX = x + bounds.Width - errorCounterWidth;

					int rW = errorCounterWidth - 2;
					double rH = Editor.LineHeight * 3 / 4;
					
					BookmarkMarker.DrawRoundRectangle (cr, rX, rY, 8, rW, rH);
					cr.Color = new Cairo.Color (0.5, 0.5, 0.5);
					cr.Fill ();
					
					cr.MoveTo (rX + rW / 2 - rW / 4, rY + rH - rH / 4);
					cr.LineTo (rX + rW / 2 + rW / 4, rY + rH - rH / 4);
					cr.LineTo (rX + rW / 2, rY + rH / 4);
					cr.ClosePath ();
					
					cr.Color = new Cairo.Color (1, 1, 1);
					cr.Fill ();
				}
				
				cr.Color = Editor.ColorStyle.MessageBubbleError.GetColor ("color");
				
				int layoutWidth, layoutHeight;
				marker.Layouts [0].Layout.GetPixelSize (out layoutWidth, out layoutHeight);
				double ly;
				if (marker.CollapseExtendedErrors || marker.Errors.Count == 1) {
					ly = 1 + y + (bounds.Height - layoutHeight) / 2;
					double x2 = x + MessageBubbleTextMarker.border;
					if (marker.FitsInSameLine)
						x2 += 1 + Editor.LineHeight / 2;
					cr.Translate (x2, ly);
					cr.ShowLayout (marker.Layouts [0].Layout);
				
				} else {
					ly = 1 + y + (Editor.LineHeight - layoutHeight) / 2;
					for (int i = 0; i < marker.Errors.Count; i++) {
						marker.Layouts [i].Layout.GetPixelSize (out layoutWidth, out layoutHeight);
						cr.Save ();
						double x2;
						if (i == 0) {
							x2 = x + bounds.Width - layoutWidth - errorCounterWidth;
						} else {
							x2 = x + MessageBubbleTextMarker.border;
						}
						if (marker.FitsInSameLine)
							x2 += Editor.LineHeight / 2;
						cr.Translate (x2, ly);
						cr.ShowLayout (marker.Layouts [i].Layout);
						cr.Restore ();
						ly += Editor.LineHeight;
					}
				}
				
				
			}
			return false;
		}
	}
}
