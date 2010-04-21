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
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public class CodeSegmentPreviewWindow : Gtk.Window
	{
		const int DefaultPreviewWindowWidth = 320;
		const int DefaultPreviewWindowHeight = 200;
		TextEditor editor;
		Pango.FontDescription fontDescription;
		Pango.Layout layout;
		Pango.Layout informLayout;
		
		public static string CodeSegmentPreviewInformString {
			get;
			set;
		}
		
		public bool HideCodeSegmentPreviewInformString {
			get;
			private set;
		}
		
		public CodeSegmentPreviewWindow (TextEditor editor, bool hideCodeSegmentPreviewInformString, ISegment segment) : this(editor, hideCodeSegmentPreviewInformString, segment, DefaultPreviewWindowWidth, DefaultPreviewWindowHeight)
		{
		}
		
		public CodeSegmentPreviewWindow (TextEditor editor, bool hideCodeSegmentPreviewInformString, ISegment segment, int width, int height) : base (Gtk.WindowType.Popup)
		{
			this.HideCodeSegmentPreviewInformString = hideCodeSegmentPreviewInformString;
			this.editor = editor;
			this.AppPaintable = true;
			layout = new Pango.Layout (PangoContext);
			informLayout = new Pango.Layout (PangoContext);
			informLayout.SetText (CodeSegmentPreviewInformString);
			
			fontDescription = Pango.FontDescription.FromString (editor.Options.FontName);
			fontDescription.Size = (int)(fontDescription.Size * 0.8f);
			layout.FontDescription = fontDescription;
			layout.Ellipsize = Pango.EllipsizeMode.End;
			// setting a max size for the segment (40 lines should be enough), 
			// no need to markup thousands of lines for a preview window
			int startLine = editor.Document.OffsetToLineNumber (segment.Offset);
			int endLine = editor.Document.OffsetToLineNumber (segment.EndOffset);
			const int maxLines = 40;
			bool pushedLineLimit = endLine - startLine > maxLines;
			if (pushedLineLimit)
				segment = new Segment (segment.Offset, editor.Document.GetLine (startLine + maxLines).Offset - segment.Offset);
			layout.Ellipsize = Pango.EllipsizeMode.End;
			layout.SetMarkup (editor.Document.SyntaxMode.GetMarkup (editor.Document,
			                                                        editor.Options,
			                                                        editor.ColorStyle,
			                                                        segment.Offset,
			                                                        segment.Length,
			                                                        true) + (pushedLineLimit ? Environment.NewLine + "..." : ""));
			CalculateSize ();
		}
		
		public void CalculateSize ()
		{
			int w, h;
			layout.GetPixelSize (out w, out h);
			
			if (!HideCodeSegmentPreviewInformString) {
				int w2, h2;
				informLayout.GetPixelSize (out w2, out h2); 
				w = System.Math.Max (w, w2);
				h += h2;
			}
			
			this.SetSizeRequest (System.Math.Max (1, System.Math.Min (w + 3, Screen.Width * 2 / 5)), 
			                     System.Math.Max (1, System.Math.Min (h + 3, Screen.Height * 2 / 5)));
		}
		
		protected override void OnDestroyed ()
		{
			layout = layout.Kill ();
			informLayout = informLayout.Kill ();
			fontDescription = fontDescription.Kill ();
			gc = gc.Kill ();
			base.OnDestroyed ();
		}
		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			Console.WriteLine (evnt.Key);
			return base.OnKeyPressEvent (evnt);
		}

		
		Gdk.GC gc = null;
		
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			if (gc == null) {
				gc = new Gdk.GC (ev.Window);
			}
			
			gc.RgbFgColor = editor.ColorStyle.Default.BackgroundColor;
			ev.Window.DrawRectangle (gc, true, ev.Area);
			ev.Window.DrawLayout (Style.TextGC (StateType.Normal), 1, 1, layout);
		
			ev.Window.DrawRectangle (gc, false, 1, 1, this.Allocation.Width - 3, this.Allocation.Height - 3);
			gc.RgbFgColor = editor.ColorStyle.FoldLine.Color;
			ev.Window.DrawRectangle (gc, false, 0, 0, this.Allocation.Width - 1, this.Allocation.Height - 1);
			
			if (!HideCodeSegmentPreviewInformString) {
				informLayout.SetText (CodeSegmentPreviewInformString);
				int w, h;
				informLayout.GetPixelSize (out w, out h); 
				
				gc.RgbFgColor = editor.ColorStyle.FoldLine.BackgroundColor;
				ev.Window.DrawRectangle (gc, true, Allocation.Width - w - 3, Allocation.Height - h, w + 2, h - 1);
				gc.RgbFgColor = editor.ColorStyle.FoldLine.Color;
				ev.Window.DrawLayout (gc, Allocation.Width - w - 3, Allocation.Height - h, informLayout);
			}
			return true;
		}
	}
}
