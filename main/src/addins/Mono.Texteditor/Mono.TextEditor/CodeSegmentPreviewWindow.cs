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
		const int MaxWidth = 320;
		const int MaxHeight = 200;
		TextEditor editor;
		ISegment segment;
		
		public CodeSegmentPreviewWindow (TextEditor editor, ISegment segment) : base (Gtk.WindowType.Popup)
		{
			this.editor = editor;
			this.segment  = segment;
			
			Pango.Layout layout = new Pango.Layout (this.PangoContext);
			layout.FontDescription = TextEditorOptions.Options.Font;
			layout.Ellipsize = Pango.EllipsizeMode.End;
			layout.SetMarkup (editor.Document.SyntaxMode.GetMarkup (editor.Document, 
			                                                        editor.ColorStyle,
			                                                        segment.Offset,
			                                                        segment.Length,
			                                                        true));
			int width, height;
			layout.GetPixelSize (out width, out height);
			this.SetSizeRequest (System.Math.Min (width, MaxWidth), 
			                     System.Math.Min (height, MaxHeight));
			layout.Dispose ();
			
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			Gdk.GC gc = new Gdk.GC (ev.Window);
			gc.RgbFgColor = editor.ColorStyle.Background;
			ev.Window.DrawRectangle (gc, true, ev.Area);
			Pango.Layout layout = new Pango.Layout (this.PangoContext);
			layout.FontDescription = TextEditorOptions.Options.Font;
			layout.Ellipsize = Pango.EllipsizeMode.End;
			layout.SetMarkup (editor.Document.SyntaxMode.GetMarkup (editor.Document, 
			                                                        editor.ColorStyle,
			                                                        segment.Offset,
			                                                        segment.Length,
			                                                        true));
			ev.Window.DrawLayout (Style.TextGC (StateType.Normal), 0, 0, layout);
			layout.Dispose ();
			gc.RgbFgColor = editor.ColorStyle.FoldLine;
			ev.Window.DrawRectangle (gc, false, 0, 0, this.Allocation.Width - 1, this.Allocation.Height - 1);
			gc.Dispose ();
			return true;
		}
	}
}
