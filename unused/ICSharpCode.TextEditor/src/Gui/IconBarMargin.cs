//  IconBarMargin.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
