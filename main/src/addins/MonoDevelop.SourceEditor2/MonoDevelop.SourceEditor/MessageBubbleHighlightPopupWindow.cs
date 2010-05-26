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
		
		public MessageBubbleHighlightPopupWindow (SourceEditorView view, MessageBubbleTextMarker marker)
			: base (view.TextEditor, marker.ErrorTextBounds)
		{
			this.marker = marker;
		}
		
		protected override Gdk.Pixbuf RenderInitialPixbuf (Gdk.Window parentwindow, Gdk.Rectangle bounds)
		{
			using (Gdk.Pixmap pixmap = new Gdk.Pixmap (parentwindow, bounds.Width, bounds.Height)) {
				using (var bgGc = new Gdk.GC(pixmap)) {
					bgGc.RgbFgColor = CairoExtensions.CairoColorToGdkColor (marker.colorMatrix[0, 0, 0, 0, 0]);
					pixmap.DrawRectangle (bgGc, true, 0, 0, bounds.Width, bounds.Height);
					
/*						pixmap.DrawPixbuf (marker.gc, 
					                marker.Errors.Any (e => e.IsError) ? marker.errorPixbuf : marker.warningPixbuf, 
					                0, 0, 0, (currentBounds.Height - marker.errorPixbuf.Height) / 2,
					                marker.errorPixbuf.Width, marker.errorPixbuf.Height, 
					                Gdk.RgbDither.None, 0, 0);
					 */
					pixmap.DrawLayout (marker.gc, /*marker.errorPixbuf.Width +*/ 0, (bounds.Height - marker.Layouts[0].Height) / 2, marker.Layouts[0].Layout);
				}
				return Gdk.Pixbuf.FromDrawable (pixmap, Colormap, 0, 0, 0, 0, bounds.Width, bounds.Height);
			}
		}
	}
}

/* doesn't work because of a cairo quartz bug cairo-font-face.c:191: failed assertion `CAIRO_REFERENCE_COUNT_HAS_REFERENCE (&font_face->ref_count)'
 
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
using Cairo;

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
			this.TransientFor = (view.SourceEditorWidget.TextEditor.Toplevel as Gtk.Window) ?? IdeApp.Workbench.RootWindow;
		}
		
		Stage<MessageBubbleHighlightPopupWindow> stage = new Stage<MessageBubbleHighlightPopupWindow> ();

		public void Popup ()
		{
			var rgbaColormap = Screen.RgbaColormap;
			if (rgbaColormap == null)
				return;
			Colormap = rgbaColormap;
			
			Gdk.Rectangle currentBounds = marker.ErrorTextBounds;
			int i = 12;
			int j = 2;
			int x, y;
			view.SourceEditorWidget.TextEditor.GdkWindow.GetOrigin (out x, out y);
			Move (x + currentBounds.X - i, y + currentBounds.Y - j);
			Resize (currentBounds.Width + 2 * i, view.TextEditor.LineHeight + 2 * j);
			
			stage.ActorStep += OnAnimationActorStep;
			stage.Iteration += OnAnimationIteration;
			stage.UpdateFrequency = 10;
			stage.Add (this, 250);
			
			Show ();
		}
		
		double Percent = 0.0;
		
		void OnAnimationIteration (object sender, EventArgs args)
		{
			QueueDraw ();
		}
		
		bool isComing = true;
		bool isFading = false;
		
		bool OnAnimationActorStep (Actor<MessageBubbleHighlightPopupWindow> actor)
		{
			if (isComing) {
				Percent = actor.Percent;
				if (actor.Expired) {
					isComing = false;
					isFading = true;
					actor.Reset ();
					return true;
				}
			} else if (isFading) {
				Percent = 1.0 - actor.Percent;
				if (actor.Expired) {
					isFading = false;
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

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			stage.Playing = false;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var g = Gdk.CairoHelper.Create (evnt.Window)) {
				g.SetSourceRGBA (1, 1, 1, 0);
				g.Operator = Cairo.Operator.Source;
				g.Paint ();
				
				int i = (int)(12.0 * Percent);
				int j = (int)(2.0 * Percent);
				if (!isFading && !isComing) {
					i = j = 0;
				}
				int x = 12 - i;
				int y = 2 - j;
				
				if (marker.FitsInSameLine) {
					g.MoveTo (x + view.TextEditor.LineHeight / 2, y);
					g.LineTo (x, Allocation.Height / 2);
					g.LineTo (x + view.TextEditor.LineHeight / 2, Allocation.Height - y);
				} else {
					g.MoveTo (x, y);
					g.LineTo (x, Allocation.Height - y);
				}
				g.LineTo (Allocation.Width - x, Allocation.Height - y);
				g.LineTo (Allocation.Width - x, y);
				g.ClosePath ();
				Mono.TextEditor.HslColor hsl = marker.colorMatrix[0, 0, 0, 0, 0];
				double delta;
				if (isComing) {
					delta = 1 + 2 - Percent; 
				} else if (isFading) {
					delta = 1 + Percent; 
				} else {
					delta = 1 - Percent; 
				}
				hsl.S += delta;
				g.Color = hsl;
				g.FillPreserve ();
				g.Color = marker.colorMatrix [0, 0, 2, 0, 0];
				g.Stroke ();
				
				if (marker.Errors.Count > 1) {
					int rY = y + view.TextEditor.LineHeight / 6;
					int ew, eh;
					marker.errorCountLayout.GetPixelSize (out ew, out eh);
					int errorCounterWidth = ew + 10;
					int rX = Allocation.Width - errorCounterWidth - 2 - x;

					int rW = errorCounterWidth - 2;
					int rH = view.TextEditor.LineHeight * 3 / 4;
					
					BookmarkMarker.DrawRoundRectangle (g, rX, rY, 8, rW, rH);
					g.Color = new Cairo.Color (0.5, 0.5, 0.5);
					g.Fill ();
					
					if (marker.CollapseExtendedErrors) {
						// TODO !!!
					}
					g.MoveTo (rX + rW / 2 - rW / 4, rY + rH - rH / 4);
					g.LineTo (rX + rW / 2 + rW / 4, rY + rH - rH / 4);
					g.LineTo (rX + rW / 2 , rY + rH / 4);
					g.ClosePath ();
					
					g.Color = new Cairo.Color (1, 1, 1);
					g.Fill ();
				}
				
				g.SelectFontFace ("Sans", FontSlant.Normal, FontWeight.Normal);
				g.SetFontSize (marker.Layouts[0].Layout.FontDescription.Size / Pango.Scale.PangoScale + j);
				string typeString = "error";
				g.Color = Mono.TextEditor.Highlighting.Style.ToCairoColor (view.TextEditor.ColorStyle.GetChunkStyle ("bubble." + typeString + ".text").Color);
				var extends = g.TextExtents (marker.Errors[0].ErrorMessage);
				g.MoveTo (x + 0.5 - extends.XBearing + MessageBubbleTextMarker.border + (marker.FitsInSameLine ? view.TextEditor.LineHeight / 2 : 0), (Allocation.Height - extends.Height) / 2 - extends.YBearing - 0.5);
				g.ShowText (marker.Errors[0].ErrorMessage);
				g.ContextFontFace.Dispose ();
			}
			return false;
		}
	}
}

*/

