// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Drawing;
using MonoDevelop.TextEditor.Document;

using Gdk;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// This class views the line numbers and folding markers.
	/// </summary>
	public class FoldMargin : AbstractMargin
	{
		public override Size Size {
			get {
				return new Size((int)(textArea.TextView.FontHeight),
				                -1);
			}
		}
		
		public override Cursor Cursor {
			get {
				return GutterMargin.RightLeftCursor;
			}
		}
		
		public override bool IsVisible {
			get {
				return textArea.TextEditorProperties.EnableFolding;
			}
		}
		
		public FoldMargin(TextArea textArea) : base(textArea)
		{
		}
		
		public override void Paint(Gdk.Drawable wnd, System.Drawing.Rectangle rect)
		{
			HighlightColor lineNumberPainterColor = textArea.Document.HighlightingStrategy.GetColorFor("LineNumbers");
			
			Gdk.Color rect_fg = TextArea.Style.White;
			Gdk.Color text_fg = new Gdk.Color (lineNumberPainterColor.Color);
			Gdk.Color text_bg = new Gdk.Color (lineNumberPainterColor.BackgroundColor);
			
			using (Gdk.GC gc = new Gdk.GC(wnd)) {
			for (int y = 0; y < (DrawingPosition.Height + textArea.TextView.VisibleLineDrawingRemainder) / textArea.TextView.FontHeight + 1; ++y) {
				
				gc.RgbFgColor = rect_fg;
				System.Drawing.Rectangle markerRectangle = new System.Drawing.Rectangle(DrawingPosition.X, DrawingPosition.Top + y * textArea.TextView.FontHeight - textArea.TextView.VisibleLineDrawingRemainder, DrawingPosition.Width, textArea.TextView.FontHeight);
				
				if (rect.IntersectsWith(markerRectangle)) {
					wnd.DrawRectangle (gc, true, new System.Drawing.Rectangle (markerRectangle.X + 1, markerRectangle.Y, markerRectangle.Width - 1, markerRectangle.Height));
					
					int currentLine = textArea.TextView.FirstVisibleLine + y;
					PaintFoldMarker(wnd, currentLine, markerRectangle);
				} //Using
			}
			}
		
		}
		
		
		void PaintFoldMarker(Gdk.Drawable g, int lineNumber, System.Drawing.Rectangle drawingRectangle)
		{
			HighlightColor foldLineColor = textArea.Document.HighlightingStrategy.GetColorFor("FoldLine");
			
			bool isFoldStart = textArea.Document.FoldingManager.IsFoldStart(lineNumber);
			bool isBetween   = textArea.Document.FoldingManager.IsBetweenFolding(lineNumber);
			bool isFoldEnd   = textArea.Document.FoldingManager.IsFoldEnd(lineNumber);
							
			int foldMarkerSize = (int)Math.Round(textArea.TextView.FontHeight * 0.57f);
			foldMarkerSize -= (foldMarkerSize) % 2;
			int foldMarkerYPos = drawingRectangle.Y + (int)((drawingRectangle.Height - foldMarkerSize) / 2);
			int xPos = drawingRectangle.X + (drawingRectangle.Width - foldMarkerSize) / 2 + foldMarkerSize / 2;
			
			
			if (isFoldStart) {
				ArrayList startFoldings = textArea.Document.FoldingManager.GetFoldingsWithStart(lineNumber);
				bool isVisible = true;
				bool moreLinedOpenFold = false;
				foreach (FoldMarker foldMarker in startFoldings) {
					if (foldMarker.IsFolded) {
						isVisible = false;
					} else {
						moreLinedOpenFold = foldMarker.EndLine > foldMarker.StartLine;
					}
				}
				
				ArrayList endFoldings = textArea.Document.FoldingManager.GetFoldingsWithEnd(lineNumber);
				bool isFoldEndFromUpperFold = false;
				foreach (FoldMarker foldMarker in endFoldings) {
					if (foldMarker.EndLine > foldMarker.StartLine && !foldMarker.IsFolded) {
						isFoldEndFromUpperFold = true;
					} 
				}
				
				DrawFoldMarker(g, new RectangleF(drawingRectangle.X + (drawingRectangle.Width - foldMarkerSize) / 2,
				                                 foldMarkerYPos,
				                                 foldMarkerSize,
				                                 foldMarkerSize),
				                  isVisible);
				if (isBetween || isFoldEndFromUpperFold) {
					using (Gdk.GC gc = new Gdk.GC(g)) {
						gc.RgbFgColor = new Gdk.Color (foldLineColor.Color);
					
						g.DrawLine(gc,
					           xPos,
					           drawingRectangle.Top,
					           xPos,
					           foldMarkerYPos);
					}
				}
				
				if (isBetween || moreLinedOpenFold) {
					using (Gdk.GC gc = new Gdk.GC(g)) {
						gc.RgbFgColor = new Gdk.Color (foldLineColor.Color);
						g.DrawLine(gc,
					           xPos,
					           foldMarkerYPos + foldMarkerSize,
					           xPos,
					           drawingRectangle.Bottom);
					}
				}
			} else {
				if (isFoldEnd) {
					int midy = drawingRectangle.Top + drawingRectangle.Height / 2;
					using (Gdk.GC gc = new Gdk.GC(g)) {
						gc.RgbFgColor = new Gdk.Color (foldLineColor.Color);
						g.DrawLine(gc,
					                xPos,
					                drawingRectangle.Top,
					                xPos,
					                isBetween ? drawingRectangle.Bottom : midy);
						g.DrawLine(gc,
									xPos,
									midy,
									xPos + foldMarkerSize / 2,
									midy);
					}
				} else if (isBetween) {
					using (Gdk.GC gc = new Gdk.GC(g)) {
						gc.RgbFgColor = new Gdk.Color (foldLineColor.Color);
						g.DrawLine(gc,
					                xPos,
					                drawingRectangle.Top,
					                xPos,
					                drawingRectangle.Bottom);
					}
				}
			}
		}
		
//		protected override void OnClick(EventArgs e)
		public void OnClick (System.Drawing.Point mousepos)
		{
			bool  showFolding = textArea.Document.TextEditorProperties.EnableFolding;
			//Point mousepos    = PointToClient(Control.MousePosition);
			int   realline    = textArea.Document.GetVisibleLine((int)((mousepos.Y/* + virtualTop*/) / textArea.TextView.FontHeight));
			
			// focus the textarea if the user clicks on the line number view
			textArea.GrabFocus();
			
			if (!showFolding || mousepos.X < Size.Width - 15 || realline < 0 || realline + 1 >= textArea.Document.TotalNumberOfLines) {
				return;
			}
			
			ArrayList foldMarkers = textArea.Document.FoldingManager.GetFoldingsWithStart(realline);
			foreach (FoldMarker fm in foldMarkers) {
				fm.IsFolded = !fm.IsFolded;
			}
			//this.QueueDraw ();
			textArea.QueueDraw ();
			//TextEditorControl.IconBar.QueueDraw ();
		}
//		{
//			base.OnClick(e);
//			bool  showFolding = textarea.Document.TextEditorProperties.EnableFolding;
//			Point mousepos    = PointToClient(Control.MousePosition);
//			int   realline    = textarea.Document.GetVisibleLine((int)((mousepos.Y + virtualTop) / textarea.FontHeight));
//			
//			// focus the textarea if the user clicks on the line number view
//			textarea.Focus();
//			
//			if (!showFolding || mousepos.X < Width - 15 || realline < 0 || realline + 1 >= textarea.Document.TotalNumberOfLines) {
//				return;
//			}
//			
//			ArrayList foldMarkers = textarea.Document.FoldingManager.GetFoldingsWithStart(realline);
//			foreach (FoldMarker fm in foldMarkers) {
//				fm.IsFolded = !fm.IsFolded;
//			}
//			Refresh();
//			textarea.Refresh();
//			TextEditorControl.IconBar.Refresh();
//		}
		
#region Drawing functions
		void DrawFoldMarker(Gdk.Drawable g, RectangleF rectangle, bool isOpened)
		{

			HighlightColor foldMarkerColor = textArea.Document.HighlightingStrategy.GetColorFor("FoldMarker");
			HighlightColor foldLineColor   = textArea.Document.HighlightingStrategy.GetColorFor("FoldLine");
			
			System.Drawing.Rectangle intRect = new System.Drawing.Rectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height);
			using (Gdk.GC gc = new Gdk.GC(g)) {
				gc.RgbFgColor = new Gdk.Color (foldMarkerColor.BackgroundColor);
				g.DrawRectangle(gc, true, intRect);
				gc.RgbFgColor = new Gdk.Color (foldMarkerColor.Color);
				g.DrawRectangle(gc, false, intRect);
			
				int space  = (int)Math.Round(((double)rectangle.Height) / 8d) + 1;
				int mid    = intRect.Height / 2 + intRect.Height % 2;			
			
				gc.RgbFgColor = new Gdk.Color (foldMarkerColor.Color);
				g.DrawLine(gc, 
			           (int)rectangle.X + space, 
			           (int)rectangle.Y + mid, 
			           (int)rectangle.X + (int)rectangle.Width - space, 
			           (int)rectangle.Y + mid);
			
				if (!isOpened) {
					g.DrawLine(gc, 
				           (int)rectangle.X + mid, 
				           (int)rectangle.Y + space, 
				           (int)rectangle.X + mid, 
				           (int)rectangle.Y + (int)rectangle.Height - space);

				}
			}
		}
#endregion
	}
}
