// 
// StatusBox.cs
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
using System.ComponentModel;
using Gtk;
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	public class StatusBox : Gtk.Button
	{
		Pango.Layout layout;
		const int leftSpacing   = 2;
		const int ySpacing   = 1;
		
		public string Text {
			get {
				return layout.Text;
			}
			set {
				layout.SetText (value);
				QueueResize ();
			}
		}
		
		public bool DrawRightBorder {
			get;
			set;
		}
		
		public StatusBox ()
		{
			WidgetFlags |= WidgetFlags.NoWindow;
			
			layout = new Pango.Layout (this.PangoContext);
		}
		
		int requestWidth = 200;
		public void UpdateWidth (TextEditorData data)
		{
			Console.WriteLine ("UPDATE !!!");
			using (Pango.Layout layout2 = new Pango.Layout (this.PangoContext)) {
				Console.WriteLine (GetText (data.Document.LineCount, 10000));
				layout2.SetText (GetText (data.Document.LineCount, 10000));
				int h;
				layout2.GetPixelSize (out this.requestWidth, out h);
				QueueResize ();
			}
		}
		
		string GetText (int line, int column)
		{
			return string.Format ("Line: {0}, Column: {1}", line, column);
		}
		
		public void ShowCaretState (int line, int column)
		{
			this.Text = GetText (line, column);
			this.QueueResize ();
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			requisition.Width = requestWidth + 50 + leftSpacing * 2;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Gdk.Drawable win = args.Window;
		
			int width, height;
			layout.GetPixelSize (out width, out height);
			
			int arrowHeight = height / 2; 
			int arrowWidth = arrowHeight + 1;
			int arrowXPos = this.Allocation.X + this.Allocation.Width - arrowWidth;
			if (DrawRightBorder)
				arrowXPos -= 2;
			var state = StateType.Normal;
			//HACK: don't ever draw insensitive, only active/prelight/normal, because insensitive generally looks really ugly
			//this *might* cause some theme issues with the state of the text/arrows rendering on top of it
			
			//HACK: paint the button background as if it were bigger, but it stays clipped to the real area,
			// so we get the content but not the border. This might break with crazy themes.
			//FIXME: we can't use the style's actual internal padding because GTK# hasn't wrapped GtkBorder AFAICT
			// (default-border, inner-border, default-outside-border, etc - see http://git.gnome.org/browse/gtk+/tree/gtk/gtkbutton.c)
			const int padding = 4;
			Style.PaintBox (Style, args.Window, state, ShadowType.None, args.Area, this, "button", 
			                Allocation.X - padding, Allocation.Y - padding, Allocation.Width + padding * 2, Allocation.Height + padding * 2);
			
			int xPos = Allocation.Left;
			
			//constrain the text area so it doesn't get rendered under the arrows
			var textArea = new Gdk.Rectangle (xPos + 2, Allocation.Y + ySpacing, arrowXPos - xPos - 2, Allocation.Height - ySpacing);
			args.Window.DrawLayout (Style.TextGC (StateType.Normal), Allocation.X + 2, Allocation.Y+ ySpacing, layout);
			//Style.PaintLayout (Style, win, state, true, textArea, this, "", textArea.X, textArea.Y, layout);
			
			if (DrawRightBorder)
				win.DrawLine (this.Style.DarkGC (StateType.Normal), Allocation.X + Allocation.Width - 1, Allocation.Y, Allocation.X + Allocation.Width - 1, Allocation.Y + Allocation.Height);			
			return true;
		}
		
	}
}

