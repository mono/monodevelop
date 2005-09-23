// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// This class views the line numbers and folding markers.
	/// </summary>
	public class IconBarMargin : AbstractMargin
	{
		public override Size Size {
			get {
				return new Size((int)(textArea.TextView.FontHeight * 1.2f),
				                -1);
			}
		}
		
		public override bool IsVisible {
			get {
				return textArea.TextEditorProperties.IsIconBarVisible;
			}
		}
		
		
		public IconBarMargin(TextArea textArea) : base(textArea)
		{
		}
		
		public override void Paint(Gdk.Drawable wnd, Rectangle rect)
		{
			HighlightColor lineNumberPainterColor = textArea.Document.HighlightingStrategy.GetColorFor("LineNumbers");
			
			Gdk.Color rect_fg = TextArea.Style.White;
			Gdk.Color text_fg = new Gdk.Color (lineNumberPainterColor.Color);
			Gdk.Color text_bg = new Gdk.Color (lineNumberPainterColor.BackgroundColor);
			
			using (Gdk.GC gc = new Gdk.GC (wnd)) {
				gc.RgbFgColor = rect_fg;
				wnd.DrawRectangle (gc, true, new System.Drawing.Rectangle (drawingPosition.X, rect.Top, drawingPosition.Width - 1, rect.Height));
				
				
				//g.DrawLine(SystemPens.ControlDark, base.drawingPosition.Right - 1, rect.Top, base.drawingPosition.Right - 1, rect.Bottom);
				
				// paint icons
				foreach (int mark in textArea.Document.BookmarkManager.Marks) {
					int lineNumber = textArea.Document.GetLogicalLine(mark);
					int yPos = (int)(lineNumber * textArea.TextView.FontHeight) - textArea.VirtualTop.Y;
//					if (yPos >= rect.Y && yPos <= rect.Bottom) {
						DrawBookmark(gc, wnd, yPos);
//					}
				}
			}
		
		}
		
#region Drawing functions
		void DrawBookmark(Gdk.GC gc, Gdk.Drawable wnd, int y)
		{
			gc.RgbFgColor = new Gdk.Color (Color.DarkBlue);
			int delta = textArea.TextView.FontHeight / 6;
			Rectangle rect = new Rectangle( 2, y + delta, base.drawingPosition.Width - 6, textArea.TextView.FontHeight - 2 * delta);
			
			wnd.DrawRectangle (gc, true, rect);
			
			gc.RgbFgColor = new Gdk.Color (Color.Black);
			
			wnd.DrawRectangle (gc, false, rect);
			//FillRoundRect(g, Brushes.Cyan, rect);
			//DrawRoundRect(g, Pens.Black, rect);
		}
		
		GraphicsPath CreateRoundRectGraphicsPath(Rectangle r)
		{
			/*
			GraphicsPath gp = new GraphicsPath();
			int radius = r.Width / 2;
			gp.AddLine(r.X + radius, r.Y, r.Right - radius, r.Y);
			gp.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
			
			gp.AddLine(r.Right, r.Y + radius, r.Right, r.Bottom - radius);
			gp.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
			
			gp.AddLine(r.Right - radius, r.Bottom, r.X + radius, r.Bottom);
			gp.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
			
			gp.AddLine(r.X, r.Bottom - radius, r.X, r.Y + radius);
			gp.AddArc(r.X, r.Y, radius, radius, 180, 90);
			
			gp.CloseFigure();
			*/
			return new GraphicsPath();
		}
		
		/*void DrawRoundRect(Gdk.GC gc, Gdk.Drawable wnd, Rectangle r)
		{
			GraphicsPath gp = CreateRoundRectGraphicsPath(r);
			g.DrawPath(p, gp);
			gp.Dispose();
		}

		void FillRoundRect(Gdk.GC gc, Gdk.Drawable wnd, Rectangle r)
		{
			GraphicsPath gp = CreateRoundRectGraphicsPath(r);
			g.FillPath(b, gp);
			gp.Dispose();
		}*/
#endregion
	}
}
