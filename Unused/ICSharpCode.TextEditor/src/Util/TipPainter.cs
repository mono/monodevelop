// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="?" email="?"/>
//     <version value="$version"/>
// </file>

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
