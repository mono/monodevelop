//  TipPainter.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 http://www.icsharpcode.net/ <#Develop>
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

using System.Drawing;
using System.Drawing.Text;

namespace MonoDevelop.TextEditor.Util
{
	sealed class TipPainter
	{
		const float HorizontalBorder = 2;
		const float VerticalBorder   = 1;
		
		//static StringFormat centerTipFormat = CreateTipStringFormat();
		
		TipPainter()
		{
			
		}
		
		public static void DrawTip(Gtk.Widget control, Graphics graphics,
		                           Font font, string description)
		{
        		DrawTip(control, graphics, new TipText (graphics, font, description));
		}
		                           
		public static void DrawTip(Gtk.Widget control, Graphics graphics,
		                           TipSection tipData)
		{
			Size tipSize = Size.Empty; SizeF tipSizeF = SizeF.Empty;
						
#if GTK
			RectangleF workingArea = control.RootWindow.FrameExtents;
			int x, y;	
			control.GetPointer (out x, out y);
#else
			RectangleF workingArea = SystemInformation.WorkingArea;
#endif
		
			PointF screenLocation = new PointF (x, y);
			
			SizeF maxLayoutSize = new SizeF
				(workingArea.Right - screenLocation.X - HorizontalBorder * 2,
				 workingArea.Bottom - screenLocation.Y - VerticalBorder * 2);
			
			if (maxLayoutSize.Width > 0 && maxLayoutSize.Height > 0) {
				graphics.TextRenderingHint =
					TextRenderingHint.AntiAliasGridFit;
				
				tipData.SetMaximumSize(maxLayoutSize);
				tipSizeF = tipData.GetRequiredSize();
				tipData.SetAllocatedSize(tipSizeF);

				tipSizeF += new SizeF(HorizontalBorder * 2,
				                      VerticalBorder   * 2);
				tipSize = Size.Ceiling(tipSizeF);
			}

#if GTK			
			if (control.RequestSize != tipSize) {
				control.RequestSize = tipSize;
			}
#else
			if (control.ClientSize != tipSize) {
				control.ClientSize = tipSize;
			}
#endif
			
			if (tipSize != Size.Empty) {
				Rectangle borderRectangle = new Rectangle
					(Point.Empty, tipSize - new Size(1, 1));
				
				RectangleF displayRectangle = new RectangleF
					(HorizontalBorder, VerticalBorder,
					 tipSizeF.Width - HorizontalBorder * 2,
					 tipSizeF.Height - VerticalBorder * 2);
				
				// DrawRectangle draws from Left to Left + Width. A bug? :-/
				graphics.DrawRectangle(SystemPens.WindowFrame,
				                       borderRectangle);
				tipData.Draw(new PointF(HorizontalBorder, VerticalBorder));
			}
		}
	}
}
