//
// CodeSegmentPreviewWindow.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using Gdk;
using Gtk;

namespace Mono.TextEditor
{
	public class CodeSegmentPreviewWindow : Gtk.Window
	{
		const int DefaultPreviewWindowWidth = 320;
		const int DefaultPreviewWindowHeight = 200;
		TextEditor editor;
		ISegment segment;

		public CodeSegmentPreviewWindow (TextEditor editor, ISegment segment) : this (editor, segment, DefaultPreviewWindowWidth, DefaultPreviewWindowHeight)
		{
		}
		
		public CodeSegmentPreviewWindow (TextEditor editor, ISegment segment, int width, int height) : base (Gtk.WindowType.Popup)
		{
			this.editor = editor;
			this.segment = segment;
			this.AppPaintable = true;
			this.DoubleBuffered = false;
			Pango.Layout layout = new Pango.Layout (this.PangoContext);
			layout.FontDescription = editor.Options.Font;
			layout.Ellipsize = Pango.EllipsizeMode.End;
			// setting a max size for the segment (40 lines should be enough), 
			// no need to markup thousands of lines for a preview window
			int startLine = editor.Document.OffsetToLineNumber (segment.Offset);
			int endLine = editor.Document.OffsetToLineNumber (segment.EndOffset);
			if (endLine - startLine > 40)
				this.segment = segment = new Segment (segment.Offset, editor.Document.GetLine (startLine + 20).Offset - segment.Offset);
			layout.SetMarkup (editor.Document.SyntaxMode.GetMarkup (editor.Document,
			                                                        editor.Options,
			                                                        editor.ColorStyle,
			                                                        segment.Offset,
			                                                        segment.Length,
			                                                        true));
			int w, h;
			layout.GetPixelSize (out w, out h);
			this.SetSizeRequest (System.Math.Min (w, width), 
			                     System.Math.Min (h, height));
			layout.Dispose ();
			this.Hidden += delegate { Destroy (); Dispose ();};
		}
		
		public override void Dispose ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
			if (gc != null) { 
				gc.Dispose ();
				gc = null;
			}
			base.Dispose ();
		}
		
		Gdk.GC gc = null;
		Pango.Layout layout = null;
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			if (gc == null)
				gc = new Gdk.GC (ev.Window);
			
			gc.RgbFgColor = editor.ColorStyle.Background;
			ev.Window.DrawRectangle (gc, true, ev.Area);
			
			if (layout == null) {
				layout = new Pango.Layout (this.PangoContext);
				layout.FontDescription = editor.Options.Font;
				layout.Ellipsize = Pango.EllipsizeMode.End;
				layout.SetMarkup (editor.Document.SyntaxMode.GetMarkup (editor.Document,
				                                                        editor.Options,
				                                                        editor.ColorStyle,
				                                                        segment.Offset,
				                                                        segment.Length,
				                                                        true));
			}
			ev.Window.DrawLayout (Style.TextGC (StateType.Normal), 0, 0, layout);
			gc.RgbFgColor = editor.ColorStyle.FoldLine;
			ev.Window.DrawRectangle (gc, false, 0, 0, this.Allocation.Width - 1, this.Allocation.Height - 1);
			return true;
		}
	}
}
